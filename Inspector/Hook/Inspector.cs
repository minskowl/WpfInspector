using ChristianMoser.WpfInspector.UserInterface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using ChristianMoser.WpfInspector.Services;
namespace ChristianMoser.WpfInspector.Hook
{
    public static class SnoopModes
    {
        /// <summary>
        /// Whether Snoop is snooping in a situation where there are multiple app domains.
        /// The main Snoop UI is needed for each app domain.
        /// </summary>
        public static bool MultipleAppDomainMode { get; set; }

        /// <summary>
        /// Whether Snoop is snooping in a situation where there are multiple dispatchers.
        /// The main Snoop UI is needed for each dispatcher.
        /// </summary>
        public static bool MultipleDispatcherMode { get; set; }

        public static bool SwallowExceptions { get; set; }

        public static bool IgnoreExceptions { get; set; }
    }
    public class Inspector
    {

        public static int Inject(string p)
        {
            return new Inspector().StartInject(p);
        }
        public  int StartInject(string p)
        {
            try
            {
                Debugger.Launch();
                var settingsData = TransientSettingsData.LoadCurrent(p);

                var succeeded = false;

                    succeeded = this.RunInCurrentAppDomain(settingsData);

                return 0;
            }
            catch (Exception exception)
            {
                Trace.WriteLine(exception);
                return 1;
            }
        }
        public bool RunInCurrentAppDomain(TransientSettingsData settingsData)
        {
            Trace.WriteLine($"Trying to run Snoop in app domain \"{AppDomain.CurrentDomain.FriendlyName}\"...");

            try
            {
                var instanceCreator = GetInstanceCreator(settingsData.StartTarget);

                var result = InjectSnoopIntoDispatchers(settingsData, (data, dispatcher) => CreateSnoopWindow(data, dispatcher, instanceCreator));

                Trace.WriteLine($"Successfully running Snoop in app domain \"{AppDomain.CurrentDomain.FriendlyName}\".");

                return result;
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Failed to to run Snoop in app domain \"{AppDomain.CurrentDomain.FriendlyName}\".");

                if (SnoopModes.MultipleAppDomainMode)
                {
                    Trace.WriteLine($"Could not snoop a specific app domain with friendly name of \"{AppDomain.CurrentDomain.FriendlyName}\" in multiple app domain mode.");
                    Trace.WriteLine(exception);
                }
                else
                {
                    //ErrorDialog.ShowDialog(exception, "Error snooping", "There was an error snooping the application.", exceptionAlreadyHandled: true);
                }

                return false;
            }
        }

        private static InspectorWindow CreateSnoopWindow(TransientSettingsData settingsData, Dispatcher dispatcher, Func<InspectorWindow> instanceCreator)
        {
            var inspectorWinow = instanceCreator();

       
            ServiceLocator.RegisterInstance<InspectorWindow>(inspectorWinow);
            if (Application.Current != null && Application.Current.MainWindow != null)
            {
                inspectorWinow.Owner = Application.Current.MainWindow;
            }
            inspectorWinow.Show();
            return inspectorWinow;
        }
        private static Func<InspectorWindow> GetInstanceCreator(SnoopStartTarget startTarget)
        {
          return () =>  new InspectorWindow();
        }



        private static bool InjectSnoopIntoDispatchers(TransientSettingsData settingsData, Func<TransientSettingsData, Dispatcher, InspectorWindow> instanceCreator)
        {
            // check and see if any of the root visuals have a different mainDispatcher
            // if so, ask the user if they wish to enter multiple mainDispatcher mode.
            // if they do, launch a snoop ui for every additional mainDispatcher.
            // see http://snoopwpf.codeplex.com/workitem/6334 for more info.

            var rootVisuals = new List<Visual>();
            var dispatchers = new List<Dispatcher>();

            foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
            {
                var presentationSourceRootVisual = presentationSource.RootVisual;

                if (presentationSourceRootVisual is null)
                {
                    continue;
                }

                var presentationSourceRootVisualDispatcher = presentationSourceRootVisual.Dispatcher;

                // Check if we have already seen this dispatcher and it's root visual
                if (dispatchers.IndexOf(presentationSourceRootVisualDispatcher) == -1)
                {
                    dispatchers.Add(presentationSourceRootVisualDispatcher);

                    rootVisuals.Add(presentationSourceRootVisual);
                }
            }

            var useMultipleDispatcherMode = false;
            if (rootVisuals.Count > 0)
            {
                if (rootVisuals.Count == 1
                    || settingsData.MultipleDispatcherMode == MultipleDispatcherMode.NeverUse)
                {
                    var rootVisual = rootVisuals[0];

                    rootVisual.Dispatcher.Invoke((Action)(() =>
                    {
                        var snoopInstance = instanceCreator(settingsData, rootVisual.Dispatcher);

                        //if (snoopInstance.Target is null)
                        //{
                        //    snoopInstance.Target = rootVisual;
                        //}
                    }));

                    return true;
                }

                // Should we skip the question and always use multiple dispatcher mode?
                if (settingsData.MultipleDispatcherMode == MultipleDispatcherMode.AlwaysUse)
                {
                    useMultipleDispatcherMode = true;
                }
                else
                {
                    var result =
                        MessageBox.Show(
                            "Snoop has noticed windows running in multiple dispatchers.\n\n" +
                            "Would you like to enter multiple dispatcher mode, and have a separate Snoop window for each dispatcher?\n\n" +
                            "Without having a separate Snoop window for each dispatcher, you will not be able to Snoop the windows in the dispatcher threads outside of the main dispatcher. " +
                            "Also, note, that if you bring up additional windows in additional dispatchers (after snooping), you will need to Snoop again in order to launch Snoop windows for those additional dispatchers.",
                            "Enter Multiple Dispatcher Mode",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        useMultipleDispatcherMode = true;
                    }
                }

                if (useMultipleDispatcherMode)
                {
                    SnoopModes.MultipleDispatcherMode = true;

                    var thread = new Thread(DispatchOut);
                    thread.Start(new DispatchOutParameters(settingsData, instanceCreator, rootVisuals));

                    // todo: check if we really created something
                    return true;
                }
            }

            return false;
        }

        public static void Inject()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            var inspectorWinow = new InspectorWindow();
            ServiceLocator.RegisterInstance<InspectorWindow>(inspectorWinow);
            if (Application.Current != null && Application.Current.MainWindow != null)
            {
                inspectorWinow.Owner = Application.Current.MainWindow;
            }
            inspectorWinow.Show();
        }

        static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(((Exception)e.ExceptionObject).Message);
        }
        private static void DispatchOut(object o)
        {
            var dispatchOutParameters = (DispatchOutParameters)o;

            foreach (var visual in dispatchOutParameters.Visuals)
            {
                // launch a snoop ui on each dispatcher
                visual.Dispatcher.Invoke(
                    (Action)(() =>
                    {
                        var snoopInstance = dispatchOutParameters.InstanceCreator(dispatchOutParameters.SettingsData, visual.Dispatcher);

                        //if (snoopInstance.Target is null)
                        //{
                        //    snoopInstance.Target = visual;
                        //}
                    }));
            }
        }

        private class DispatchOutParameters
        {
            public DispatchOutParameters(TransientSettingsData settingsData, Func<TransientSettingsData, Dispatcher, InspectorWindow> instanceCreator, List<Visual> visuals)
            {
                this.SettingsData = settingsData;
                this.InstanceCreator = instanceCreator;
                this.Visuals = visuals;
            }

            public TransientSettingsData SettingsData { get; }

            public Func<TransientSettingsData, Dispatcher, InspectorWindow> InstanceCreator { get; }

            public List<Visual> Visuals { get; }
        }
    }
}

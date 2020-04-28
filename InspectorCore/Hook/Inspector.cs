using ChristianMoser.WpfInspector.UserInterface;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using ChristianMoser.WpfInspector.Services;
namespace ChristianMoser.WpfInspector.Hook
{
    public class Inspector
    {
        public static void Inject()
        {
            Log("Injected");
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            var inspectorWinow = new InspectorWindow();
            ServiceLocator.RegisterInstance<InspectorWindow>(inspectorWinow);
            if (Application.Current != null && Application.Current.MainWindow != null)
            {
                inspectorWinow.Owner = Application.Current.MainWindow;
            }
            inspectorWinow.Show();
        }

        public static void Log(string text)
        {
            File.WriteAllText("d:\\log.txt", DateTime.Now+text);
        }
        static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(((Exception) e.ExceptionObject).Message);
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;


namespace ChristianMoser.WpfInspector.Services
{
    public static class InjectorLauncher
    {
        private static string GetSuffix(ManagedApplicationInfo windowInfo)
        {
            if (windowInfo.Bitness == 32)
            {
                return "x86";
            }

            return "x64";
        }

        public static void Launch(ManagedApplicationInfo processInfo, string assembly, string className,
            string methodName, string text)
        {


            var location = Assembly.GetExecutingAssembly().Location;
            var directory = Path.GetDirectoryName(location) ?? string.Empty;
            var injectorLauncherExe = Path.Combine(directory, $"Snoop.InjectorLauncher.{GetSuffix(processInfo)}.exe");

            if (File.Exists(injectorLauncherExe) == false)
            {
                var message = $@"Could not find the injector launcher ""{injectorLauncherExe}"".
Snoop requires this component, which is part of the Snoop project, to do it's job.
- If you compiled snoop yourself, you should compile all required components.
- If you downloaded snoop you should not omit any files contained in the archive you downloaded and make sure that no anti virus deleted the file.";
                throw new FileNotFoundException(message, injectorLauncherExe);
            }




            var startInfo = new ProcessStartInfo(injectorLauncherExe, $"--t { processInfo.ProcessId} --a {assembly} --c {className} --m {methodName} --h {processInfo.HWnd.ToInt32()} --v ")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = processInfo.IsOwningProcessElevated
                                           ? "runas"
                                           : null
            };

            using (var process = Process.Start(startInfo))
            {
                process?.WaitForExit();
            }




        }
    }
}
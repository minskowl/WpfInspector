#if NETFULL
using System;
using System.Threading;
using System.ServiceModel;
using ChristianMoser.WpfInspector.Services;



namespace ChristianMoser.WpfInspector.Process32Helper
{


    public static class Application
    {
        /// <summary>
        /// Static application entry point
        /// </summary>
        /// <param name="args">The args.</param>
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello");
            // Start a serivce host to query 32-bit processes
            try
            {
                var host = new ServiceHost(typeof(ProcessService));
                host.AddServiceEndpoint(typeof(IProcessService), new NetTcpBinding(), "net.tcp://localhost/ProcessService");
                host.Open();
                Console.WriteLine("Started");
                // Wait forever
                var evt = new AutoResetEvent(false);
                evt.WaitOne();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
   

        }

     }

}
#endif
using System;
using System.Net;
using System.Collections.Generic;
using System.Linq; using System.Threading;
using System.Windows.Forms;
using Castle.MicroKernel.Registration;
using AutoTest.Core.Configuration;
using AutoTest.Core.FileSystem;
using AutoTest.Core.Presenters;
using AutoTest.Core.Messaging;
using AutoTest.Core.Caching.RunResultCache;
using AutoTest.Messages;
using AutoTest.Core.DebugLog;
using AutoTest.Server.Communication;
using AutoTest.Core.Launchers;

namespace AutoTest.Server
{
    class ConsoleWriter : IWriteDebugInfo
    {
        public void SetRecycleSize(long size) {}
        public void WriteError(string message) {
            Console.WriteLine(message);
        }
        public void WriteInfo(string message) {
            Console.WriteLine(message);
        }
        public void WriteDebug(string message) {
            Console.WriteLine(message);
        }
        public void WritePreProcessor(string message) {
            Console.WriteLine(message);
        }
        public void WriteDetail(string message) {
            Console.WriteLine(message);
        }
    }
    
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var exit = false;
            Console.CancelKeyPress += delegate {
                exit = true;
            };

            var watchdir = "/home/ack/src/OpenIDE";
            BootStrapper.Configure();
            Debug.EnableLogging(new ConsoleWriter());
            BootStrapper.Container
                .Register(
                    Component.For<IMessageProxy>()
                        .Forward<IRunFeedbackView>()
                        .Forward<IInformationFeedbackView>()
                        .Forward<IConsumerOf<AbortMessage>>()
                        .ImplementedBy<MessageProxy>().LifeStyle.Singleton);

            var launcher = BootStrapper.Services.Locate<IApplicatonLauncher>();
            using (var server = new MessageEndpoint(launcher)) {
                var proxy = BootStrapper.Services.Locate<IMessageProxy>();
                proxy.SetMessageForwarder(server);
                BootStrapper.Services.Locate<IRunResultCache>().EnabledDeltas();
                BootStrapper.InitializeCache(watchdir);
                using (var watcher = BootStrapper.Services.Locate<IDirectoryWatcher>())
                {
                    watcher.Watch(watchdir);

                    while (!exit && server.IsAlive) {
                        Thread.Sleep(100);
                    }
                    Console.WriteLine("exiting");
                    //Application.EnableVisualStyles();
                    //Application.SetCompatibleTextRenderingDefault(false);
                    //Application.Run(new HiddenGetFocusForm());
                }
                Console.WriteLine("shutting down");
                BootStrapper.ShutDown();
                Console.WriteLine("disposing server");
            }
            Console.WriteLine("done");
        }
    }
}

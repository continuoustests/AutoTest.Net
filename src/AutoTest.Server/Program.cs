using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Castle.MicroKernel.Registration;
using AutoTest.Core.Configuration;
using AutoTest.Core.FileSystem;
using AutoTest.Core.Presenters;
using AutoTest.Core.Messaging;
using AutoTest.Core.Caching.RunResultCache;
using AutoTest.Messages;
using AutoTest.Core.DebugLog;
using AutoTest.Core.Caching;
using AutoTest.Server.Communication;
using AutoTest.Core.Launchers;
using AutoTest.Server.Handlers;

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

    public class Arguments
    {
        public string WatchToken { get; set; }
        public string ConfigurationLocation { get; set; }
        public bool Help { get; set; }
    }

    public class ArgumentParser
    {
        public static Arguments Parse(string[] arguments)
        {
            var parsed = new Arguments();
            foreach (var argument in arguments) {
                if (!argument.StartsWith("--")) {
                    parsed.WatchToken = argument;
                } else {
                    if (argument.StartsWith("--local-config-location="))
                        parsed.ConfigurationLocation = argument.Replace("--local-config-location=", "");
                    else if (argument.StartsWith("--help"))
                        parsed.Help = true;
                }
            }
            return parsed;
        }
    }
    
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var exit = false;
            Console.CancelKeyPress += delegate {
                exit = true;
            };

            var arguments = ArgumentParser.Parse(args);
            if (arguments.Help) {
                Console.WriteLine("AutoTest.Server.exe command line arguments");
                Console.WriteLine("");
                Console.WriteLine("To specify watch directory on startup you can type:");
                Console.WriteLine("\tAutoTest.WinForms.exe [WATCH_DIRECTORY] [--local-config-location=/path]");
                return;
            }
            string token = null; 
            if (arguments.WatchToken != null) {
                var tokenExists = Directory.Exists(arguments.WatchToken) || File.Exists(arguments.WatchToken);
                if (arguments.WatchToken.Contains(".." + Path.DirectorySeparatorChar) || !tokenExists)
                    token = new PathParser(Environment.CurrentDirectory).ToAbsolute(arguments.WatchToken);
                else
                    token = arguments.WatchToken;
            } else {
                token = Environment.CurrentDirectory;
            }
            var watchDirectory = token;
            if (File.Exists(token))
                watchDirectory = Path.GetDirectoryName(token);
            BootStrapper.Configure();
            Debug.EnableLogging(new ConsoleWriter());
            BootStrapper.Container
                .Register(
                    Component.For<IMessageProxy>()
                        .Forward<IRunFeedbackView>()
                        .Forward<IInformationFeedbackView>()
                        .Forward<IConsumerOf<AbortMessage>>()
                        .ImplementedBy<MessageProxy>().LifeStyle.Singleton);

            var proxy = BootStrapper.Services.Locate<IMessageProxy>();
            using (var server = new MessageEndpoint(watchDirectory, createHandlers(), proxy)) {
                proxy.SetMessageForwarder(server);
                BootStrapper.Services.Locate<IRunResultCache>().EnabledDeltas();
                BootStrapper.InitializeCache(token);
                using (var watcher = BootStrapper.Services.Locate<IDirectoryWatcher>())
                {
                    if (arguments.ConfigurationLocation != null) {
                        var configurationLocation = arguments.ConfigurationLocation;
                        if (Directory.Exists(Path.Combine(token, configurationLocation)))
                            configurationLocation = Path.Combine(token, configurationLocation);
                        watcher.LocalConfigurationIsLocatedAt(configurationLocation);
                    }
                    watcher.Watch(token);

                    while (!exit && server.IsAlive) {
                        Thread.Sleep(100);
                    }
                    Console.WriteLine("exiting");
                }
                Console.WriteLine("shutting down");
                BootStrapper.ShutDown();
                Console.WriteLine("disposing server");
            }
            Console.WriteLine("done");
        }

        static List<IHandler> createHandlers() {
            var launcher = BootStrapper.Services.Locate<IApplicatonLauncher>();
            var bus = BootStrapper.Services.Locate<IMessageBus>();
            var cache = BootStrapper.Services.Locate<ICache>();
            var handlers = new List<IHandler>();
            handlers.Add(new RunHandler(bus));
            handlers.Add(new GoToHandler(launcher));
            handlers.Add(new TriggerRunHandler(cache, bus));
            handlers.Add(new ShutdownHandler());
            handlers.Add(new RunItemHandler());
            handlers.Add(new StatusHandler());

            return handlers;
        }
    }
}

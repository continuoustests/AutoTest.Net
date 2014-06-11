using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AutoTest.Core.Configuration;
using System.Reflection;
using Castle.MicroKernel.Registration;
using System.IO;
using AutoTest.Core.Messaging;
using AutoTest.Core.FileSystem;
using AutoTest.Messages;
using AutoTest.UI;
using AutoTest.Core.Presenters;
using AutoTest.Core.Caching.RunResultCache;

namespace AutoTest.WinForms
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            tryStartApplication(args);
        }

        private static void  tryStartApplication(string[] args)
		{
			try
			{
				if (userWantedCommandLineHelpPrinted(args))
					return;
                var arguments = getPossibleCommandArgs(args);
                string directoryToWatch = null; 
                if (arguments.WatchToken != null) {
                    var tokenExists = Directory.Exists(arguments.WatchToken) || File.Exists(arguments.WatchToken);
                    if (arguments.WatchToken.Contains(".." + Path.DirectorySeparatorChar) || !tokenExists)
                        directoryToWatch = new PathParser(Environment.CurrentDirectory).ToAbsolute(arguments.WatchToken);
                    else
                        directoryToWatch = arguments.WatchToken;
                }
                if ((directoryToWatch = ConfigureApplication(directoryToWatch)) == null)
                    return;
        	    var overviewForm = BootStrapper.Services.Locate<IOverviewForm>();
                overviewForm.SetWatchDirectory(directoryToWatch);
			    notifyOnLoggingSetup();

                using (var watcher = BootStrapper.Services.Locate<IDirectoryWatcher>())
                {
                    if (arguments.ConfigurationLocation != null) {
                        var configurationLocation = arguments.ConfigurationLocation;
                        if (Directory.Exists(Path.Combine(directoryToWatch, configurationLocation)))
                            configurationLocation = Path.Combine(directoryToWatch, configurationLocation);
                        watcher.LocalConfigurationIsLocatedAt(configurationLocation);
                    }
                    watcher.Watch(directoryToWatch);
                    var proxy = BootStrapper.Services.Locate<IMessageProxy>();
                    proxy.SetMessageForwarder(overviewForm.Form);
        	        Application.Run(overviewForm.Form);
                }
        	    BootStrapper.ShutDown();
			}
			catch (Exception exception)
			{
				logException(exception);
			}
		}

		private static bool userWantedCommandLineHelpPrinted(string[] args)
		{
			if (args.Length != 1)
				return false;
			if (args[0] != "--help" && args[0] != "-help" && args[0] != "/help")
				return false;
			writeConsoleUsage();
			return true;
		}
		
		private static void writeConsoleUsage()
		{
			Console.WriteLine("AutoTest.WinForms.exe command line arguments");
			Console.WriteLine("");
			Console.WriteLine("To specify watch directory on startup you can type:");
			Console.WriteLine("\tAutoTest.WinForms.exe [WATCH_DIRECTORY] [--local-config-location=/path]");
		}

        private static Arguments getPossibleCommandArgs(string[] args)
        {
            if (args == null)
                return null;
            return ArgumentParser.Parse(args);
        }

		private static void logException (Exception exception)
		{
			var file = Path.Combine(PathParsing.GetRootDirectory(), "panic.dump");
			using (var writer = new StreamWriter(file))
			{
				writeException(writer, exception);
			}
		}
		
		private static void writeException(StreamWriter writer, Exception exception)
		{
			writer.WriteLine(string.Format("Message: {0}", exception.Message));
			writer.WriteLine("Stack trace:");
			writer.WriteLine(exception.StackTrace);
			if (exception.InnerException != null)
			{
				writer.WriteLine("Inner exception");
				writer.WriteLine("");
				writeException(writer, exception.InnerException);
			}
		}

        private static string ConfigureApplication(string watchDirectory)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bootstrapApplication();
            if (watchDirectory == null)
            {
                watchDirectory = getWatchDirectory();
                if (watchDirectory == null)
                    return null;
            }
            BootStrapper.Services
                .Locate<IRunResultCache>().EnabledDeltas();
            BootStrapper.InitializeCache(watchDirectory);
            return watchDirectory;
        }

        private static string getWatchDirectory()
        {
            var directoryPicker = BootStrapper.Services.Locate<IWatchDirectoryPicker>();
            if (directoryPicker.ShowDialog() == DialogResult.Cancel)
                return null;
            return directoryPicker.DirectoryToWatch;
        }

        public static void bootstrapApplication()
        {
            BootStrapper.Configure();
            BootStrapper.Container
                .Register(Component.For<IMessageProxy>()
                                    .Forward<IRunFeedbackView>()
                                    .Forward<IInformationFeedbackView>()
                                    .Forward<IConsumerOf<AbortMessage>>()
                                    .ImplementedBy<MessageProxy>().LifeStyle.Singleton)
                .Register(Component.For<IOverviewForm>().ImplementedBy<FeedbackForm>())
                .Register(Component.For<IInformationForm>().ImplementedBy<InformationForm>())
                .Register(Component.For<IWatchDirectoryPicker>().ImplementedBy<WatchDirectoryPickerForm>());
        }
		
		private static void notifyOnLoggingSetup()
		{
			var bus = BootStrapper.Services.Locate<IMessageBus>();
			bus.Publish<InformationMessage>(new InformationMessage("Debugging enabled"));
		}
    }
}

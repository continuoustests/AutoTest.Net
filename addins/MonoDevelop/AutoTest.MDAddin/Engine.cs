// Copyright (c) 2013, Eberhard Beilharz
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).
using System;
using System.IO;
using Castle.MicroKernel.Registration;
using AutoTest.Core.Configuration;
using AutoTest.Core.Presenters;
using AutoTest.Core.Messaging;
using AutoTest.Messages;
using AutoTest.Core.Caching.RunResultCache;
using AutoTest.MDAddin.Listeners;
using AutoTest.Core.FileSystem;
using AutoTest.Core.DebugLog;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using AutoTest.Core.BuildRunners;

namespace AutoTest.MDAddin
{
	public class Engine
	{
		private string m_WatchToken;
		private ContinuousTestsPad m_Pad;
		private string m_ConfiguredCustomOutput;
		private IDirectoryWatcher m_Watcher;

		public Engine(Pad pad)
		{
			if (pad == null || pad.Content == null)
				return;
			SetPad(pad.Content as ContinuousTestsPad);
		}

		internal void SetPad(ContinuousTestsPad pad)
		{
			if (pad == null)
				return;
			m_Pad = pad;
		}

		public ContinuousTestsPad Pad { get { return m_Pad; }}

		public void BootStrap(string watchToken)
		{
			m_WatchToken = watchToken;

			BootStrapper.SetBuildConfiguration(new BuildConfiguration(OptimisticBuildStrategy));
			BootStrapper.Configure(new LocalAppDataConfigurationLocator("AutoTest.config.template.MD"));
			BootStrapper.Container
				.Register(Component.For<IMessageProxy>()
				          .Forward<IRunFeedbackView>()
				          .Forward<IInformationFeedbackView>()
				          .Forward<IConsumerOf<AbortMessage>>()
				          .ImplementedBy<MessageProxy>().LifeStyle.Singleton);

			BootStrapper.Services
				.Locate<IRunResultCache>().EnabledDeltas();
			BootStrapper.InitializeCache(m_WatchToken);
			BootStrapper.Services
				.Locate<IMessageProxy>()
					.SetMessageForwarder(new FeedbackListener(m_Pad));

			m_ConfiguredCustomOutput = BootStrapper.Services.Locate<IConfiguration>().CustomOutputPath;
			m_Watcher = BootStrapper.Services.Locate<IDirectoryWatcher>();
			m_Watcher.Watch(m_WatchToken);
			//_window.DebugTest += new EventHandler<UI.DebugTestArgs>(_window_DebugTest);
			SetCustomOutputPath();
		}

		public void EnableOptimisticBuildStrategy(bool enable)
		{
			if (enable)
				BootStrapper.Services.Locate<BuildConfiguration>().SetOptimisticBuildStrategy(OptimisticBuildStrategy);
			else
				BootStrapper.Services.Locate<BuildConfiguration>().SetOptimisticBuildStrategy(null);
		}

		private static bool OptimisticBuildStrategy(string file, string filePrev)
		{
			return true;
		}
	
		public bool IsRunning
		{
			get
			{
				if (m_Watcher == null)
					return false;
				return !m_Watcher.IsPaused;
			}
		}
		private void SetCustomOutputPath()
		{
			if (!IsRunning || string.IsNullOrEmpty(m_ConfiguredCustomOutput))
				SetCustomOutputPath(Path.Combine(IdeApp.ProjectOperations.CurrentSelectedSolution.BaseDirectory.ToString(), Path.Combine("bin", "AutoTest.Net")));
			else
				SetCustomOutputPath(m_ConfiguredCustomOutput);
		}

		public void SetCustomOutputPath(string newPath)
		{
			var config = BootStrapper.Services.Locate<IConfiguration>();
			if (config.CustomOutputPath.Equals(newPath))
				return;

			Debug.WriteDebug("Setting custom output folder to " + newPath);
			config.SetCustomOutputPath(newPath);
		}

		public void Pause()
		{
			m_Watcher.Pause();
			SetCustomOutputPath();
			m_Pad.SetText("Engine is paused and will not detect changes");
			m_Pad.SetIcon(true);
		}

		public void Resume()
		{
			if (m_Watcher.IsPaused)
				m_Watcher.Resume();
			SetCustomOutputPath();
			m_Pad.SetText("Engine is running and waiting for changes");
			m_Pad.SetIcon(false);
		}

		public void Shutdown()
		{
			//_window.DebugTest -= _window_DebugTest;
			m_Pad.SetText("Engine is paused and will not detect changes");
			m_Pad.SetIcon(true);

			m_Watcher.Dispose();
			BootStrapper.ShutDown();
		}

	}
}


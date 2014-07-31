// Copyright (c) 2013, Eberhard Beilharz
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).
using System;
using System.IO;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using AutoTest.Core.Configuration;
using AutoTest.Core.Messaging;

namespace AutoTest.MDAddin
{
	public class Startup: CommandHandler
	{
		private string m_WatchToken;
		private static Engine m_Engine;

		internal static Engine Engine { get { return m_Engine; }}

		protected override void Run()
		{
			IdeApp.ProjectOperations.CurrentSelectedSolutionChanged += OnCurrentSelectedSolutionChanged;
			IdeApp.Exiting += OnExiting;
		}

		private void OnCurrentSelectedSolutionChanged (object sender, SolutionEventArgs e)
		{
			if (e.Solution == null)
				return;

			m_WatchToken = e.Solution.FileName;
			var pad = IdeApp.Workbench.GetPad<ContinuousTestsPad>();
			if (m_Engine != null)
				m_Engine.Shutdown();

			m_Engine = new Engine(pad);
			m_Engine.BootStrap(m_WatchToken);

			if (m_Engine.Pad != null)
				m_Engine.Pad.Clear();
		}

		private void OnExiting(object sender, ExitEventArgs args)
		{
			if (m_Engine != null)
			{
				m_Engine.Shutdown();
				m_Engine = null;
			}
		}
	}
}


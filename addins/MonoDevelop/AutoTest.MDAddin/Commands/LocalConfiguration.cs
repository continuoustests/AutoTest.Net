// Copyright (c) 2013, Eberhard Beilharz
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).
using System;
using System.IO;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;

namespace AutoTest.MDAddin.Commands
{
	public class LocalConfiguration: CommandHandler
	{
		protected override void Run()
		{
			var configFile = LocalConfigFile;
			if (File.Exists(configFile))
				IdeApp.Workbench.OpenDocument(configFile);
		}

		protected override void Update(CommandInfo info)
		{
			info.Enabled = File.Exists(LocalConfigFile);
		}

		private string LocalConfigFile
		{
			get
			{
				if (IdeApp.ProjectOperations.CurrentSelectedSolution != null && IdeApp.ProjectOperations.CurrentSelectedSolution.BaseDirectory != null)
					return Path.Combine(IdeApp.ProjectOperations.CurrentSelectedSolution.BaseDirectory.ToString(), "AutoTest.config");
				return string.Empty;
			}
		}
	}
}


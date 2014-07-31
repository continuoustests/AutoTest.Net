// Copyright (c) 2013, Eberhard Beilharz
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).
using System;
using System.IO;
using System.Reflection;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;

namespace AutoTest.MDAddin.Commands
{
	public class LocalConfiguration: CommandHandler
	{
		protected override void Run()
		{
			var configFile = LocalConfigFile;
			if (string.IsNullOrEmpty(configFile))
				return;

			if (File.Exists(configFile))
				IdeApp.Workbench.OpenDocument(configFile);
			else
			{
				var assembly = Assembly.GetExecutingAssembly();
				var stream = assembly.GetManifestResourceStream("AutoTest.MDAddin.Resources.AutoTest.config.template.MD");
				IdeApp.Workbench.NewDocument(LocalConfigFile, "application/xml", stream);
			}
		}

		protected override void Update(CommandInfo info)
		{
			info.Enabled = (IdeApp.ProjectOperations.CurrentSelectedSolution != null);
		}

		private string LocalConfigFile
		{
			get
			{
				if (IdeApp.ProjectOperations.CurrentSelectedSolution != null)
					return Path.Combine(IdeApp.ProjectOperations.CurrentSelectedSolution.BaseDirectory.ToString(), "AutoTest.config");
				return string.Empty;
			}
		}
	}
}


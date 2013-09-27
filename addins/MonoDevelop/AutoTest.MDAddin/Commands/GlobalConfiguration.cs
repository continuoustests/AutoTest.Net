// Copyright (c) 2013, Eberhard Beilharz
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).
using System;
using System.IO;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;

namespace AutoTest.MDAddin.Commands
{
	public class GlobalConfiguration: CommandHandler
	{
		protected override void Run()
		{
			var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			var atDir = Path.Combine(appData, "AutoTest.Net");
			var configFile = Path.Combine(atDir, "AutoTest.config");
			if (File.Exists(configFile))
				IdeApp.Workbench.OpenDocument(configFile);
		}
	}
}


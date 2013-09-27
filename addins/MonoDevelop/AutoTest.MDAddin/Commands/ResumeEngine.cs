// Copyright (c) 2013, Eberhard Beilharz
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).
using System;
using MonoDevelop.Components.Commands;

namespace AutoTest.MDAddin.Commands
{
	public class ResumeEngine: CommandHandler
	{
		protected override void Run()
		{
			if (Startup.Engine == null)
				return;

			Startup.Engine.Resume();
		}

		protected override void Update(CommandInfo info)
		{
			info.Enabled = Startup.Engine != null && !Startup.Engine.IsRunning;
		}
	}
}


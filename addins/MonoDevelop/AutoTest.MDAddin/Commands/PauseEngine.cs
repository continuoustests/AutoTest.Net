// Copyright (c) 2013, Eberhard Beilharz
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).
using System;
using MonoDevelop.Components.Commands;

namespace AutoTest.MDAddin.Commands
{
	public class PauseEngine: CommandHandler
	{
		protected override void Run()
		{
			if (Startup.Engine == null)
				return;

			Startup.Engine.Pause();
		}

		protected override void Update(CommandInfo info)
		{
			info.Enabled = Startup.Engine != null && Startup.Engine.IsRunning;
		}
	}
}


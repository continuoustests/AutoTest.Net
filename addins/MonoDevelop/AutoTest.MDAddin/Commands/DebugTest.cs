// Copyright (c) 2013, Eberhard Beilharz
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).
using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;

namespace AutoTest.MDAddin.Commands
{
	public class DebugTest: CommandHandler
	{
		protected override void Run()
		{
			var pad = IdeApp.Workbench.GetPad<ContinuousTestsPad>();
			if (pad != null && pad.Content != null)
			{
				((ContinuousTestsPad)pad.Content).OnDebugTest();
			}
		}

		protected override void Update(CommandInfo info)
		{
			var pad = IdeApp.Workbench.GetPad<ContinuousTestsPad>();
			if (pad != null && pad.Content != null)
			{
				((ContinuousTestsPad)pad.Content).OnUpdateDebugTest(info);
			}
		}
	}
}


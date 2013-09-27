// Copyright (c) 2013, Eberhard Beilharz
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using MonoDevelop.Core.Execution;
using AutoTest.Core.TestRunners;
using AutoTest.Core.TestRunners.TestRunners;
using MonoDevelop.Ide;

namespace AutoTest.MDAddin
{
	public class NUnitDebugTestRunner: NUnitTestRunner
	{
		private IExecutionHandler m_ExecutionHandler;

		public NUnitDebugTestRunner(IExecutionHandler executionHandler, ITestRunner other): base(other as NUnitTestRunner)
		{
			m_ExecutionHandler = executionHandler;
		}

		protected override void StartProcess(ProcessStartInfo startInfo)
		{
			if (m_ExecutionHandler != null)
			{
				var command = new DotNetExecutionCommand(startInfo.FileName, startInfo.Arguments, startInfo.WorkingDirectory);
				if (command.EnvironmentVariables == null)
					command.EnvironmentVariables = new Dictionary<string, string>();
				foreach (DictionaryEntry item in startInfo.EnvironmentVariables)
					command.EnvironmentVariables.Add((string)item.Key, (string)item.Value);
				m_ExecutionHandler.Execute(command, (IConsole)IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor());
			}
			else
				base.StartProcess(startInfo);
		}
	}
}


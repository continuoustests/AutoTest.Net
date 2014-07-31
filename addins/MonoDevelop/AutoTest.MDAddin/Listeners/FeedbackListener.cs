// Copyright (c) 2013, Eberhard Beilharz.
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using AutoTest.Messages;
using AutoTest.UI;
using AutoTest.MDAddin;
using System.Collections.Generic;
using Gtk;

namespace AutoTest.MDAddin.Listeners
{
	class FeedbackListener : IMessageForwarder
	{
		private readonly SynchronizationContext m_syncContext;
		private readonly object m_messagLock = new object();
		private IFeedback UI { get; set; }
		private bool IsRunning { get; set; }
		private bool ShowFailing = true;
		private bool ShowErrors = true;
		private bool ShowWarnings = true;
		private bool ShowIgnored = true;

		public FeedbackListener(IFeedback feedback)
		{
			m_syncContext = AsyncOperationManager.SynchronizationContext;
			UI = feedback;
		}

		public void Forward(object message)
		{
			m_syncContext.Post(x =>
			                   {
				lock (m_messagLock)
				{
					ConsumeMessage(x as IMessage);
				}
			}, message);
		}

		private void ConsumeMessage(IMessage message)
		{
			if (message.GetType() == typeof(CacheMessages))
				Handle((CacheMessages)message);
			else if (message.GetType() == typeof(LiveTestStatusMessage))
				Handle((LiveTestStatusMessage)message);
			else if (message.GetType() == typeof(ErrorMessage))
				UI.Store.AppendValues("Error", ((ErrorMessage)message).Error, "");
			else if (message.GetType() == typeof(WarningMessage))
				UI.Store.AppendValues("Warning", ((WarningMessage)message).Warning, "");
			else if (message.GetType() == typeof(InformationMessage))
				UI.Store.AppendValues("Info", ((InformationMessage)message).Message, "");
			else if (message.GetType() == typeof(RunStartedMessage))
				RunStarted("Processing changes");
			else if (message.GetType() == typeof(RunFinishedMessage))
				RunFinished((RunFinishedMessage)message);
			else if (message.GetType() == typeof(RunInformationMessage))
				RunInformationMessage((RunInformationMessage)message);
			else if (message.GetType() == typeof(TestRunMessage) || message.GetType() == typeof(FileChangeMessage))
			{
				// ignore. This gets handled as part of CacheMessages
			}
			else if (message.GetType() == typeof(BuildRunMessage))
			{
				// ignore
			}
			else
				UI.Store.AppendValues("Unknown", message.ToString(), "");
		}

		private void Handle(CacheMessages cache)
		{
			var buildMsgsToRemove = new List<CacheBuildMessage>();
			var testMsgsToRemove = new List<CacheTestMessage>();

			if (ShowErrors)
			{
				foreach (var error in cache.ErrorsToAdd)
					AddFeedbackItem("Build error", FormatBuildResult(error), "red", error);
				buildMsgsToRemove.AddRange(cache.ErrorsToRemove);
			}

			if (ShowFailing)
			{
				foreach (var failed in cache.FailedToAdd)
					AddFeedbackItem("Test failed", FormatTestResult(failed), "red", failed, failed.Test.Message);
				testMsgsToRemove.AddRange(cache.TestsToRemove);
			}

			if (ShowWarnings)
			{
				foreach (var warning in cache.WarningsToAdd)
					AddFeedbackItem("Build warning", FormatBuildResult(warning), "black", warning);
				buildMsgsToRemove.AddRange(cache.WarningsToRemove);
			}

			if (ShowIgnored)
			{
				foreach (var ignored in cache.IgnoredToAdd)
					AddFeedbackItem("Test ignored", FormatTestResult(ignored), "black", ignored);
			}

			TreeIter iter;
			bool cont;
			cont = UI.Store.GetIterFirst(out iter);
			while (cont)
			{
				var tag = UI.Store.GetValue(iter, 3);
				var tagBuildMsg = tag as CacheBuildMessage;
				var tagTestMsg = tag as CacheTestMessage;
				if ((tagBuildMsg != null && buildMsgsToRemove.Contains(tagBuildMsg)) ||
					(tagTestMsg != null && testMsgsToRemove.Contains(tagTestMsg)))
				{
					cont = UI.Store.Remove(ref iter);
				}
				else
					cont = UI.Store.IterNext(ref iter);
			}
		}

		private void Handle(LiveTestStatusMessage liveStatus)
		{
			if (!IsRunning)
				return;

			var ofCount = liveStatus.TotalNumberOfTests > 0 ? string.Format(" of {0}", liveStatus.TotalNumberOfTests) : "";
			var testName = liveStatus.CurrentTest;
			if (testName.Trim().Length > 0)
				testName += " in ";
			UI.SetText(string.Format("testing {3}{0} ({1}{2} tests completed)", Path.GetFileNameWithoutExtension(liveStatus.CurrentAssembly), liveStatus.TestsCompleted, ofCount, testName));

			//			if (ShowFailing)
			//			{
			//				foreach (var test in liveStatus.FailedButNowPassingTests)
			//				{
			//					var testItem = new CacheTestMessage(test.Assembly, test.Test);
			//					foreach (ListViewItem item in listViewFeedback.Items)
			//					{
			//						if (item.Tag.GetType() != typeof(CacheTestMessage))
			//							continue;
			//						var itm = (CacheTestMessage)item.Tag;
			//						if (isTheSameTestAs(itm, testItem))
			//						{
			//							item.Remove();
			//							break;
			//						}
			//					}
			//				}
			//
			//				object selected = null;
			//				if (listViewFeedback.SelectedItems.Count == 1)
			//					selected = listViewFeedback.SelectedItems[0].Tag;
			//				foreach (var test in liveStatus.FailedTests)
			//				{
			//					var testItem = new CacheTestMessage(test.Assembly, test.Test);
			//					ListViewItem toRemove = null;
			//					foreach (ListViewItem item in listViewFeedback.Items)
			//					{
			//						if (item.Tag.GetType() != typeof(CacheTestMessage))
			//							continue;
			//						var itm = (CacheTestMessage)item.Tag;
			//						if (isTheSameTestAs(itm, testItem))
			//						{
			//							toRemove = item;
			//							break;
			//						}
			//					}
			//					int index = toRemove == null ? 0 : toRemove.Index;
			//					if (toRemove != null)
			//						toRemove.Remove();
			//					addFeedbackItem("Test failed", formatTestResult(testItem), Color.Red, testItem, selected, index);
			//				}
			//			}
		}

		private void AddFeedbackItem(string type, string message, string color, object tag, string toolTip = "")
		{
			UI.Store.AppendValues(string.Format("<span color='{0}'>{1}</span>", color, type), string.Format("<span color='{0}'>{1}</span>", color, message), toolTip, tag);
		}

		private static string FormatBuildResult(CacheBuildMessage item)
		{
			return string.Format("{0}, {1}, line {2}:{3}", item.BuildItem.ErrorMessage, item.BuildItem.File,
				item.BuildItem.LineNumber, item.BuildItem.LinePosition);
		}

		private static string FormatTestResult(CacheTestMessage item)
		{
			return string.Format("{1} ({0})", item.Test.Runner, item.Test.DisplayName);
		}

		private void RunInformationMessage(RunInformationMessage message)
		{
			if (!IsRunning)
				return;
			var text = string.Empty;
			switch (message.Type)
			{
				case InformationType.Build:
					text = string.Format("building {0}", Path.GetFileName(message.Project));
					break;
				case InformationType.TestRun:
					text = "testing...";
					break;
				case InformationType.PreProcessing:
					text = "locating affected tests";
					break;
			}
			if (!string.IsNullOrEmpty(text))
			{
				UI.SetProgress(ImageStates.Progress, text);
				UI.SetText(text);
			}
		}

		private void CleanMessages()
		{
			bool cont;
			TreeIter iter;
			cont = UI.Store.GetIterFirst(out iter);
			while (cont)
			{
				var type = UI.Store.GetValue(iter, 0) as string;
				if (type == "Info")
					cont = UI.Store.Remove(ref iter);
				else
					cont = UI.Store.IterNext(ref iter);
			}
		}

		private void RunStarted(string text)
		{
			CleanMessages();
			UI.GenerateSummary(null);
			UI.SetText(text);
			UI.SetProgress(ImageStates.Progress, "processing changes...");
			IsRunning = true;
		}

		private void RunFinished(RunFinishedMessage message)
		{
			if (message.Report.Aborted)
			{
				UI.SetProgress(ImageStates.None, "");
				UI.Store.AppendValues("Info", "last build/test run was aborted", "");
			}
			else
			{
				var info = GetRunFinishedInfo(message);
				var runType = info.Succeeded ? RunMessageType.Succeeded : RunMessageType.Failed;
				UI.SetProgress(runType == RunMessageType.Succeeded ? ImageStates.Green : ImageStates.Red, "");
				UI.SetMarkupText(info.Text);
				UI.GenerateSummary(info.Report);
			}
			IsRunning = false;
		}

		private static RunFinishedInfo GetRunFinishedInfo(RunFinishedMessage message)
		{
			var report = message.Report;
			var text = string.Format(
				"Ran {0} build(s) ({1} succeeded, {2} failed) and {3} test(s) ({4} passed, {5} failed, {6} ignored)",
				report.NumberOfProjectsBuilt,
				report.NumberOfBuildsSucceeded,
				report.NumberOfBuildsFailed,
				report.NumberOfTestsRan,
				report.NumberOfTestsPassed,
				report.NumberOfTestsFailed,
				report.NumberOfTestsIgnored);
			var succeeded = !(report.NumberOfBuildsFailed > 0 || report.NumberOfTestsFailed > 0);
			if (report.NumberOfBuildsFailed > 0)
				text = "<span foreground='red'>" + text + "</span>";
			else if (report.NumberOfTestsFailed > 0)
				text = "<span background='yellow'>" + text + "</span>";
			else
				text = "<span foreground='green'>" + text + "</span>";
			return new RunFinishedInfo(text, succeeded, report);
		}
	}
}


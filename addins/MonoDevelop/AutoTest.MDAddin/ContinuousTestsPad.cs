// Copyright (c) 2013, Eberhard Beilharz
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).
using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Castle.MicroKernel.Registration;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Components.Docking;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using AutoTest.Core.Messaging;
using AutoTest.Core.DebugLog;
using AutoTest.UI;
using AutoTest.Messages;
using AutoTest.Core.Configuration;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.NUnit;
using Mono.Addins;
using AutoTest.MDAddin.Commands;
using AutoTest.Core.Messaging.MessageConsumers;
using AutoTest.Core.TestRunners;
using AutoTest.Core.TestRunners.TestRunners;

namespace AutoTest.MDAddin
{
	public class ContinuousTestsPad: IPadContent, IFeedback
	{
		private HPaned m_Control;
		private CompactScrolledWindow m_ScrolledWindow;
		private TreeView m_TreeView;

		private ToggleButton m_btnInfo;
		private ToggleButton m_btnBuildError;
		private ToggleButton m_btnBuildWarning;
		private ToggleButton m_btnTestFailure;
		private ToggleButton m_btnTestIgnored;

		private ListStore m_Store;
		private TreeModelFilter m_Filter;

		private ToggleButton m_Icon;
		private Label m_Status;

		private Gtk.Image m_ImageAbort;
		private Gtk.Image m_ImageFail;
		private Gtk.Image m_ImageSuccess;
		private Gtk.Image m_ImageProgress;
		private Gtk.Image m_ImagePaused;

		private bool m_fExit;

		public ContinuousTestsPad()
		{
			m_Store = new ListStore(typeof(string), typeof(string), typeof(string), typeof(object));

			if (Startup.Engine != null)
			{
				Startup.Engine.SetPad(this);
				Clear();
			}
		}

		private bool FilterTaskTypes(TreeModel model, TreeIter iter)
		{
			try
			{
				var type = m_Store.GetValue(iter, 0) as string;
				if (type == null)
					return true;
				return (type.Contains("Info") && m_btnInfo.Active) ||
					(type.Contains("Build error") && m_btnBuildError.Active) ||
					(type.Contains("Build warning") && m_btnBuildWarning.Active) ||
					(type.Contains("Test failed") && m_btnTestFailure.Active) ||
					(type.Contains("Test ignored") && m_btnTestIgnored.Active);
			}
			catch
			{
				//Not yet fully added
				return false;
			}
		}

		private static int TypeIterSort(TreeModel model, TreeIter a, TreeIter z)
		{
			var typeA = model.GetValue(a, 0) as string;
			var typeZ = model.GetValue(z, 0) as string;

			if (typeA == null && typeZ == null)
				return 0;

			if (typeZ == "Test ignored" && typeA == "Test failed")
				return -1;

			return typeZ.CompareTo(typeA);
		}

		private void CreateControl()
		{
			m_Control = new HPaned();
			m_TreeView = new TreeView();

			m_Filter = new TreeModelFilter(m_Store, null);
			m_Filter.VisibleFunc = FilterTaskTypes;

			var sorter = new TreeModelSort(m_Filter);
			sorter.SetSortFunc(0, TypeIterSort);

			var typeColumn = new TreeViewColumn();
			typeColumn.Title = "Type";
			var typeRenderer = new CellRendererText();
			typeColumn.PackStart(typeRenderer, true);
			typeColumn.AddAttribute(typeRenderer, "markup", 0);

			var messageColumn = new TreeViewColumn();
			messageColumn.Title = "Message";
			var messageRenderer = new CellRendererText();
			messageColumn.PackStart(messageRenderer, true);
			messageColumn.AddAttribute(messageRenderer, "markup", 1);

			m_TreeView.AppendColumn(typeColumn);
			m_TreeView.AppendColumn(messageColumn);
			m_TreeView.HasTooltip = true;
			m_TreeView.HeadersVisible = true;
			m_TreeView.QueryTooltip += HandleQueryTooltip;
			m_TreeView.SizeAllocated += HandleSizeAllocated;
			m_TreeView.RowActivated += HandleRowActivated;
			m_TreeView.ButtonPressEvent += HandleTreeViewButtonPress;

			m_ScrolledWindow = new CompactScrolledWindow();
			m_ScrolledWindow.ShadowType = ShadowType.None;
			m_ScrolledWindow.Add(m_TreeView);

			m_Control.Add(m_ScrolledWindow);

			Control.ShowAll();

			m_TreeView.Model = sorter;
		}

		[GLib.ConnectBefore]
		private void HandleTreeViewButtonPress(object o, ButtonPressEventArgs args)
		{
//			if (args.Event.Button == 3)
//			{
//				IdeApp.CommandService.ShowContextMenu((Widget)o, args.Event, "/AutoTest/MDAddin/ContextMenu/ContinuousTestsPad");
//				args.RetVal = true;
//			}
		}

		private void HandleRowActivated(object o, RowActivatedArgs args)
		{
			TreeIter iter;
			if (m_TreeView.Model.GetIter(out iter, args.Path))
			{
				var obj = m_TreeView.Model.GetValue(iter, 3);
				var testMsg = obj as CacheTestMessage;
				if (testMsg != null && testMsg.Test.StackTrace.Length > 0)
				{
					var stackLine = testMsg.Test.StackTrace[0];

					IdeApp.Workbench.OpenDocument(stackLine.File, stackLine.LineNumber, 1);
					return;
				}
				var buildMsg = obj as CacheBuildMessage;
				if (buildMsg != null)
				{
					var doc = IdeApp.Workbench.OpenDocument(buildMsg.BuildItem.File,
						buildMsg.BuildItem.LineNumber, buildMsg.BuildItem.LinePosition);
					// The column might be off if the text contains tabs.
					var lineText = doc.Editor.GetLineText(buildMsg.BuildItem.LineNumber);
					// we use two indices: column is the real column number in the text that
					// might include tab characters. iBuild is the counter where tab characters
					// are replaced with 8 spaces.
					int column = 0;
					for (int iBuild = 0; iBuild < buildMsg.BuildItem.LinePosition; iBuild++, column++)
					{
						if (lineText[column] == '\t')
							iBuild += 7; // tab width of 8
					}
					doc = IdeApp.Workbench.OpenDocument(buildMsg.BuildItem.File,
						buildMsg.BuildItem.LineNumber, column);
					doc.Select();
					return;
				}
			}
		}

		private void HandleSizeAllocated(object o, SizeAllocatedArgs args)
		{
			var vadj = m_ScrolledWindow.Vadjustment;
			vadj.Value = vadj.Upper - vadj.PageSize;
		}

		private void HandleQueryTooltip (object o, QueryTooltipArgs args)
		{
			int x, y;
			m_TreeView.ConvertWidgetToBinWindowCoords(args.X, args.Y, out x, out y);

			TreePath treePath;
			TreeViewColumn column;
			string toolTip = null;
			if (m_TreeView.GetPathAtPos(x, y, out treePath, out column))
			{
				TreeIter iter;
				if (m_TreeView.Model.GetIter(out iter, treePath))
				{
					// display column 2 (test failure msg) if it exists, otherwise
					// full text of column 1 (which might be partly hidden if it is long)
					var obj = m_TreeView.Model.GetValue(iter, 2) as string;
					if (string.IsNullOrEmpty(obj))
						obj = m_TreeView.Model.GetValue(iter, 1) as string;
					if (!string.IsNullOrEmpty(obj))
					{
						var regex = new Regex("<span color='[^']+'>([^<]+)</span>");
						toolTip = regex.Replace(obj, "$1");
					}
				}
			}
			args.Tooltip.Markup = toolTip;
			args.RetVal = !string.IsNullOrEmpty(toolTip);
		}

		public void Clear()
		{
			m_Store.Clear();
		}

		#region IFeedback
		public void SetText(string status)
		{
			m_Status.Text = status;
		}

		public void SetMarkupText(string status)
		{
			m_Status.Markup = status;
		}

		public void SetIcon(bool paused)
		{
			if (paused)
				m_Icon.Image = m_ImagePaused;
			else
				m_Icon.Image = m_ImageAbort;
			m_Icon.Image.Show();
		}

		public ListStore Store { get { return m_Store; }}

		public void SetProgress(ImageStates state, string information)
		{
			switch (state)
			{
				case ImageStates.None:
					m_Icon.Image = m_ImageAbort;
					break;
				case ImageStates.Red:
					m_Icon.Image = m_ImageFail;
					break;
				case ImageStates.Green:
					m_Icon.Image = m_ImageSuccess;
					break;
				case ImageStates.Progress:
					m_Icon.Image = m_ImageProgress;
					break;
			}
			m_Icon.ShowNow();
			m_Icon.TooltipText = information;
		}

		public void GenerateSummary(RunReport report)
		{
			if (report == null)
			{
				m_Status.TooltipText = string.Empty;
				return;
			}

			var builder = new SummaryBuilder(report);
			m_Status.TooltipText = builder.Build();
		}

		#endregion

		private Gtk.Image LoadImage(Assembly assembly, string imageName)
		{
			try
			{
				return new Gtk.Image(assembly, imageName);
			}
			catch
			{
				Debug.WriteInfo("Failed to load icon: " + imageName);
			}
			return null;
		}

		#region IPadContent implementation

		public void Initialize(IPadWindow window)
		{
			window.Title = "Continuous Tests";

			DockItemToolbar toolbar = window.GetToolbar (PositionType.Top);

			var assembly = Assembly.GetExecutingAssembly();

			var optimist = new ToggleButton();
			optimist.Active = true;
			optimist.Image = LoadImage(assembly, "AutoTest.MDAddin.Resources.circleOptimist.png");
			optimist.Image.Show();
			optimist.TooltipText = "Use optimistic builds";
			optimist.Clicked += OnOptimistButtonClicked;
			toolbar.Add(optimist);

			m_btnInfo = new ToggleButton();
			m_btnInfo.Active = true;
			m_btnInfo.Image = LoadImage(assembly, "AutoTest.MDAddin.Resources.circleInfo.png");
			m_btnInfo.Image.Show();
			m_btnInfo.TooltipText = "Show Info";
			m_btnInfo.Clicked += OnFilterChanged;
			toolbar.Add(m_btnInfo);

			m_btnBuildError = new ToggleButton();
			m_btnBuildError.Active = true;
			m_btnBuildError.Image = LoadImage(assembly, "AutoTest.MDAddin.Resources.circleBuildError.png");
			m_btnBuildError.Image.Show();
			m_btnBuildError.TooltipText = "Show Build Errors";
			m_btnBuildError.Clicked += OnFilterChanged;
			toolbar.Add(m_btnBuildError);

			m_btnBuildWarning = new ToggleButton();
			m_btnBuildWarning.Active = true;
			m_btnBuildWarning.Image = LoadImage(assembly, "AutoTest.MDAddin.Resources.circleBuildWarning.png");
			m_btnBuildWarning.Image.Show();
			m_btnBuildWarning.TooltipText = "Show Build Warnings";
			m_btnBuildWarning.Clicked += OnFilterChanged;
			toolbar.Add(m_btnBuildWarning);

			m_btnTestFailure = new ToggleButton();
			m_btnTestFailure.Active = true;
			m_btnTestFailure.Image = LoadImage(assembly, "AutoTest.MDAddin.Resources.circleTest.png");
			m_btnTestFailure.Image.Show();
			m_btnTestFailure.Clicked += OnFilterChanged;
			m_btnTestFailure.TooltipText = "Show failed tests";
			toolbar.Add(m_btnTestFailure);

			m_btnTestIgnored = new ToggleButton();
			m_btnTestIgnored.Active = true;
			m_btnTestIgnored.Image = LoadImage(assembly, "AutoTest.MDAddin.Resources.circleTestIgnored.png");
			m_btnTestIgnored.Image.Show();
			m_btnTestIgnored.Clicked += OnFilterChanged;
			m_btnTestIgnored.TooltipText = "Show ignored tests";
			toolbar.Add(m_btnTestIgnored);

			m_ImageAbort = LoadImage(assembly, "AutoTest.MDAddin.Resources.circleAbort.png");
			m_ImageFail = LoadImage(assembly, "AutoTest.MDAddin.Resources.circleFAIL.png");
			m_ImageSuccess = LoadImage(assembly, "AutoTest.MDAddin.Resources.circleWIN.png");
			m_ImageProgress = LoadImage(assembly, "AutoTest.MDAddin.Resources.progress.gif");
			m_ImagePaused = LoadImage(assembly, "AutoTest.MDAddin.Resources.circlePaused.png");

			string labelText = "Engine paused";
			var image = m_ImagePaused;
			if (Startup.Engine != null && Startup.Engine.IsRunning)
			{
				labelText = "Engine is running and waiting for changes";
				image = m_ImageAbort;
			}

			var separator = new HSeparator();
			toolbar.Add(separator);

			m_Icon = new ToggleButton();
			m_Icon.Active = false;
			m_Icon.Image = image;
			m_Icon.Image.Show();
			m_Icon.Clicked += OnIconClicked;
			toolbar.Add(m_Icon);

			m_Status = new Label(labelText);
			toolbar.Add(m_Status);

			toolbar.ShowAll();
		}

		private void OnOptimistButtonClicked(object sender, EventArgs e)
		{
			var button = sender as ToggleButton;
			if (button == null)
				return;

			Startup.Engine.EnableOptimisticBuildStrategy(button.Active);
		}

		private void OnFilterChanged(object sender, EventArgs e)
		{
			m_Filter.Refilter();
		}

		private void OnIconClicked(object sender, EventArgs e)
		{
			if (Startup.Engine.IsRunning)
				Startup.Engine.Pause();
			else
				Startup.Engine.Resume();
		}

		public void RedrawContent()
		{
		}

		public Gtk.Widget Control
		{
			get
			{
				if (m_Control == null)
					CreateControl();
				return m_Control;
			}
		}

		#endregion

		#region IDisposable implementation

		public void Dispose()
		{
			m_fExit = true;
		}

		#endregion

		private void RunSelectedTest(IExecutionHandler mode)
		{
			var test = GetSelectedTest();
			if (test == null)
				return;

			TestRunInfo runInfo = new TestRunInfo(null, test.Assembly);
			runInfo.AddTestsToRun(TestRunner.NUnit, test.Test.DisplayName);

			var nunit = new NUnitDebugTestRunner(mode, BootStrapper.Container.Resolve<ITestRunner>());
			nunit.RunTests(new[] { runInfo }, null, () => {
				return m_fExit; });
		}

		private CacheTestMessage GetSelectedTest()
		{
			TreeIter iter;
			if (m_TreeView.Selection.GetSelected(out iter))
			{
				var obj = m_TreeView.Model.GetValue(iter, 3);
				var c = obj as CacheTestMessage;
				return c;
			}
			return null;
		}

		#region Command handler
		public void OnRunTest()
		{
			RunSelectedTest(null);
		}

		public void OnUpdateRunTest(CommandInfo info)
		{
			info.Enabled = GetSelectedTest() != null;
		}

		internal void OnDebugTest()
		{
#if MD_41
			var debugModeSet = Runtime.ProcessService.GetDebugExecutionMode();
			var mode = debugModeSet.ExecutionModes.First();
			RunSelectedTest(mode.ExecutionHandler);
#endif
		}

		internal void OnUpdateDebugTest(CommandInfo info)
		{
#if MD_41
			OnUpdateRunTest(info);
#else
			info.Enabled = false;
#endif
		}

		#endregion
	}
}


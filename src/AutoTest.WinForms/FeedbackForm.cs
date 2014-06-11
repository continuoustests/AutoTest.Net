using System;
using System.Windows.Forms;
using AutoTest.Core.Launchers;
using AutoTest.Core.Messaging;
using AutoTest.Messages;
using AutoTest.Core.DebugLog;
using AutoTest.UI;

namespace AutoTest.WinForms
{
    public class LabelItembehaviour : IListItemBehaviour
    {
        private Label _label;

        public LabelItembehaviour(Label label) {
            _label = label;
        }

        public int Left { get { return _label.Left; } set { _label.Left = value; } }
        public int Width { get { return _label.Width; } set { _label.Width = value; } }
        public string Name { get { return _label.Name; } }
        public bool Visible { get { return _label.Visible; } set { _label.Visible = value; } }
    }

    public partial class FeedbackForm : Form, IOverviewForm, IMessageForwarder
    {
        private IApplicatonLauncher _launcher;
        private IMessageBus _bus;
        private string _watchToken = null;
        private FeedbackProvider _provider;

        public FeedbackForm(IApplicatonLauncher launcher, IMessageBus bus)
        {
            _launcher = launcher;
            _bus = bus;
            addContextMenues();
            InitializeComponent();
            _provider = new FeedbackProvider(
                new LabelItembehaviour(runFeedback.linkLabelCancelRun),
                new LabelItembehaviour(runFeedback.linkLabelDebugTest),
                new LabelItembehaviour(runFeedback.linkLabelTestDetails),
                new LabelItembehaviour(runFeedback.linkLabelErrorDescription));
            runFeedback.SetFeedbackProvider(_provider);
        }

        private void addContextMenues()
        {
            var keepOnTop = new ToolStripMenuItem("Always On Top");
            keepOnTop.CheckOnClick = true;
            keepOnTop.Click += (sender, eventArgs) => this.TopMost = ((ToolStripMenuItem)sender).Checked;

            var minimal = new ToolStripMenuItem("Hide Title Bar");
            minimal.CheckOnClick = true;
            minimal.Click += (sender, eventArgs) =>
            {
                var isChecked = ((ToolStripMenuItem)sender).Checked;

                this.ControlBox = !isChecked;
                this.ShowIcon = !isChecked;
                this.FormBorderStyle = !isChecked ? FormBorderStyle.FixedSingle : FormBorderStyle.Sizable;
                this.Text = !isChecked ? "AutoTest.Net" : string.Empty;
            };

            ContextMenuStrip = new ContextMenuStrip();
            ContextMenuStrip.Items.Add(keepOnTop);
            ContextMenuStrip.Items.Add(minimal);
        }

        private void runFeedback_GoToReference(object sender, UI.GoToReferenceArgs e)
        {
            _launcher.LaunchEditor(e.Position.File, e.Position.LineNumber, e.Position.Column);
        }

        public void SetWatchDirectory(string directory)
        {
            _watchToken = directory;
        }
        
        public FeedbackForm Form
        {
            get { return this; }
        }

        public void Forward(object message)
        {
            Debug.WriteDetail("Handling {0}", message.GetType());
            _provider.ConsumeMessage(message);
            if (message.GetType().Equals(typeof(ExternalCommandMessage)))
            {
                var commandMessage = (ExternalCommandMessage)message;
				Debug.WriteDebug("Handling external messag: " + commandMessage.Command + " from " + commandMessage.Sender);
                if (commandMessage.Sender == "EditorEngine")
                {
                    var msg = EditorEngineMessage.New(commandMessage.Sender + " " + commandMessage.Command);
					Debug.WriteDebug(" args count is " + msg.Arguments.Count.ToString());
					if (msg.Arguments.Count == 1 &&
						msg.Arguments[0].ToLower() == "shutdown")
					{
						Debug.WriteDebug("Shutting down");
						Close();
					}
                    if (msg.Arguments.Count == 2 &&
						msg.Arguments[0].ToLower() == "autotest.net" &&
						msg.Arguments[1].ToLower() == "setfocus")
                    {
                        Activate();
                        _provider.PrepareForFocus();
                    }
                }
            }
        }

        private void runFeedback_CancelRun(object sender, EventArgs e)
        {
            _bus.Publish(new AbortMessage(""));
        }

        private void FeedbackFormResize(object sender, EventArgs e)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                runFeedback.Width = Width - (runFeedback.Left * 2) - 10;
                runFeedback.Height = Height - (runFeedback.Top * 2) - 30;
            }
            runFeedback.Resize();
        }
    }
}

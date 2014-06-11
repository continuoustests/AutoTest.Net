using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using AutoTest.Core.Messaging;
using AutoTest.Messages;
using AutoTest.UI;
using EnvDTE80;
using AutoTest.VS.Util.Debugger;
using EnvDTE;
using AutoTest.VS.Util.Navigation;

namespace AutoTest.VSAddin
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

    [ProgId("AutoTestNet_FeedbackWindow"), ClassInterface(ClassInterfaceType.AutoDual), Guid("67663444-f874-401c-9e55-053bb0b5bd0d")]
    public partial class FeedbackWindow : UserControl
    {
        private DTE2 _application;
        private IMessageBus _bus;
        private FeedbackProvider _provider;

        public event EventHandler<UI.DebugTestArgs> DebugTest;

        public FeedbackWindow()
        {
            InitializeComponent();
            _provider = new FeedbackProvider(
                new LabelItembehaviour(runFeedback.linkLabelCancelRun),
                new LabelItembehaviour(runFeedback.linkLabelDebugTest),
                new LabelItembehaviour(runFeedback.linkLabelTestDetails),
                new LabelItembehaviour(runFeedback.linkLabelErrorDescription));
            runFeedback.SetFeedbackProvider(_provider);
        }

        public void SetApplication(DTE2 application)
        {
            _application = application;
        }

        public void SetMessageBus(IMessageBus bus)
        {
            _bus = bus;
        }

        public void Consume(object message)
        {
            _provider.ConsumeMessage(message);
        }

        public void SetText(string text)
        {
            _provider.PrintMessage(new UI.RunMessages(UI.RunMessageType.Normal, text));
        }

        private void runFeedback_DebugTest(object sender, UI.DebugTestArgs e)
        {
            if (DebugTest != null)
                DebugTest(sender, e);
        }

        private void runFeedback_GoToReference(object sender, UI.GoToReferenceArgs e)
        {
            try
            {
                var window = _application.OpenFile(EnvDTE.Constants.vsViewKindCode, e.Position.File);
                window.Activate();
                var selection = (TextSelection)_application.ActiveDocument.Selection;
                selection.MoveToDisplayColumn(e.Position.LineNumber, e.Position.Column, false);
            }
            catch
            {
            }
        }

        private void runFeedback_GoToType(object sender, UI.GoToTypeArgs e)
        {
            try
            {
                new TypeNavigation().GoToType(_application, e.Assembly, e.TypeName);
            }
            catch (Exception ex)
            {
            }
        }

        public void PrepareForFocus()
        {
            _provider.PrepareForFocus();
        }

        public bool IsInFocus()
        {
            return _provider.IsInFocus();
        }

        public void Clear()
        {
            _provider.ClearList();
        }

        public void ClearBuilds(string project)
        {
            _provider.ClearBuilds(project);
        }

        private void FeedbackWindow_Resize(object sender, EventArgs e)
        {
            runFeedback.Resize();
        }

        private void runFeedback_CancelRun(object sender, EventArgs e)
        {
            _bus.Publish(new AbortMessage(""));
        }
    }
}

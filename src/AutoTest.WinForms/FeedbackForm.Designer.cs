using System.Windows.Forms;
namespace AutoTest.WinForms
{
    partial class FeedbackForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            var keepOnTop = new ToolStripMenuItem("keep on top")
            {
                CheckOnClick = true
            };
            keepOnTop.Click += (sender, eventArgs) => this.TopMost = ((ToolStripMenuItem)sender).Checked;

            var minimal = new ToolStripMenuItem("hide title bar")
            {
                CheckOnClick = true
            };
            minimal.Click += (sender, eventArgs) =>
            {
                var isChecked = ((ToolStripMenuItem)sender).Checked;

                this.ControlBox = !isChecked;
                this.ShowIcon = !isChecked;
                this.FormBorderStyle = !isChecked ? FormBorderStyle.FixedSingle : FormBorderStyle.Sizable;
                this.Text = !isChecked ? "AutoTest.Net" : string.Empty;
            };

            this.ContextMenuStrip = new ContextMenuStrip();
            this.ContextMenuStrip.Items.Add(keepOnTop);
            this.ContextMenuStrip.Items.Add(minimal);

            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FeedbackForm));
            this.runFeedback = new AutoTest.UI.RunFeedback();
            this.SuspendLayout();
            // 
            // runFeedback
            // 
            this.runFeedback.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.runFeedback.CanDebug = false;
            this.runFeedback.CanGoToTypes = false;
            this.runFeedback.ListViewWidthOffset = 0;
            this.runFeedback.Location = new System.Drawing.Point(2, 2);
            this.runFeedback.Name = "runFeedback";
            this.runFeedback.ShowIcon = true;
            this.runFeedback.ShowRunInformation = true;
            this.runFeedback.Size = new System.Drawing.Size(937, 216);
            this.runFeedback.TabIndex = 0;
            this.runFeedback.GoToReference += new System.EventHandler<AutoTest.UI.GoToReferenceArgs>(this.runFeedback_GoToReference);
            this.runFeedback.CancelRun += new System.EventHandler(this.runFeedback_CancelRun);
            // 
            // FeedbackForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(941, 219);
            this.Controls.Add(this.runFeedback);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FeedbackForm";
            this.Text = "AutoTest.Net";
            this.ResumeLayout(false);

        }

        #endregion

        private UI.RunFeedback runFeedback;


    }
}


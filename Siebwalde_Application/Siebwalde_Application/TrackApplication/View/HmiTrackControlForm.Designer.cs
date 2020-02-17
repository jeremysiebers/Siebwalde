namespace Siebwalde_Application.TrackApplication.View
{
    partial class HmiTrackControlForm
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
        private void InitializeComponent(TrackController trackController)
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HmiTrackControlForm));
            this.HmiTrackControl = new System.Windows.Forms.Integration.ElementHost();
            this.hmiTrackControl1 = new Siebwalde_Application.TrackApplication.View.HmiTrackControl(trackController);
            this.SuspendLayout();
            // 
            // HmiTrackControl
            // 
            this.HmiTrackControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.HmiTrackControl.Location = new System.Drawing.Point(0, 0);
            this.HmiTrackControl.Name = "HmiTrackControl";
            this.HmiTrackControl.Size = new System.Drawing.Size(1264, 921);
            this.HmiTrackControl.TabIndex = 0;
            this.HmiTrackControl.Text = "HmiTrackControl";
            this.HmiTrackControl.Child = this.hmiTrackControl1;
            // 
            // HmiTrackControlForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1264, 921);
            this.Controls.Add(this.HmiTrackControl);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "HmiTrackControlForm";
            this.Text = "Track Controller";
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.Integration.ElementHost HmiTrackControl;
        public HmiTrackControl hmiTrackControl1;
    }
}
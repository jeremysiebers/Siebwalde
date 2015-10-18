namespace Siebwalde_Application
{
    partial class FiddleYardSettingsForm
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.FiddleYardSimSpeedSetting = new System.Windows.Forms.NumericUpDown();
            this.BtnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.BtnReload = new System.Windows.Forms.Button();
            this.BtnSave = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.FiddleYardSimSpeedSetting)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // FiddleYardSimSpeedSetting
            // 
            this.FiddleYardSimSpeedSetting.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::Siebwalde_Application.Properties.Settings.Default, "FIDDLExYARDxSIMxSPEEDxSETTING", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.FiddleYardSimSpeedSetting.Location = new System.Drawing.Point(189, 9);
            this.FiddleYardSimSpeedSetting.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.FiddleYardSimSpeedSetting.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.FiddleYardSimSpeedSetting.Name = "FiddleYardSimSpeedSetting";
            this.FiddleYardSimSpeedSetting.Size = new System.Drawing.Size(57, 20);
            this.FiddleYardSimSpeedSetting.TabIndex = 0;
            this.FiddleYardSimSpeedSetting.Value = global::Siebwalde_Application.Properties.Settings.Default.FIDDLExYARDxSIMxSPEEDxSETTING;
            // 
            // BtnCancel
            // 
            this.BtnCancel.Location = new System.Drawing.Point(179, 19);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(75, 23);
            this.BtnCancel.TabIndex = 1;
            this.BtnCancel.Text = "Cancel";
            this.BtnCancel.UseVisualStyleBackColor = true;
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 11);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(176, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Fiddle Yard Simulator Speed Setting";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.BtnSave);
            this.groupBox1.Controls.Add(this.BtnReload);
            this.groupBox1.Controls.Add(this.BtnCancel);
            this.groupBox1.Location = new System.Drawing.Point(10, 129);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(265, 48);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            // 
            // BtnReload
            // 
            this.BtnReload.Location = new System.Drawing.Point(17, 19);
            this.BtnReload.Name = "BtnReload";
            this.BtnReload.Size = new System.Drawing.Size(75, 23);
            this.BtnReload.TabIndex = 2;
            this.BtnReload.Text = "Reload";
            this.BtnReload.UseVisualStyleBackColor = true;
            this.BtnReload.Click += new System.EventHandler(this.BtnReload_Click);
            // 
            // BtnSave
            // 
            this.BtnSave.Location = new System.Drawing.Point(98, 19);
            this.BtnSave.Name = "BtnSave";
            this.BtnSave.Size = new System.Drawing.Size(75, 23);
            this.BtnSave.TabIndex = 3;
            this.BtnSave.Text = "Save";
            this.BtnSave.UseVisualStyleBackColor = true;
            this.BtnSave.Click += new System.EventHandler(this.BtnSave_Click);
            // 
            // FiddleYardSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(284, 174);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.FiddleYardSimSpeedSetting);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FiddleYardSettingsForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.TopMost = true;
            this.Load += new System.EventHandler(this.FiddleYardSettingsForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.FiddleYardSimSpeedSetting)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NumericUpDown FiddleYardSimSpeedSetting;
        private System.Windows.Forms.Button BtnCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button BtnReload;
        private System.Windows.Forms.Button BtnSave;

    }
}

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
            this.BtnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.BtnSave = new System.Windows.Forms.Button();
            this.BtnReload = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.SetColorTrackOccupied = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.FiddleYardSimSpeedSetting = new System.Windows.Forms.NumericUpDown();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.SetColorTrackDisabled = new System.Windows.Forms.Label();
            this.SetColorTrackNotActive = new System.Windows.Forms.Label();
            this.SetColorTrackNotInitialized = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FiddleYardSimSpeedSetting)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
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
            this.label1.Location = new System.Drawing.Point(15, 21);
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
            this.groupBox1.Location = new System.Drawing.Point(12, 309);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(265, 48);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
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
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 25);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(166, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "SiebWalde Track Occupied Color";
            // 
            // SetColorTrackOccupied
            // 
            this.SetColorTrackOccupied.BackColor = System.Drawing.Color.Red;
            this.SetColorTrackOccupied.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.SetColorTrackOccupied.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.SetColorTrackOccupied.Location = new System.Drawing.Point(228, 25);
            this.SetColorTrackOccupied.Name = "SetColorTrackOccupied";
            this.SetColorTrackOccupied.Size = new System.Drawing.Size(26, 14);
            this.SetColorTrackOccupied.TabIndex = 81;
            this.SetColorTrackOccupied.Click += new System.EventHandler(this.SetColorTrackOccupied_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.FiddleYardSimSpeedSetting);
            this.groupBox2.Location = new System.Drawing.Point(12, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(333, 128);
            this.groupBox2.TabIndex = 82;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "FiddleYard Settings";
            // 
            // FiddleYardSimSpeedSetting
            // 
            this.FiddleYardSimSpeedSetting.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::Siebwalde_Application.Properties.Settings.Default, "FIDDLExYARDxSIMxSPEEDxSETTING", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.FiddleYardSimSpeedSetting.Location = new System.Drawing.Point(197, 19);
            this.FiddleYardSimSpeedSetting.Maximum = new decimal(new int[] {
            10000,
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
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label5);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.SetColorTrackDisabled);
            this.groupBox3.Controls.Add(this.SetColorTrackNotActive);
            this.groupBox3.Controls.Add(this.SetColorTrackNotInitialized);
            this.groupBox3.Controls.Add(this.label4);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.SetColorTrackOccupied);
            this.groupBox3.Location = new System.Drawing.Point(12, 146);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(333, 157);
            this.groupBox3.TabIndex = 83;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "SiebWalde Settings";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(15, 94);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(161, 13);
            this.label5.TabIndex = 87;
            this.label5.Text = "SiebWalde Track Disabled Color";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 72);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(170, 13);
            this.label3.TabIndex = 85;
            this.label3.Text = "SiebWalde Track Not Active Color";
            // 
            // SetColorTrackDisabled
            // 
            this.SetColorTrackDisabled.BackColor = System.Drawing.Color.Red;
            this.SetColorTrackDisabled.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.SetColorTrackDisabled.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.SetColorTrackDisabled.Location = new System.Drawing.Point(228, 94);
            this.SetColorTrackDisabled.Name = "SetColorTrackDisabled";
            this.SetColorTrackDisabled.Size = new System.Drawing.Size(26, 14);
            this.SetColorTrackDisabled.TabIndex = 84;
            this.SetColorTrackDisabled.Click += new System.EventHandler(this.SetColorTrackDisabled_Click);
            // 
            // SetColorTrackNotActive
            // 
            this.SetColorTrackNotActive.BackColor = System.Drawing.Color.Red;
            this.SetColorTrackNotActive.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.SetColorTrackNotActive.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.SetColorTrackNotActive.Location = new System.Drawing.Point(228, 71);
            this.SetColorTrackNotActive.Name = "SetColorTrackNotActive";
            this.SetColorTrackNotActive.Size = new System.Drawing.Size(26, 14);
            this.SetColorTrackNotActive.TabIndex = 84;
            this.SetColorTrackNotActive.Click += new System.EventHandler(this.SetColorTrackNotActive_Click);
            // 
            // SetColorTrackNotInitialized
            // 
            this.SetColorTrackNotInitialized.BackColor = System.Drawing.Color.Red;
            this.SetColorTrackNotInitialized.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.SetColorTrackNotInitialized.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.SetColorTrackNotInitialized.Location = new System.Drawing.Point(228, 48);
            this.SetColorTrackNotInitialized.Name = "SetColorTrackNotInitialized";
            this.SetColorTrackNotInitialized.Size = new System.Drawing.Size(26, 14);
            this.SetColorTrackNotInitialized.TabIndex = 84;
            this.SetColorTrackNotInitialized.Click += new System.EventHandler(this.SetColorTrackNotInitialized_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(15, 48);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(183, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "SiebWalde Track Not Initialized Color";
            // 
            // FiddleYardSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(357, 371);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FiddleYardSettingsForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.TopMost = true;
            this.Load += new System.EventHandler(this.FiddleYardSettingsForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FiddleYardSimSpeedSetting)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NumericUpDown FiddleYardSimSpeedSetting;
        private System.Windows.Forms.Button BtnCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button BtnReload;
        private System.Windows.Forms.Button BtnSave;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label SetColorTrackOccupied;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label SetColorTrackNotInitialized;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label SetColorTrackNotActive;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label SetColorTrackDisabled;

    }
}

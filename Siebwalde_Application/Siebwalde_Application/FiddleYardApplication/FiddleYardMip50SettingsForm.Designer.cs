namespace Siebwalde_Application
{
    partial class FiddleYardMip50SettingsForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FiddleYardMip50SettingsForm));
            this.TB_MIP50PositioningVelocity = new System.Windows.Forms.MaskedTextBox();
            this.TB_MIP50PositioningAcceleration = new System.Windows.Forms.MaskedTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.BT_MIP50PositioningVelocity_SET = new System.Windows.Forms.Button();
            this.BT_MIP50PositioningAcceleration_SET = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.BT_MIP50PositioningAcceleration_RELOAD = new System.Windows.Forms.Button();
            this.BT_MIP50PositioningVelocity_RELOAD = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.maskedTextBox1 = new System.Windows.Forms.MaskedTextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.maskedTextBox2 = new System.Windows.Forms.MaskedTextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // TB_MIP50PositioningVelocity
            // 
            this.TB_MIP50PositioningVelocity.Location = new System.Drawing.Point(172, 26);
            this.TB_MIP50PositioningVelocity.Mask = "000";
            this.TB_MIP50PositioningVelocity.Name = "TB_MIP50PositioningVelocity";
            this.TB_MIP50PositioningVelocity.Size = new System.Drawing.Size(49, 20);
            this.TB_MIP50PositioningVelocity.TabIndex = 0;
            this.TB_MIP50PositioningVelocity.MaskInputRejected += new System.Windows.Forms.MaskInputRejectedEventHandler(this.TB_MIP50PositioningVelocity_MaskInputRejected);
            // 
            // TB_MIP50PositioningAcceleration
            // 
            this.TB_MIP50PositioningAcceleration.Location = new System.Drawing.Point(172, 69);
            this.TB_MIP50PositioningAcceleration.Mask = "0.0";
            this.TB_MIP50PositioningAcceleration.Name = "TB_MIP50PositioningAcceleration";
            this.TB_MIP50PositioningAcceleration.Size = new System.Drawing.Size(49, 20);
            this.TB_MIP50PositioningAcceleration.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 72);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(148, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "MIP50PositioningAcceleration";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 29);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(126, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "MIP50PositioningVelocity";
            // 
            // BT_MIP50PositioningVelocity_SET
            // 
            this.BT_MIP50PositioningVelocity_SET.Location = new System.Drawing.Point(250, 24);
            this.BT_MIP50PositioningVelocity_SET.Name = "BT_MIP50PositioningVelocity_SET";
            this.BT_MIP50PositioningVelocity_SET.Size = new System.Drawing.Size(75, 23);
            this.BT_MIP50PositioningVelocity_SET.TabIndex = 2;
            this.BT_MIP50PositioningVelocity_SET.Text = "Set";
            this.BT_MIP50PositioningVelocity_SET.UseVisualStyleBackColor = true;
            this.BT_MIP50PositioningVelocity_SET.Click += new System.EventHandler(this.BT_MIP50PositioningVelocity_SET_Click);
            // 
            // BT_MIP50PositioningAcceleration_SET
            // 
            this.BT_MIP50PositioningAcceleration_SET.Location = new System.Drawing.Point(250, 67);
            this.BT_MIP50PositioningAcceleration_SET.Name = "BT_MIP50PositioningAcceleration_SET";
            this.BT_MIP50PositioningAcceleration_SET.Size = new System.Drawing.Size(75, 23);
            this.BT_MIP50PositioningAcceleration_SET.TabIndex = 2;
            this.BT_MIP50PositioningAcceleration_SET.Text = "Set";
            this.BT_MIP50PositioningAcceleration_SET.UseVisualStyleBackColor = true;
            this.BT_MIP50PositioningAcceleration_SET.Click += new System.EventHandler(this.BT_MIP50PositioningAcceleration_SET_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.BT_MIP50PositioningAcceleration_SET);
            this.groupBox1.Controls.Add(this.TB_MIP50PositioningVelocity);
            this.groupBox1.Controls.Add(this.BT_MIP50PositioningAcceleration_RELOAD);
            this.groupBox1.Controls.Add(this.BT_MIP50PositioningVelocity_RELOAD);
            this.groupBox1.Controls.Add(this.BT_MIP50PositioningVelocity_SET);
            this.groupBox1.Controls.Add(this.TB_MIP50PositioningAcceleration);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(21, 28);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(438, 109);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "MIP50 Parameters";
            // 
            // BT_MIP50PositioningAcceleration_RELOAD
            // 
            this.BT_MIP50PositioningAcceleration_RELOAD.Location = new System.Drawing.Point(331, 67);
            this.BT_MIP50PositioningAcceleration_RELOAD.Name = "BT_MIP50PositioningAcceleration_RELOAD";
            this.BT_MIP50PositioningAcceleration_RELOAD.Size = new System.Drawing.Size(75, 23);
            this.BT_MIP50PositioningAcceleration_RELOAD.TabIndex = 2;
            this.BT_MIP50PositioningAcceleration_RELOAD.Text = "Set";
            this.BT_MIP50PositioningAcceleration_RELOAD.UseVisualStyleBackColor = true;
            this.BT_MIP50PositioningAcceleration_RELOAD.Click += new System.EventHandler(this.BT_MIP50PositioningAcceleration_RELOAD_Click);
            // 
            // BT_MIP50PositioningVelocity_RELOAD
            // 
            this.BT_MIP50PositioningVelocity_RELOAD.Location = new System.Drawing.Point(331, 24);
            this.BT_MIP50PositioningVelocity_RELOAD.Name = "BT_MIP50PositioningVelocity_RELOAD";
            this.BT_MIP50PositioningVelocity_RELOAD.Size = new System.Drawing.Size(75, 23);
            this.BT_MIP50PositioningVelocity_RELOAD.TabIndex = 2;
            this.BT_MIP50PositioningVelocity_RELOAD.Text = "Set";
            this.BT_MIP50PositioningVelocity_RELOAD.UseVisualStyleBackColor = true;
            this.BT_MIP50PositioningVelocity_RELOAD.Click += new System.EventHandler(this.BT_MIP50PositioningVelocity_RELOAD_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.button1);
            this.groupBox2.Controls.Add(this.maskedTextBox1);
            this.groupBox2.Controls.Add(this.button2);
            this.groupBox2.Controls.Add(this.button3);
            this.groupBox2.Controls.Add(this.button4);
            this.groupBox2.Controls.Add(this.maskedTextBox2);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Location = new System.Drawing.Point(21, 155);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(438, 118);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "MIP50 Constants";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(18, 29);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(126, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "MIP50PositioningVelocity";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(250, 67);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "Set";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // maskedTextBox1
            // 
            this.maskedTextBox1.Location = new System.Drawing.Point(172, 26);
            this.maskedTextBox1.Mask = "000";
            this.maskedTextBox1.Name = "maskedTextBox1";
            this.maskedTextBox1.Size = new System.Drawing.Size(49, 20);
            this.maskedTextBox1.TabIndex = 0;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(331, 67);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = "Set";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(331, 24);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 2;
            this.button3.Text = "Set";
            this.button3.UseVisualStyleBackColor = true;
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(250, 24);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(75, 23);
            this.button4.TabIndex = 2;
            this.button4.Text = "Set";
            this.button4.UseVisualStyleBackColor = true;
            // 
            // maskedTextBox2
            // 
            this.maskedTextBox2.Location = new System.Drawing.Point(172, 69);
            this.maskedTextBox2.Mask = "0.0";
            this.maskedTextBox2.Name = "maskedTextBox2";
            this.maskedTextBox2.Size = new System.Drawing.Size(49, 20);
            this.maskedTextBox2.TabIndex = 0;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(18, 72);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(148, 13);
            this.label4.TabIndex = 1;
            this.label4.Text = "MIP50PositioningAcceleration";
            // 
            // FiddleYardMip50SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(466, 293);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FiddleYardMip50SettingsForm";
            this.Text = "FiddleYardMip50SettingsForm";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.MaskedTextBox TB_MIP50PositioningVelocity;
        private System.Windows.Forms.MaskedTextBox TB_MIP50PositioningAcceleration;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button BT_MIP50PositioningVelocity_SET;
        private System.Windows.Forms.Button BT_MIP50PositioningAcceleration_SET;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button BT_MIP50PositioningAcceleration_RELOAD;
        private System.Windows.Forms.Button BT_MIP50PositioningVelocity_RELOAD;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.MaskedTextBox maskedTextBox1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.MaskedTextBox maskedTextBox2;
        private System.Windows.Forms.Label label4;
    }
}
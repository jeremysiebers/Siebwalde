namespace Siebwalde_Application
{
    partial class Main
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reConnectFiddleYardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearEventLoggersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hardResetFiddleYardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.FiddleYardFormTop = new System.Windows.Forms.Button();
            this.FiddleYardFormBot = new System.Windows.Forms.Button();
            this.LinkActivity = new System.Windows.Forms.ProgressBar();
            this.labelFiddleYard = new System.Windows.Forms.Label();
            this.SiebwaldeAppLog = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.fToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(784, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(92, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click_1);
            // 
            // fToolStripMenuItem
            // 
            this.fToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.reConnectFiddleYardToolStripMenuItem,
            this.clearEventLoggersToolStripMenuItem,
            this.resetToolStripMenuItem});
            this.fToolStripMenuItem.Name = "fToolStripMenuItem";
            this.fToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.fToolStripMenuItem.Text = "Edit";
            // 
            // reConnectFiddleYardToolStripMenuItem
            // 
            this.reConnectFiddleYardToolStripMenuItem.Name = "reConnectFiddleYardToolStripMenuItem";
            this.reConnectFiddleYardToolStripMenuItem.Size = new System.Drawing.Size(213, 22);
            this.reConnectFiddleYardToolStripMenuItem.Text = "Re-Connect to Fiddle Yard";
            this.reConnectFiddleYardToolStripMenuItem.Click += new System.EventHandler(this.reConnectFiddleYardToolStripMenuItem_Click);
            // 
            // clearEventLoggersToolStripMenuItem
            // 
            this.clearEventLoggersToolStripMenuItem.Name = "clearEventLoggersToolStripMenuItem";
            this.clearEventLoggersToolStripMenuItem.Size = new System.Drawing.Size(213, 22);
            this.clearEventLoggersToolStripMenuItem.Text = "Clear Event Loggers";
            this.clearEventLoggersToolStripMenuItem.Click += new System.EventHandler(this.clearEventLoggersToolStripMenuItem_Click);
            // 
            // resetToolStripMenuItem
            // 
            this.resetToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.hardResetFiddleYardToolStripMenuItem});
            this.resetToolStripMenuItem.Name = "resetToolStripMenuItem";
            this.resetToolStripMenuItem.Size = new System.Drawing.Size(213, 22);
            this.resetToolStripMenuItem.Text = "Reset";
            // 
            // hardResetFiddleYardToolStripMenuItem
            // 
            this.hardResetFiddleYardToolStripMenuItem.Name = "hardResetFiddleYardToolStripMenuItem";
            this.hardResetFiddleYardToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
            this.hardResetFiddleYardToolStripMenuItem.Text = "Hard Reset Fiddle Yard";
            this.hardResetFiddleYardToolStripMenuItem.Click += new System.EventHandler(this.hardResetFiddleYardToolStripMenuItem_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem1});
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.aboutToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem1
            // 
            this.aboutToolStripMenuItem1.Name = "aboutToolStripMenuItem1";
            this.aboutToolStripMenuItem1.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem1.Text = "About";
            this.aboutToolStripMenuItem1.Click += new System.EventHandler(this.aboutToolStripMenuItem1_Click);
            // 
            // toolTip1
            // 
            this.toolTip1.ShowAlways = true;
            // 
            // FiddleYardFormTop
            // 
            this.FiddleYardFormTop.Location = new System.Drawing.Point(4, 25);
            this.FiddleYardFormTop.Name = "FiddleYardFormTop";
            this.FiddleYardFormTop.Size = new System.Drawing.Size(150, 23);
            this.FiddleYardFormTop.TabIndex = 26;
            this.FiddleYardFormTop.Text = "Fiddle Yard TOP Layer";
            this.FiddleYardFormTop.UseVisualStyleBackColor = true;
            this.FiddleYardFormTop.Click += new System.EventHandler(this.FiddleYardFormTop_Click);
            // 
            // FiddleYardFormBot
            // 
            this.FiddleYardFormBot.Location = new System.Drawing.Point(160, 25);
            this.FiddleYardFormBot.Name = "FiddleYardFormBot";
            this.FiddleYardFormBot.Size = new System.Drawing.Size(150, 23);
            this.FiddleYardFormBot.TabIndex = 27;
            this.FiddleYardFormBot.Text = "Fiddle Yard BOT Layer";
            this.FiddleYardFormBot.UseVisualStyleBackColor = true;
            this.FiddleYardFormBot.Click += new System.EventHandler(this.FiddleYardFormBot_Click);
            // 
            // LinkActivity
            // 
            this.LinkActivity.Location = new System.Drawing.Point(446, 1);
            this.LinkActivity.Name = "LinkActivity";
            this.LinkActivity.Size = new System.Drawing.Size(100, 23);
            this.LinkActivity.TabIndex = 28;
            // 
            // labelFiddleYard
            // 
            this.labelFiddleYard.AutoSize = true;
            this.labelFiddleYard.Location = new System.Drawing.Point(317, 6);
            this.labelFiddleYard.Name = "labelFiddleYard";
            this.labelFiddleYard.Size = new System.Drawing.Size(120, 13);
            this.labelFiddleYard.TabIndex = 29;
            this.labelFiddleYard.Text = "Link Activity Fiddle Yard";
            // 
            // SiebwaldeAppLog
            // 
            this.SiebwaldeAppLog.Location = new System.Drawing.Point(12, 129);
            this.SiebwaldeAppLog.MaxLength = 999999;
            this.SiebwaldeAppLog.Multiline = true;
            this.SiebwaldeAppLog.Name = "SiebwaldeAppLog";
            this.SiebwaldeAppLog.ReadOnly = true;
            this.SiebwaldeAppLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.SiebwaldeAppLog.Size = new System.Drawing.Size(760, 242);
            this.SiebwaldeAppLog.TabIndex = 30;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 113);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(152, 13);
            this.label1.TabIndex = 31;
            this.label1.Text = "Siebwalde Application Logging";
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(784, 562);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.SiebwaldeAppLog);
            this.Controls.Add(this.labelFiddleYard);
            this.Controls.Add(this.LinkActivity);
            this.Controls.Add(this.FiddleYardFormBot);
            this.Controls.Add(this.FiddleYardFormTop);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Siebwalde Application";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Main_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem fToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reConnectFiddleYardToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearEventLoggersToolStripMenuItem;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.ToolStripMenuItem resetToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hardResetFiddleYardToolStripMenuItem;
        private System.Windows.Forms.Button FiddleYardFormTop;
        private System.Windows.Forms.Button FiddleYardFormBot;
        private System.Windows.Forms.ProgressBar LinkActivity;
        private System.Windows.Forms.Label labelFiddleYard;
        private System.Windows.Forms.TextBox SiebwaldeAppLog;
        private System.Windows.Forms.Label label1;
    }
}


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
            this.Led_CommLink = new System.Windows.Forms.Button();
            this.FiddleYardFormTop = new System.Windows.Forms.Button();
            this.FiddleYardFormBot = new System.Windows.Forms.Button();
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
            this.menuStrip1.Size = new System.Drawing.Size(1912, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
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
            this.fToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fToolStripMenuItem.Text = "Edit";
            // 
            // reConnectFiddleYardToolStripMenuItem
            // 
            this.reConnectFiddleYardToolStripMenuItem.Name = "reConnectFiddleYardToolStripMenuItem";
            this.reConnectFiddleYardToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.reConnectFiddleYardToolStripMenuItem.Text = "Re-Connect to Fiddle Yard";
            this.reConnectFiddleYardToolStripMenuItem.Click += new System.EventHandler(this.reConnectFiddleYardToolStripMenuItem_Click);
            // 
            // clearEventLoggersToolStripMenuItem
            // 
            this.clearEventLoggersToolStripMenuItem.Name = "clearEventLoggersToolStripMenuItem";
            this.clearEventLoggersToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.clearEventLoggersToolStripMenuItem.Text = "Clear Event Loggers";
            this.clearEventLoggersToolStripMenuItem.Click += new System.EventHandler(this.clearEventLoggersToolStripMenuItem_Click);
            // 
            // resetToolStripMenuItem
            // 
            this.resetToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.hardResetFiddleYardToolStripMenuItem});
            this.resetToolStripMenuItem.Name = "resetToolStripMenuItem";
            this.resetToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
            this.resetToolStripMenuItem.Text = "Reset";
            // 
            // hardResetFiddleYardToolStripMenuItem
            // 
            this.hardResetFiddleYardToolStripMenuItem.Name = "hardResetFiddleYardToolStripMenuItem";
            this.hardResetFiddleYardToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
            this.hardResetFiddleYardToolStripMenuItem.Text = "Hard Reset Fiddle Yard";
            this.hardResetFiddleYardToolStripMenuItem.Click += new System.EventHandler(this.hardResetFiddleYardToolStripMenuItem_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem1});
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(40, 20);
            this.aboutToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem1
            // 
            this.aboutToolStripMenuItem1.Name = "aboutToolStripMenuItem1";
            this.aboutToolStripMenuItem1.Size = new System.Drawing.Size(114, 22);
            this.aboutToolStripMenuItem1.Text = "About";
            this.aboutToolStripMenuItem1.Click += new System.EventHandler(this.aboutToolStripMenuItem1_Click);
            // 
            // toolTip1
            // 
            this.toolTip1.ShowAlways = true;
            // 
            // Led_CommLink
            // 
            this.Led_CommLink.Location = new System.Drawing.Point(1841, 1);
            this.Led_CommLink.Name = "Led_CommLink";
            this.Led_CommLink.Size = new System.Drawing.Size(71, 23);
            this.Led_CommLink.TabIndex = 25;
            this.Led_CommLink.Text = "Comm Link";
            this.Led_CommLink.UseVisualStyleBackColor = true;
            // 
            // FiddleYardFormTop
            // 
            this.FiddleYardFormTop.Location = new System.Drawing.Point(4, 27);
            this.FiddleYardFormTop.Name = "FiddleYardFormTop";
            this.FiddleYardFormTop.Size = new System.Drawing.Size(150, 23);
            this.FiddleYardFormTop.TabIndex = 26;
            this.FiddleYardFormTop.Text = "Fiddle Yard TOP Layer";
            this.FiddleYardFormTop.UseVisualStyleBackColor = true;
            this.FiddleYardFormTop.Click += new System.EventHandler(this.FiddleYardFormTop_Click);
            // 
            // FiddleYardFormBot
            // 
            this.FiddleYardFormBot.Location = new System.Drawing.Point(160, 27);
            this.FiddleYardFormBot.Name = "FiddleYardFormBot";
            this.FiddleYardFormBot.Size = new System.Drawing.Size(150, 23);
            this.FiddleYardFormBot.TabIndex = 27;
            this.FiddleYardFormBot.Text = "Fiddle Yard BOT Layer";
            this.FiddleYardFormBot.UseVisualStyleBackColor = true;
            this.FiddleYardFormBot.Click += new System.EventHandler(this.FiddleYardFormBot_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(1912, 51);
            this.Controls.Add(this.FiddleYardFormBot);
            this.Controls.Add(this.FiddleYardFormTop);
            this.Controls.Add(this.Led_CommLink);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(1920, 85);
            this.MinimumSize = new System.Drawing.Size(800, 85);
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Siebwalde App";
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
        private System.Windows.Forms.Button Led_CommLink;
        private System.Windows.Forms.ToolStripMenuItem resetToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hardResetFiddleYardToolStripMenuItem;
        private System.Windows.Forms.Button FiddleYardFormTop;
        private System.Windows.Forms.Button FiddleYardFormBot;
    }
}


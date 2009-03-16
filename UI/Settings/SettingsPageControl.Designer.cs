﻿namespace TrailsPlugin.UI.Settings {
	partial class SettingsPageControl {
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsPageControl));
			this.PluginInfoPanel = new ZoneFiveSoftware.Common.Visuals.Panel();
			this.label1 = new System.Windows.Forms.Label();
			this.PluginInfoBanner = new ZoneFiveSoftware.Common.Visuals.ActionBanner();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.PluginInfoPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// PluginInfoPanel
			// 
			this.PluginInfoPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.PluginInfoPanel.BackColor = System.Drawing.Color.Transparent;
			this.PluginInfoPanel.BorderColor = System.Drawing.Color.Gray;
			this.PluginInfoPanel.Controls.Add(this.label3);
			this.PluginInfoPanel.Controls.Add(this.label2);
			this.PluginInfoPanel.Controls.Add(this.textBox1);
			this.PluginInfoPanel.Controls.Add(this.label1);
			this.PluginInfoPanel.Controls.Add(this.PluginInfoBanner);
			this.PluginInfoPanel.HeadingBackColor = System.Drawing.Color.LightBlue;
			this.PluginInfoPanel.HeadingFont = null;
			this.PluginInfoPanel.HeadingLeftMargin = 0;
			this.PluginInfoPanel.HeadingText = null;
			this.PluginInfoPanel.HeadingTextColor = System.Drawing.Color.Black;
			this.PluginInfoPanel.HeadingTopMargin = 3;
			this.PluginInfoPanel.Location = new System.Drawing.Point(0, 0);
			this.PluginInfoPanel.Name = "PluginInfoPanel";
			this.PluginInfoPanel.Size = new System.Drawing.Size(461, 457);
			this.PluginInfoPanel.TabIndex = 0;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 32);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(161, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Copyright Brendan Doherty 2009";
			// 
			// PluginInfoBanner
			// 
			this.PluginInfoBanner.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.PluginInfoBanner.BackColor = System.Drawing.Color.Transparent;
			this.PluginInfoBanner.HasMenuButton = false;
			this.PluginInfoBanner.Location = new System.Drawing.Point(0, 0);
			this.PluginInfoBanner.Margin = new System.Windows.Forms.Padding(0);
			this.PluginInfoBanner.Name = "PluginInfoBanner";
			this.PluginInfoBanner.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.PluginInfoBanner.Size = new System.Drawing.Size(461, 23);
			this.PluginInfoBanner.Style = ZoneFiveSoftware.Common.Visuals.ActionBanner.BannerStyle.Header2;
			this.PluginInfoBanner.TabIndex = 0;
			this.PluginInfoBanner.Text = "Plugin Information";
			this.PluginInfoBanner.UseStyleFont = true;
			// 
			// textBox1
			// 
			this.textBox1.AcceptsReturn = true;
			this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBox1.Location = new System.Drawing.Point(3, 272);
			this.textBox1.Multiline = true;
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(452, 182);
			this.textBox1.TabIndex = 2;
			this.textBox1.Text = resources.GetString("textBox1.Text");
			this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(3, 256);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(313, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Trails Plugin is distributed under the GNU General Public Licence";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(3, 62);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(35, 13);
			this.label3.TabIndex = 4;
			this.label3.Text = "label3";
			// 
			// SettingsPageControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.PluginInfoPanel);
			this.Name = "SettingsPageControl";
			this.Size = new System.Drawing.Size(461, 457);
			this.PluginInfoPanel.ResumeLayout(false);
			this.PluginInfoPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private ZoneFiveSoftware.Common.Visuals.Panel PluginInfoPanel;
		private ZoneFiveSoftware.Common.Visuals.ActionBanner PluginInfoBanner;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;



	}
}

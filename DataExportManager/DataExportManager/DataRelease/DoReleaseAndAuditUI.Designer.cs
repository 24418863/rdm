﻿namespace DataExportManager.DataRelease
{
    partial class DoReleaseAndAuditUI
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
            this.label1 = new System.Windows.Forms.Label();
            this.btnRelease = new System.Windows.Forms.Button();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.lblReleaseRootDirectory = new System.Windows.Forms.Label();
            this.btnShowReleaseDirectory = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 4);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Configurations:";
            // 
            // btnRelease
            // 
            this.btnRelease.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnRelease.Location = new System.Drawing.Point(209, 532);
            this.btnRelease.Name = "btnRelease";
            this.btnRelease.Size = new System.Drawing.Size(135, 23);
            this.btnRelease.TabIndex = 2;
            this.btnRelease.Text = "Release";
            this.btnRelease.UseVisualStyleBackColor = true;
            this.btnRelease.Click += new System.EventHandler(this.btnRelease_Click);
            // 
            // treeView1
            // 
            this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView1.Location = new System.Drawing.Point(7, 20);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(525, 477);
            this.treeView1.TabIndex = 3;
            this.treeView1.KeyUp += new System.Windows.Forms.KeyEventHandler(this.treeView1_KeyUp);
            // 
            // lblReleaseRootDirectory
            // 
            this.lblReleaseRootDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblReleaseRootDirectory.AutoSize = true;
            this.lblReleaseRootDirectory.Location = new System.Drawing.Point(7, 504);
            this.lblReleaseRootDirectory.Name = "lblReleaseRootDirectory";
            this.lblReleaseRootDirectory.Size = new System.Drawing.Size(94, 13);
            this.lblReleaseRootDirectory.TabIndex = 4;
            this.lblReleaseRootDirectory.Text = "Release Directory:";
            // 
            // btnShowReleaseDirectory
            // 
            this.btnShowReleaseDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnShowReleaseDirectory.Location = new System.Drawing.Point(473, 504);
            this.btnShowReleaseDirectory.Name = "btnShowReleaseDirectory";
            this.btnShowReleaseDirectory.Size = new System.Drawing.Size(71, 23);
            this.btnShowReleaseDirectory.TabIndex = 5;
            this.btnShowReleaseDirectory.Text = "Show";
            this.btnShowReleaseDirectory.UseVisualStyleBackColor = true;
            this.btnShowReleaseDirectory.Click += new System.EventHandler(this.btnShowReleaseDirectory_Click);
            // 
            // DoReleaseAndAuditUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnShowReleaseDirectory);
            this.Controls.Add(this.lblReleaseRootDirectory);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.btnRelease);
            this.Controls.Add(this.label1);
            this.Name = "DoReleaseAndAuditUI";
            this.Size = new System.Drawing.Size(547, 558);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnRelease;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Label lblReleaseRootDirectory;
        private System.Windows.Forms.Button btnShowReleaseDirectory;
    }
}
﻿namespace CatalogueManager.DataLoadUIs.ANOUIs.PreLoadDiscarding
{
    partial class PreLoadDiscardedColumnUI
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
            this.btnSave = new System.Windows.Forms.Button();
            this.tbRuntimeColumnName = new System.Windows.Forms.TextBox();
            this.tbID = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.ddDestination = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tbSqlDataType = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.lblErrorInType = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(132, 133);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 4;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // tbRuntimeColumnName
            // 
            this.tbRuntimeColumnName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbRuntimeColumnName.Location = new System.Drawing.Point(132, 38);
            this.tbRuntimeColumnName.Name = "tbRuntimeColumnName";
            this.tbRuntimeColumnName.Size = new System.Drawing.Size(325, 20);
            this.tbRuntimeColumnName.TabIndex = 1;
            this.tbRuntimeColumnName.TextChanged += new System.EventHandler(this.tbRuntimeColumnName_TextChanged);
            // 
            // tbID
            // 
            this.tbID.Location = new System.Drawing.Point(132, 16);
            this.tbID.Name = "tbID";
            this.tbID.ReadOnly = true;
            this.tbID.Size = new System.Drawing.Size(126, 20);
            this.tbID.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(118, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Column Runtime Name:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(21, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "ID:";
            // 
            // ddDestination
            // 
            this.ddDestination.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddDestination.FormattingEnabled = true;
            this.ddDestination.Location = new System.Drawing.Point(132, 106);
            this.ddDestination.Name = "ddDestination";
            this.ddDestination.Size = new System.Drawing.Size(208, 21);
            this.ddDestination.TabIndex = 3;
            this.ddDestination.SelectedIndexChanged += new System.EventHandler(this.ddDestination_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 109);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(63, 13);
            this.label3.TabIndex = 11;
            this.label3.Text = "Destination:";
            // 
            // tbSqlDataType
            // 
            this.tbSqlDataType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbSqlDataType.Location = new System.Drawing.Point(132, 64);
            this.tbSqlDataType.Name = "tbSqlDataType";
            this.tbSqlDataType.Size = new System.Drawing.Size(325, 20);
            this.tbSqlDataType.TabIndex = 2;
            this.tbSqlDataType.TextChanged += new System.EventHandler(this.tbSqlDataType_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(54, 67);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(72, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "SqlDataType:";
            // 
            // lblErrorInType
            // 
            this.lblErrorInType.AutoSize = true;
            this.lblErrorInType.ForeColor = System.Drawing.Color.Red;
            this.lblErrorInType.Location = new System.Drawing.Point(129, 87);
            this.lblErrorInType.Name = "lblErrorInType";
            this.lblErrorInType.Size = new System.Drawing.Size(72, 13);
            this.lblErrorInType.TabIndex = 13;
            this.lblErrorInType.Text = "lblErrorInType";
            this.lblErrorInType.Visible = false;
            // 
            // PreLoadDiscardedColumnUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblErrorInType);
            this.Controls.Add(this.tbSqlDataType);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.ddDestination);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.tbRuntimeColumnName);
            this.Controls.Add(this.tbID);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "PreLoadDiscardedColumnUI";
            this.Size = new System.Drawing.Size(494, 160);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.TextBox tbRuntimeColumnName;
        private System.Windows.Forms.TextBox tbID;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox ddDestination;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbSqlDataType;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label lblErrorInType;
    }
}
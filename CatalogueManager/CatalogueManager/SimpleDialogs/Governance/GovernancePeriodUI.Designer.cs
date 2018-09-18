﻿using CatalogueManager.LocationsMenu.Ticketing;

namespace CatalogueManager.SimpleDialogs.Governance
{
    partial class GovernancePeriodUI
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
            this.label3 = new System.Windows.Forms.Label();
            this.dtpStartDate = new System.Windows.Forms.DateTimePicker();
            this.label4 = new System.Windows.Forms.Label();
            this.ticketingControl1 = new CatalogueManager.LocationsMenu.Ticketing.TicketingControl();
            this.rbNeverExpires = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.rbExpiresOn = new System.Windows.Forms.RadioButton();
            this.tbName = new System.Windows.Forms.TextBox();
            this.dtpEndDate = new System.Windows.Forms.DateTimePicker();
            this.label1 = new System.Windows.Forms.Label();
            this.lbCatalogues = new System.Windows.Forms.ListBox();
            this.tbDescription = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.btnAddCatalogue = new System.Windows.Forms.Button();
            this.btnImportCatalogues = new System.Windows.Forms.Button();
            this.ragSmiley1 = new ReusableUIComponents.RAGSmiley();
            this.objectSaverButton1 = new CatalogueManager.SimpleControls.ObjectSaverButton();
            this.SuspendLayout();
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(4, 335);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Start Date:";
            // 
            // dtpStartDate
            // 
            this.dtpStartDate.Location = new System.Drawing.Point(71, 329);
            this.dtpStartDate.Name = "dtpStartDate";
            this.dtpStartDate.Size = new System.Drawing.Size(159, 20);
            this.dtpStartDate.TabIndex = 4;
            this.dtpStartDate.ValueChanged += new System.EventHandler(this.dtpStartDate_ValueChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(4, 357);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(55, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "End Date:";
            // 
            // ticketingControl1
            // 
            this.ticketingControl1.Location = new System.Drawing.Point(71, 269);
            this.ticketingControl1.Name = "ticketingControl1";
            this.ticketingControl1.Size = new System.Drawing.Size(303, 54);
            this.ticketingControl1.TabIndex = 3;
            this.ticketingControl1.TicketText = "";
            // 
            // rbNeverExpires
            // 
            this.rbNeverExpires.AutoSize = true;
            this.rbNeverExpires.Location = new System.Drawing.Point(71, 357);
            this.rbNeverExpires.Name = "rbNeverExpires";
            this.rbNeverExpires.Size = new System.Drawing.Size(91, 17);
            this.rbNeverExpires.TabIndex = 6;
            this.rbNeverExpires.TabStop = true;
            this.rbNeverExpires.Text = "Never Expires";
            this.rbNeverExpires.UseVisualStyleBackColor = true;
            this.rbNeverExpires.CheckedChanged += new System.EventHandler(this.rbNeverExpires_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(2, 32);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Description:";
            // 
            // rbExpiresOn
            // 
            this.rbExpiresOn.AutoSize = true;
            this.rbExpiresOn.Location = new System.Drawing.Point(71, 380);
            this.rbExpiresOn.Name = "rbExpiresOn";
            this.rbExpiresOn.Size = new System.Drawing.Size(79, 17);
            this.rbExpiresOn.TabIndex = 6;
            this.rbExpiresOn.TabStop = true;
            this.rbExpiresOn.Text = "Expires On:";
            this.rbExpiresOn.UseVisualStyleBackColor = true;
            this.rbExpiresOn.CheckedChanged += new System.EventHandler(this.rbExpiresOn_CheckedChanged);
            // 
            // tbName
            // 
            this.tbName.Location = new System.Drawing.Point(71, 3);
            this.tbName.Name = "tbName";
            this.tbName.Size = new System.Drawing.Size(444, 20);
            this.tbName.TabIndex = 1;
            this.tbName.TextChanged += new System.EventHandler(this.tbName_TextChanged);
            // 
            // dtpEndDate
            // 
            this.dtpEndDate.Location = new System.Drawing.Point(156, 377);
            this.dtpEndDate.Name = "dtpEndDate";
            this.dtpEndDate.Size = new System.Drawing.Size(159, 20);
            this.dtpEndDate.TabIndex = 7;
            this.dtpEndDate.ValueChanged += new System.EventHandler(this.dtpEndDate_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(2, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Name:";
            // 
            // lbCatalogues
            // 
            this.lbCatalogues.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbCatalogues.FormattingEnabled = true;
            this.lbCatalogues.Location = new System.Drawing.Point(384, 283);
            this.lbCatalogues.Name = "lbCatalogues";
            this.lbCatalogues.Size = new System.Drawing.Size(667, 303);
            this.lbCatalogues.TabIndex = 9;
            this.lbCatalogues.KeyUp += new System.Windows.Forms.KeyEventHandler(this.lbCatalogues_KeyUp);
            // 
            // tbDescription
            // 
            this.tbDescription.AcceptsReturn = true;
            this.tbDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDescription.Location = new System.Drawing.Point(71, 29);
            this.tbDescription.Multiline = true;
            this.tbDescription.Name = "tbDescription";
            this.tbDescription.Size = new System.Drawing.Size(993, 234);
            this.tbDescription.TabIndex = 1;
            this.tbDescription.TextChanged += new System.EventHandler(this.tbDescription_TextChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(377, 267);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(234, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Period relates to the Governance of Catalogues:";
            // 
            // btnAddCatalogue
            // 
            this.btnAddCatalogue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddCatalogue.Location = new System.Drawing.Point(384, 589);
            this.btnAddCatalogue.Name = "btnAddCatalogue";
            this.btnAddCatalogue.Size = new System.Drawing.Size(98, 23);
            this.btnAddCatalogue.TabIndex = 11;
            this.btnAddCatalogue.Text = "Add Catalogue(s)";
            this.btnAddCatalogue.UseVisualStyleBackColor = true;
            this.btnAddCatalogue.Click += new System.EventHandler(this.btnAddCatalogue_Click);
            // 
            // btnImportCatalogues
            // 
            this.btnImportCatalogues.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnImportCatalogues.Location = new System.Drawing.Point(488, 589);
            this.btnImportCatalogues.Name = "btnImportCatalogues";
            this.btnImportCatalogues.Size = new System.Drawing.Size(282, 23);
            this.btnImportCatalogues.TabIndex = 11;
            this.btnImportCatalogues.Text = "Import Catalogue List From Another Governance Period";
            this.btnImportCatalogues.UseVisualStyleBackColor = true;
            this.btnImportCatalogues.Click += new System.EventHandler(this.btnImportCatalogues_Click);
            // 
            // ragSmiley1
            // 
            this.ragSmiley1.AlwaysShowHandCursor = false;
            this.ragSmiley1.BackColor = System.Drawing.Color.Transparent;
            this.ragSmiley1.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.ragSmiley1.Location = new System.Drawing.Point(321, 375);
            this.ragSmiley1.Name = "ragSmiley1";
            this.ragSmiley1.Size = new System.Drawing.Size(25, 25);
            this.ragSmiley1.TabIndex = 18;
            // 
            // objectSaverButton1
            // 
            this.objectSaverButton1.Location = new System.Drawing.Point(71, 407);
            this.objectSaverButton1.Margin = new System.Windows.Forms.Padding(0);
            this.objectSaverButton1.Name = "objectSaverButton1";
            this.objectSaverButton1.Size = new System.Drawing.Size(54, 27);
            this.objectSaverButton1.TabIndex = 19;
            // 
            // GovernancePeriodUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.objectSaverButton1);
            this.Controls.Add(this.ragSmiley1);
            this.Controls.Add(this.btnImportCatalogues);
            this.Controls.Add(this.btnAddCatalogue);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tbDescription);
            this.Controls.Add(this.dtpStartDate);
            this.Controls.Add(this.lbCatalogues);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.ticketingControl1);
            this.Controls.Add(this.dtpEndDate);
            this.Controls.Add(this.rbNeverExpires);
            this.Controls.Add(this.tbName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.rbExpiresOn);
            this.Name = "GovernancePeriodUI";
            this.Size = new System.Drawing.Size(1078, 615);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnImportCatalogues;
        private System.Windows.Forms.Button btnAddCatalogue;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbDescription;
        private System.Windows.Forms.ListBox lbCatalogues;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker dtpEndDate;
        private System.Windows.Forms.TextBox tbName;
        private System.Windows.Forms.RadioButton rbExpiresOn;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RadioButton rbNeverExpires;
        private TicketingControl ticketingControl1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.DateTimePicker dtpStartDate;
        private System.Windows.Forms.Label label3;
        private ReusableUIComponents.RAGSmiley ragSmiley1;
        private SimpleControls.ObjectSaverButton objectSaverButton1;
    }
}

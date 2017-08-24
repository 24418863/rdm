﻿namespace CatalogueManager.SimpleDialogs
{
    partial class BulkProcessCatalogueItems
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.btnClear = new System.Windows.Forms.Button();
            this.lbPastedColumns = new System.Windows.Forms.ListBox();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.lbCatalogueItems = new System.Windows.Forms.ListBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.ddExtractionCategory = new System.Windows.Forms.ComboBox();
            this.btnApplyTransform = new System.Windows.Forms.Button();
            this.rbMarkExtractable = new System.Windows.Forms.RadioButton();
            this.rbGuessNewAssociatedColumns = new System.Windows.Forms.RadioButton();
            this.rbDeleteExtrctionInformation = new System.Windows.Forms.RadioButton();
            this.rbDeleteAssociatedColumnInfos = new System.Windows.Forms.RadioButton();
            this.label3 = new System.Windows.Forms.Label();
            this.rbDelete = new System.Windows.Forms.RadioButton();
            this.label4 = new System.Windows.Forms.Label();
            this.cbTableInfos = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rbApplyToMatching = new System.Windows.Forms.RadioButton();
            this.rbApplyToAll = new System.Windows.Forms.RadioButton();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(106, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Paste Columns Here:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(120, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "CatalogueItemsAffected";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.btnClear);
            this.splitContainer1.Panel1.Controls.Add(this.lbPastedColumns);
            this.splitContainer1.Panel1.Controls.Add(this.label1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(940, 601);
            this.splitContainer1.SplitterDistance = 252;
            this.splitContainer1.TabIndex = 2;
            // 
            // btnClear
            // 
            this.btnClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnClear.Location = new System.Drawing.Point(6, 575);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(75, 23);
            this.btnClear.TabIndex = 2;
            this.btnClear.Text = "Clear";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // lbPastedColumns
            // 
            this.lbPastedColumns.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbPastedColumns.FormattingEnabled = true;
            this.lbPastedColumns.Location = new System.Drawing.Point(6, 25);
            this.lbPastedColumns.Name = "lbPastedColumns";
            this.lbPastedColumns.Size = new System.Drawing.Size(243, 550);
            this.lbPastedColumns.TabIndex = 1;
            this.lbPastedColumns.KeyUp += new System.Windows.Forms.KeyEventHandler(this.lbPastedColumns_KeyUp);
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.lbCatalogueItems);
            this.splitContainer2.Panel1.Controls.Add(this.label2);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.groupBox2);
            this.splitContainer2.Panel2.Controls.Add(this.groupBox1);
            this.splitContainer2.Size = new System.Drawing.Size(684, 601);
            this.splitContainer2.SplitterDistance = 279;
            this.splitContainer2.TabIndex = 3;
            // 
            // lbCatalogueItems
            // 
            this.lbCatalogueItems.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbCatalogueItems.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lbCatalogueItems.FormattingEnabled = true;
            this.lbCatalogueItems.Location = new System.Drawing.Point(6, 25);
            this.lbCatalogueItems.Name = "lbCatalogueItems";
            this.lbCatalogueItems.Size = new System.Drawing.Size(270, 563);
            this.lbCatalogueItems.TabIndex = 1;
            this.lbCatalogueItems.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lbCatalogueItems_DrawItem);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.ddExtractionCategory);
            this.groupBox2.Controls.Add(this.btnApplyTransform);
            this.groupBox2.Controls.Add(this.rbMarkExtractable);
            this.groupBox2.Controls.Add(this.rbGuessNewAssociatedColumns);
            this.groupBox2.Controls.Add(this.rbDeleteExtrctionInformation);
            this.groupBox2.Controls.Add(this.rbDeleteAssociatedColumnInfos);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.rbDelete);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.cbTableInfos);
            this.groupBox2.Location = new System.Drawing.Point(17, 79);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(372, 509);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Transform to Apply:";
            this.groupBox2.Enter += new System.EventHandler(this.groupBox2_Enter);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 46);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(52, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "Category:";
            // 
            // ddExtractionCategory
            // 
            this.ddExtractionCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddExtractionCategory.FormattingEnabled = true;
            this.ddExtractionCategory.Location = new System.Drawing.Point(66, 43);
            this.ddExtractionCategory.Name = "ddExtractionCategory";
            this.ddExtractionCategory.Size = new System.Drawing.Size(281, 21);
            this.ddExtractionCategory.TabIndex = 5;
            this.ddExtractionCategory.SelectedIndexChanged += new System.EventHandler(this.ddExtractionCategory_SelectedIndexChanged);
            // 
            // btnApplyTransform
            // 
            this.btnApplyTransform.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnApplyTransform.Location = new System.Drawing.Point(144, 301);
            this.btnApplyTransform.Name = "btnApplyTransform";
            this.btnApplyTransform.Size = new System.Drawing.Size(97, 23);
            this.btnApplyTransform.TabIndex = 4;
            this.btnApplyTransform.Text = "Apply Transform";
            this.btnApplyTransform.UseVisualStyleBackColor = true;
            this.btnApplyTransform.Click += new System.EventHandler(this.btnApplyTransform_Click);
            // 
            // rbMarkExtractable
            // 
            this.rbMarkExtractable.AutoSize = true;
            this.rbMarkExtractable.Location = new System.Drawing.Point(6, 19);
            this.rbMarkExtractable.Name = "rbMarkExtractable";
            this.rbMarkExtractable.Size = new System.Drawing.Size(203, 17);
            this.rbMarkExtractable.TabIndex = 0;
            this.rbMarkExtractable.TabStop = true;
            this.rbMarkExtractable.Text = "Mark Associated Columns Extractable";
            this.rbMarkExtractable.UseVisualStyleBackColor = true;
            // 
            // rbGuessNewAssociatedColumns
            // 
            this.rbGuessNewAssociatedColumns.AutoSize = true;
            this.rbGuessNewAssociatedColumns.Location = new System.Drawing.Point(7, 114);
            this.rbGuessNewAssociatedColumns.Name = "rbGuessNewAssociatedColumns";
            this.rbGuessNewAssociatedColumns.Size = new System.Drawing.Size(178, 17);
            this.rbGuessNewAssociatedColumns.TabIndex = 0;
            this.rbGuessNewAssociatedColumns.TabStop = true;
            this.rbGuessNewAssociatedColumns.Text = "Guess New Associated Columns";
            this.rbGuessNewAssociatedColumns.UseVisualStyleBackColor = true;
            // 
            // rbDeleteExtrctionInformation
            // 
            this.rbDeleteExtrctionInformation.AutoSize = true;
            this.rbDeleteExtrctionInformation.Location = new System.Drawing.Point(7, 226);
            this.rbDeleteExtrctionInformation.Name = "rbDeleteExtrctionInformation";
            this.rbDeleteExtrctionInformation.Size = new System.Drawing.Size(161, 17);
            this.rbDeleteExtrctionInformation.TabIndex = 3;
            this.rbDeleteExtrctionInformation.TabStop = true;
            this.rbDeleteExtrctionInformation.Text = "Delete Extraction Information";
            this.rbDeleteExtrctionInformation.UseVisualStyleBackColor = true;
            // 
            // rbDeleteAssociatedColumnInfos
            // 
            this.rbDeleteAssociatedColumnInfos.AutoSize = true;
            this.rbDeleteAssociatedColumnInfos.Location = new System.Drawing.Point(7, 249);
            this.rbDeleteAssociatedColumnInfos.Name = "rbDeleteAssociatedColumnInfos";
            this.rbDeleteAssociatedColumnInfos.Size = new System.Drawing.Size(352, 17);
            this.rbDeleteAssociatedColumnInfos.TabIndex = 3;
            this.rbDeleteAssociatedColumnInfos.TabStop = true;
            this.rbDeleteAssociatedColumnInfos.Text = "Delete Associated ColumnInfos (will also delete extraction information)";
            this.rbDeleteAssociatedColumnInfos.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(29, 77);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(337, 34);
            this.label3.TabIndex = 1;
            this.label3.Text = "(every matching CatalogueItem that has 1 (and 1 only) associated ColumnInfo will " +
    "be enabled for extraction)";
            // 
            // rbDelete
            // 
            this.rbDelete.AutoSize = true;
            this.rbDelete.Location = new System.Drawing.Point(7, 203);
            this.rbDelete.Name = "rbDelete";
            this.rbDelete.Size = new System.Drawing.Size(135, 17);
            this.rbDelete.TabIndex = 3;
            this.rbDelete.TabStop = true;
            this.rbDelete.Text = "Delete Catalogue Items";
            this.rbDelete.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(29, 166);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(337, 34);
            this.label4.TabIndex = 1;
            this.label4.Text = "(every matching CatalogueItem will have an associated ColumnInfo guessed from the" +
    " TableInfo you select in the box above)";
            // 
            // cbTableInfos
            // 
            this.cbTableInfos.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.cbTableInfos.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbTableInfos.FormattingEnabled = true;
            this.cbTableInfos.Location = new System.Drawing.Point(32, 138);
            this.cbTableInfos.Name = "cbTableInfos";
            this.cbTableInfos.Size = new System.Drawing.Size(315, 21);
            this.cbTableInfos.TabIndex = 2;
            this.cbTableInfos.SelectedIndexChanged += new System.EventHandler(this.cbTableInfos_SelectedIndexChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rbApplyToMatching);
            this.groupBox1.Controls.Add(this.rbApplyToAll);
            this.groupBox1.Location = new System.Drawing.Point(17, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(353, 60);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Apply Transform To:";
            // 
            // rbApplyToMatching
            // 
            this.rbApplyToMatching.AutoSize = true;
            this.rbApplyToMatching.Location = new System.Drawing.Point(124, 28);
            this.rbApplyToMatching.Name = "rbApplyToMatching";
            this.rbApplyToMatching.Size = new System.Drawing.Size(165, 17);
            this.rbApplyToMatching.TabIndex = 0;
            this.rbApplyToMatching.TabStop = true;
            this.rbApplyToMatching.Text = "Only those matching paste list";
            this.rbApplyToMatching.UseVisualStyleBackColor = true;
            // 
            // rbApplyToAll
            // 
            this.rbApplyToAll.AutoSize = true;
            this.rbApplyToAll.Location = new System.Drawing.Point(6, 28);
            this.rbApplyToAll.Name = "rbApplyToAll";
            this.rbApplyToAll.Size = new System.Drawing.Size(112, 17);
            this.rbApplyToAll.TabIndex = 0;
            this.rbApplyToAll.TabStop = true;
            this.rbApplyToAll.Text = "All CatalogueItems";
            this.rbApplyToAll.UseVisualStyleBackColor = true;
            // 
            // BulkProcessCatalogueItems
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(940, 601);
            this.Controls.Add(this.splitContainer1);
            this.Name = "BulkProcessCatalogueItems";
            this.Text = "BulkProcessCatalogueItems";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel1.PerformLayout();
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListBox lbPastedColumns;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ListBox lbCatalogueItems;
        private System.Windows.Forms.RadioButton rbDeleteAssociatedColumnInfos;
        private System.Windows.Forms.RadioButton rbDelete;
        private System.Windows.Forms.ComboBox cbTableInfos;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RadioButton rbGuessNewAssociatedColumns;
        private System.Windows.Forms.RadioButton rbMarkExtractable;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rbApplyToMatching;
        private System.Windows.Forms.RadioButton rbApplyToAll;
        private System.Windows.Forms.Button btnApplyTransform;
        private System.Windows.Forms.RadioButton rbDeleteExtrctionInformation;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox ddExtractionCategory;
    }
}
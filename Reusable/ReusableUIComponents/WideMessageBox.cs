﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;


namespace ReusableUIComponents
{
    /// <summary>
    /// Used to display a message to the user including selectable text and resizing.  Basically improves on System.Windows.Forms.MessageBox
    /// </summary>
    [TechnicalUI]
    public partial class WideMessageBox : Form
    {
        private readonly string _environmentDotStackTrace;

        public WideMessageBox(string message, string environmentDotStackTrace = null, string keywordNotToAdd=null)
        {
            _environmentDotStackTrace = environmentDotStackTrace;
            InitializeComponent();
            
            richTextBox1.Text = message;
            richTextBox1.Select(0, 0);

            keywordHelpTextListbox1.Setup(richTextBox1, keywordNotToAdd);
            splitContainer1.Panel2Collapsed = !keywordHelpTextListbox1.HasEntries;
            
            //try to resize form to fit bounds
            this.Size = FormsHelper.GetPreferredSizeOfTextControl(richTextBox1);
            this.Size = new Size(this.Size.Width + 10, this.Size.Height + 150);//leave a bit of padding

            //can only write to clipboard in STA threads
            btnCopyToClipboard.Visible = Thread.CurrentThread.GetApartmentState() == ApartmentState.STA;

            btnViewStackTrace.Visible = _environmentDotStackTrace != null;

            var theScreen = Screen.FromControl(this);
            if (this.Width > theScreen.Bounds.Width)
            {
                this.Width = theScreen.Bounds.Width - 100;
                richTextBox1.WordWrap = true;
            }

            if (this.Height > theScreen.Bounds.Height)
                this.WindowState = FormWindowState.Maximized;
        }

        public static void Show(string message, string environmentDotStackTrace = null, bool isModalDialog = true, string keywordNotToAdd = null,string title = null)
        {
            WideMessageBox wmb = new WideMessageBox(message, environmentDotStackTrace, keywordNotToAdd);

            if(title != null)
                wmb.Text = title;

            if (isModalDialog)
                wmb.ShowDialog();
            else
                wmb.Show();
            
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnCopyToClipboard_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(richTextBox1.Text);
        }

        private void WideMessageBox_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Escape)
                this.Close();
        }

        private void btnViewStackTrace_Click(object sender, EventArgs e)
        {
            var dialog = new ExceptionViewerStackTraceWithHyperlinks(_environmentDotStackTrace);
            dialog.Show();
        }
    }
}
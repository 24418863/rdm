﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReusableUIComponents
{
    /// <summary>
    /// Part of ExceptionViewer, displays keywords that relate to the Exception being viewed.  For example if the Exception Message or StackTrace talks about Catalogue then the keyword 
    /// 'Catalogue' will appear in the KeywordHelpTextListbox.  The keywords and descriptions stored in HelpKeywordsDictionary which is populated both from the source code (zip file) and
    /// from the additional help keyword resources.  This includes keywords for all DatabaseEntity types (Catalogue, TableInfo etc) as well as all foreign keys (that do not CASCADE).
    /// </summary>
    public partial class KeywordHelpTextListbox : UserControl
    {
        #region Static setup of dictionary of keywords
        private static Dictionary<string, string> HelpKeywordsDictionary = new Dictionary<string, string>();

        public static void AddToHelpDictionaryIfNotExists(string key, string value)
        {
            //get rid of newlines
            value = value.Replace(Environment.NewLine, " ");
            
            if(!HelpKeywordsDictionary.ContainsKey(key))
                HelpKeywordsDictionary.Add(key,value);
        }
        #endregion
        
        public bool HasEntries { get; private set; }

        public KeywordHelpTextListbox()
        {
            InitializeComponent();
        }

        public void Setup(RichTextBox richTextBoxToHighlight, string keywordNotToAdd = null)
        {

            //if theres no keywords dont show the help listbox
            olvHelpSections.Visible = false;
            olvHelpSections.SmallImageList = imageList1;
            olvHelpSections.FullRowSelect = true;
            olvKeyword.ImageGetter += rowObject => "Information.ico";

            //unless the text is unreasonably long
            if(richTextBoxToHighlight.TextLength < 100000)
                //Highlight keywords and add the help text to the olvlistbox
                foreach (KeyValuePair<string, string> kvp in HelpKeywordsDictionary)
                    if (richTextBoxToHighlight.Text.Contains(kvp.Key))
                    {
                        if (keywordNotToAdd != null && kvp.Key.Equals(keywordNotToAdd))//if it is the one keyword we are not supposed to be adding (this is used when you double click a help and get a WideMessageBox with help for yourself you shouldn't add your own keyword)
                            continue;

                        HighlightText(richTextBoxToHighlight, kvp.Key, Color.MediumOrchid);
                        olvHelpSections.Visible = true;
                        HasEntries = true;

                        olvHelpSections.AddObject(new HelpSection(kvp.Key, kvp.Value));
                    }
        }

        public static void HighlightText(RichTextBox myRtb, string word, Color color)
        {
            if (word == string.Empty)
                return;
            var reg = new Regex(@"\b" + word + @"(\b|s\b)",RegexOptions.IgnoreCase);
            
            foreach (Match match in reg.Matches(myRtb.Text))
            {
                myRtb.Select(match.Index, match.Length);
                myRtb.SelectionColor = color;
            }

            myRtb.SelectionLength = 0;
            myRtb.SelectionColor = Color.Black;
        }

        private void olvHelpSections_ItemActivate(object sender, EventArgs e)
        {
            var hs = olvHelpSections.SelectedObject as HelpSection;

            if(hs != null)
                WideMessageBox.Show(hs.Keyword +":" + Environment.NewLine + hs.HelpText,Environment.StackTrace,false,hs.Keyword);
        }

    }
}
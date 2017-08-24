﻿using System.Drawing;
using System.Windows.Forms;

namespace ReusableUIComponents.SingleControlForms
{
    /// <summary>
    /// TECHNICAL: Helper class that turns a Control into a Form by mounting it.  Also wires up IConsultableBeforeClosing to the Form Closing event if the hosted Control implements it.
    /// </summary>
    [TechnicalUI]
    [System.ComponentModel.DesignerCategory("")]
    public class SingleControlForm:Form
    {
        public SingleControlForm(Control control, bool showOkButton = false)
        {
            SetClientSizeCore(control.Width, control.Height);
            Text = !string.IsNullOrWhiteSpace(control.Text)?control.Text:control.Name;

            Controls.Add(control);
            control.Anchor = AnchorStyles.Top | AnchorStyles.Left| AnchorStyles.Right | AnchorStyles.Bottom;
            
            var consult = control as IConsultableBeforeClosing;

            if (consult != null)
                FormClosing += consult.ConsultAboutClosing;

            if(showOkButton)
            {
                var okButton = new Button();
                okButton.Text = "Ok";
                okButton.Click += (s, e) =>
                {
                    DialogResult = DialogResult.OK;
                    Close();
                };

                var btnHeight = okButton.PreferredSize.Height;
                var btnWidth = okButton.PreferredSize.Width;
                
                this.Height += btnHeight;
                control.Height -= btnHeight;
                okButton.Location = new Point((ClientSize.Width / 2) - (btnWidth / 2), ClientSize.Height - btnHeight);
                okButton.Anchor = AnchorStyles.Bottom;
                
                Controls.Add(okButton);
                okButton.BringToFront();
            }
        }

        public static DialogResult ShowDialog(Control control,bool showOkButton = false)
        {
            return new SingleControlForm(control,showOkButton).ShowDialog();
        }
    }
}
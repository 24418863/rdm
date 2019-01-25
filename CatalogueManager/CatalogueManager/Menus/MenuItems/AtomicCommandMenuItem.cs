﻿using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CatalogueManager.ItemActivation;
using ReusableLibraryCode;
using ReusableLibraryCode.CommandExecution.AtomicCommands;
using ReusableUIComponents;
using ReusableUIComponents.Dialogs;

namespace CatalogueManager.Menus.MenuItems
{
    [System.ComponentModel.DesignerCategory("")]
    public class AtomicCommandMenuItem : ToolStripMenuItem
    {
        private readonly IAtomicCommand _command;
        private readonly IActivateItems _activator;

        public AtomicCommandMenuItem(IAtomicCommand command,IActivateItems activator)
        {
            _command = command;
            _activator = activator;

            Text = command.GetCommandName();
            Tag = command;
            Image = command.GetImage(activator.CoreIconProvider);
            
            //disable if impossible command
            Enabled = !command.IsImpossible;

            ToolTipText = command.IsImpossible ? command.ReasonCommandImpossible : command.GetCommandHelp() ?? activator.GetDocumentation(command.GetType());
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            try
            {
                _command.Execute();
            }
            catch (Exception ex)
            {
                var sqlException = ex.GetExceptionIfExists<SqlException>();

                if (sqlException != null)
                {
                    Regex fk = new Regex("((FK_)|(ix_))([A-Za-z_]*)");
                    var match = fk.Match(sqlException.Message);

                    if (match.Success)
                    {
                        var helpDict = _activator.RepositoryLocator.CatalogueRepository.CommentStore;

                        if (helpDict != null && helpDict.ContainsKey(match.Value))
                        {
                            ExceptionViewer.Show("Rule Broken" + Environment.NewLine + helpDict[match.Value] + Environment.NewLine + "(" + match.Value + ")",ex);
                            return;
                        }
                    }
                }

                ExceptionViewer.Show("Failed to execute command '" + _command.GetCommandName() + "' (Type was '" + _command.GetType().Name + "')", ex);
            }
        }
    }
}
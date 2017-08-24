﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using ReusableUIComponents.Copying;

namespace CatalogueManager.CommandExecution.AtomicCommands.UIFactory
{
    public class AtomicCommandUIFactory
    {
        private readonly IIconProvider _iconProvider;

        public AtomicCommandUIFactory(IIconProvider iconProvider)
        {
            _iconProvider = iconProvider;
        }

        public ToolStripMenuItem CreateMenuItem(IAtomicCommand command)
        {
            var toReturn = new ToolStripMenuItem(command.GetCommandName(), command.GetImage(_iconProvider));
            toReturn.Tag = command;
            toReturn.Click += (s,e) => command.Execute();

            //disable if impossible command
            if (command.IsImpossible)
                toReturn.Enabled = false;

            return toReturn;
        }

        public AtomicCommandLinkLabel CreateLinkLabel(IAtomicCommand command)
        {
            return new AtomicCommandLinkLabel(_iconProvider,command);
        }

        public AtomicCommandWithTargetUI<T> CreateLinkLabelWithSelection<T>(IAtomicCommandWithTarget command, IEnumerable<T> selection, Func<T, string> propertySelector)
        {
            return new AtomicCommandWithTargetUI<T>(_iconProvider, command, selection, propertySelector);
        }

        public ContextMenuStrip CreateMenu(params IAtomicCommand[] commands)
        {
            var toReturn = new ContextMenuStrip();
            
            foreach (IAtomicCommand command in commands)
                toReturn.Items.Add(CreateMenuItem(command));
            
            return toReturn;
        }
    }
}
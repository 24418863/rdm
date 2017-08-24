﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueManager.Icons.IconOverlays;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;

namespace CatalogueManager.PluginChildProvision
{
    public abstract class PluginUserInterface:IPluginUserInterface
    {
        protected readonly IActivateItems ItemActivator;
        public List<Exception> Exceptions { get; private set; }

        protected PluginUserInterface(IActivateItems itemActivator)
        {
            ItemActivator = itemActivator;
            Exceptions = new List<Exception>();
        }
        
        public abstract object[] GetChildren(object model);
        public abstract ToolStripMenuItem[] GetAdditionalRightClickMenuItems(DatabaseEntity databaseEntity);

        /// <summary>
        /// Use ItemActivator ShowRDMPSingleDatabaseObjectControl to create new tabs or ActivateWindow to show Forms (must be top level controls i.e. Forms)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="model"></param>
        public abstract void Activate(object sender, object model);
        public abstract Bitmap GetImage(object concept, OverlayKind kind = OverlayKind.None);
    }
}
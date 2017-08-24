﻿using System;
using System.Linq;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.PerformanceImprovement;
using CatalogueLibrary.Repositories;
using CatalogueManager.Collections.Providers;
using CatalogueManager.Icons.IconOverlays;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using CatalogueManager.Refreshing;
using CatalogueManager.SimpleDialogs;
using CatalogueManager.TestsAndSetup.ServicePropogation;
using MapsDirectlyToDatabaseTableUI;
using RDMPStartup;

namespace CatalogueManager.Menus
{
    [System.ComponentModel.DesignerCategory("")]
    public class ExtractionInformationMenu : RDMPContextMenuStrip
    {
        private readonly ExtractionInformation _extractionInformation;

        public ExtractionInformationMenu(IActivateItems activator, ExtractionInformation extractionInformation)
            : base(activator,extractionInformation)
        {
            _extractionInformation = extractionInformation;

            var addFilter = new ToolStripMenuItem("Add New Extraction Filter", activator.CoreIconProvider.GetImage(RDMPConcept.Filter,OverlayKind.Add), (s, e) => AddFilter());
            Items.Add(addFilter);

            AddCommonMenuItems();
        }
        
        private void AddFilter()
        {
            var newFilter = new ExtractionFilter(RepositoryLocator.CatalogueRepository, "New Filter " + Guid.NewGuid(),_extractionInformation);
            _activator.RefreshBus.Publish(this,new RefreshObjectEventArgs(newFilter));
            _activator.ActivateFilter(this,newFilter);
        }
    }
}
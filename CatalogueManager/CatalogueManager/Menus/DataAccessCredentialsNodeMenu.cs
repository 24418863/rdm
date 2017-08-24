using System;
using CatalogueLibrary.Data;
using CatalogueManager.Collections.Providers;
using CatalogueManager.Icons.IconOverlays;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using CatalogueManager.Refreshing;
using RDMPStartup;

namespace CatalogueManager.Menus
{
    internal class DataAccessCredentialsNodeMenu : RDMPContextMenuStrip
    {
        public DataAccessCredentialsNodeMenu(IActivateItems activator):base(activator,null)
        {
            Items.Add("Add New Credentials", activator.CoreIconProvider.GetImage(RDMPConcept.DataAccessCredentials,OverlayKind.Add), (s, e) => AddCredentials());
        }

        private void AddCredentials()
        {
            var newCredentials = new DataAccessCredentials(RepositoryLocator.CatalogueRepository, "New Blank Credentials " + Guid.NewGuid());

            _activator.RefreshBus.Publish(this,new RefreshObjectEventArgs(newCredentials));
        }
    }
}
using System;
using CatalogueLibrary.Nodes;
using CatalogueLibrary.Repositories;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using CatalogueManager.Menus.MenuItems;
using RDMPStartup;

namespace CatalogueManager.Menus
{
    public class AggregatesNodeMenu : RDMPContextMenuStrip
    {
        public AggregatesNodeMenu(IActivateItems activator, AggregatesNode aggregatesNode):base(activator,null)
        {
            Items.Add(new AddAggregateMenuItem(activator, aggregatesNode.Catalogue));
        }
    }
}
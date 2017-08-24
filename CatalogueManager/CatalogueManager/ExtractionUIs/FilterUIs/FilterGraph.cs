﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Aggregation;
using CatalogueLibrary.Data.Dashboarding;
using CatalogueLibrary.QueryBuilding;
using CatalogueLibrary.Spontaneous;
using CatalogueManager.AggregationUIs;
using CatalogueManager.ItemActivation;
using CatalogueManager.Refreshing;
using CatalogueManager.TestsAndSetup.ServicePropogation;

namespace CatalogueManager.ExtractionUIs.FilterUIs
{
    /// <summary>
    /// Shows a given Aggregate Graph with an additional IFilter applied.  This can be used for checking that a filter SQL is working how you intend by giving you a view you are already 
    /// familiar with (the graph you created) but with the addition of the filter.  You can also launch the graph normally (See AggregateGraph) to see a side by side comparison
    /// </summary>
    public partial class FilterGraph : AggregateGraph, IObjectCollectionControl
    {
        private FilterGraphObjectCollection _collection;

        public FilterGraph()
        {
            InitializeComponent();
        }

        protected override object[] GetRibbonObjects()
        {
            if (_collection == null)
                return base.GetRibbonObjects();

            return new object[] {_collection.GetFilter(), _collection.GetGraph()};
        }

        protected override AggregateBuilder GetQueryBuilder(AggregateConfiguration aggregateConfiguration)
        {
            var basicQueryBuilder =  base.GetQueryBuilder(aggregateConfiguration);
            
            var rootContainer = basicQueryBuilder.RootFilterContainer;

            //stick our IFilter into the root container (actually create a new root container with our filter in it and move the old root if any into it)
            rootContainer =
                new SpontaneouslyInventedFilterContainer(rootContainer == null ? null : new[] {rootContainer},
                    new[] {_collection.GetFilter()}, FilterContainerOperation.AND);

            basicQueryBuilder.RootFilterContainer = rootContainer;

            return basicQueryBuilder;
        }

        public void RefreshBus_RefreshObject(object sender, RefreshObjectEventArgs e)
        {
            _collection.HandleRefreshObject(e);
        }

        public void SetCollection(IActivateItems activator, IPersistableObjectCollection collection)
        {
            _collection = (FilterGraphObjectCollection)collection;
            _activator = activator;
            SetAggregate(_collection.GetGraph());
            LoadGraphAsync();
        }

        public IPersistableObjectCollection GetCollection()
        {
            return _collection;
        }
        public override string GetTabName()
        {
            return "Filter Graph '" + _collection.GetFilter() + "'";
        }
    }
}
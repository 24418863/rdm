﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Aggregation;
using CatalogueManager.ItemActivation;
using CatalogueManager.Refreshing;

namespace CatalogueManager.AggregationUIs.Advanced
{
    /// <summary>
    /// Allows you to limit the graph generated to X bars (in the case of a graph without an axis) or restrict the number of Pivot values returned.  For example you can graph 'Top 10 most
    /// prescribed drugs'.  Top X is meaningless without an order by statement, therefore you must also configure a dimension to order by (and a direction).  In most cases you should leave 
    /// the Dimension at 'Count Column' this will mean that whatever your count dimension is (usually count(*)) will be used to determine the TOP X.  Setting to Ascending will give you the
    /// lowest number e.g. 'Top 10 LEAST prescribed drugs' instead.  If you change the dimension from the 'count column' to one of your dimensions then the TOP X will apply to that column
    /// instead.  e.g. the 'The first 10 prescribed drugs alphabetically' (not particularly useful).
    /// </summary>
    public partial class AggregateTopXUI : UserControl
    {
        private AggregateTopX _topX;
        private AggregateConfiguration _aggregate;
        private IActivateItems _activator;

        private const string CountColumn  = "Count Column";

        public AggregateTopXUI()
        {
            InitializeComponent();
        }

        private bool bLoading = false;

        public void SetUp(IActivateItems activator, IAggregateEditorOptions options, AggregateConfiguration aggregate)
        {
            _activator = activator;
            Enabled = options.ShouldBeEnabled(AggregateEditorSection.TOPX, aggregate);
            _aggregate = aggregate;
            _topX = aggregate.GetTopXIfAny();

            RefreshUIFromDatabase();
        }

        private void RefreshUIFromDatabase()
        {
            bLoading = true;
            ddOrderByDimension.Items.Clear();
            ddOrderByDimension.Items.Add(CountColumn);
            ddOrderByDimension.Items.AddRange(_aggregate.AggregateDimensions);

            if (_topX != null)
            {
                ddOrderByDimension.Enabled = true;
                ddAscOrDesc.Enabled = true;
                
                tbTopX.Text = _topX.TopX.ToString();
                ddAscOrDesc.DataSource = Enum.GetValues(typeof(AggregateTopX.AggregateTopXOrderByDirection));
                ddAscOrDesc.SelectedItem = _topX.OrderByDirection;

                if (_topX.OrderByDimensionIfAny_ID == null)
                    ddOrderByDimension.SelectedItem = CountColumn;
                else
                    ddOrderByDimension.SelectedItem = _topX.OrderByDimensionIfAny;
            }
            else
            {
                ddOrderByDimension.Enabled = false;
                ddAscOrDesc.Enabled = false;
            }
            bLoading = false;
        }

        private void tbTopX_TextChanged(object sender, EventArgs e)
        {
            if(bLoading)
                return;

            //user is trying to delete an existing TopX
            if (_topX != null && string.IsNullOrWhiteSpace(tbTopX.Text))
            {
                _topX.DeleteInDatabase();
                _activator.RefreshBus.Publish(this,new RefreshObjectEventArgs(_aggregate));
                return;
            }

            //user is typing something illegal like 'ive got a lovely bunch o coconuts'
            int i;
            if (!int.TryParse(tbTopX.Text, out i))
            {
                //not an int
                tbTopX.ForeColor = Color.Red;
                return;
            }

            //user put in a negative
            if (i <= 0)
            {
                tbTopX.ForeColor = Color.Red;
                return;
            }

            tbTopX.ForeColor = Color.Black;

            //there isn't one yet
            if (_topX == null)
                _topX = new AggregateTopX(_activator.RepositoryLocator.CatalogueRepository, _aggregate, i);
            else
            {
                //there is one so change it's topX
                _topX.TopX = i;
                _topX.SaveToDatabase();
            }
            _activator.RefreshBus.Publish(this, new RefreshObjectEventArgs(_aggregate));
        }

        private void ddOrderByDimension_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (bLoading)
                return;

            if(_topX == null || ddOrderByDimension.SelectedItem == null)
                return;

            var dimension = ddOrderByDimension.SelectedItem as AggregateDimension;

            if (dimension != null)
                _topX.OrderByDimensionIfAny_ID = dimension.ID;
            else
                _topX.OrderByDimensionIfAny_ID = null; //means use count column 

            _topX.SaveToDatabase();
            _activator.RefreshBus.Publish(this, new RefreshObjectEventArgs(_aggregate));
        }

        private void ddAscOrDesc_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (bLoading)
                return;

            if(_topX == null || ddAscOrDesc.SelectedItem == null)
                return;

            _topX.OrderByDirection = (AggregateTopX.AggregateTopXOrderByDirection) ddAscOrDesc.SelectedItem;
            _topX.SaveToDatabase();
            _activator.RefreshBus.Publish(this, new RefreshObjectEventArgs(_aggregate));
        }
    }
}
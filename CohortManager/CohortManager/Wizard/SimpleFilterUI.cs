﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Aggregation;
using CatalogueLibrary.FilterImporting;
using CatalogueLibrary.FilterImporting.Construction;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using IContainer = CatalogueLibrary.Data.IContainer;

namespace CohortManager.Wizard
{
    /// <summary>
    /// Part of CreateNewCohortIdentificationConfigurationUI.  Allows you to view and edit the parameters (if any) of a Filter you have added (or was Mandatory) on a dataset.  For example if
    /// you have a Filter 'Drug Prescribed' on the dataset 'Prescribing' typing "'Paracetamol'" into the parameter will likely restrict the cohort to matching only those patients who have ever
    /// been prescribed Paracetamol.  
    /// 
    /// If the control is Readonly (disabled / greyed out) then it is probably a Mandatory filter on your dataset and you will not be able to remove it.
    /// 
    /// This UI is a simplified version of ExtractionFilterUI
    /// </summary>
    public partial class SimpleFilterUI : UserControl
    {
        private readonly IActivateItems _activator;
        private readonly ExtractionFilter _filter;
        
        public event Action RequestDeletion;

        int rowHeight = 30;

        public IFilter Filter {get { return _filter; }}

        public bool Mandatory
        {
            get { return _mandatory; }
            set
            {
                _mandatory = value;

                if (value)
                {
                    btnDelete.Enabled = false;
                    lblFilterName.Enabled = false;
                }
                else
                {
                    btnDelete.Enabled = true;
                    lblFilterName.Enabled = true;
                }


            }
        }

        List<SimpleParameterUI>  parameterUis = new List<SimpleParameterUI>();
        private bool _mandatory;

        public SimpleFilterUI(IActivateItems activator,ExtractionFilter filter)
        {
            _activator = activator;
            _filter = filter;
            InitializeComponent();

            lblFilterName.Text = filter.Name;
            pbFlter.Image = activator.CoreIconProvider.GetImage(RDMPConcept.Filter);

            var parameters = filter.ExtractionFilterParameters.ToArray();

            SetupKnownGoodValues();
            
            for (int i = 0; i < parameters.Length; i++)
            {
                var currentRowPanel = new Panel();
                
                currentRowPanel.Bounds = new Rectangle(0, 0, tableLayoutPanel1.Width, rowHeight);
                currentRowPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                currentRowPanel.Margin = Padding.Empty;

                var p = new SimpleParameterUI(activator,parameters[i]);
                currentRowPanel.Controls.Add(p);
                p.tbValue.TextChanged += (s, e) =>
                {
                    //we are here because user is selecting a value from the dropdown not because he is editting the text field manually
                    if(_settingAKnownGoodValue)
                        return;

                    //user is manually editting a Parameters so it no longer matches a Known value
                    ddKnownGoodValues.SelectedItem = "";
                };
                parameterUis.Add(p);

                tableLayoutPanel1.Controls.Add(currentRowPanel,0,i+1);
                tableLayoutPanel1.CellBorderStyle = TableLayoutPanelCellBorderStyle.Inset;
            }

            Height = 35 + (parameters.Length*rowHeight);
        }

        private void SetupKnownGoodValues()
        {
            var knownGoodValues = _activator.RepositoryLocator.CatalogueRepository.GetAllObjectsWithParent<ExtractionFilterParameterSet>(_filter);
            
            if (knownGoodValues.Any())
            {
                pbKnownValueSets.Visible = true;
                ddKnownGoodValues.Visible = true;

                List<object> l = new List<object>();
                l.Add("");
                l.AddRange(knownGoodValues);

                ddKnownGoodValues.DataSource = l;
                pbKnownValueSets.Image = _activator.CoreIconProvider.GetImage(RDMPConcept.ExtractionFilterParameterSet);
                
                pbKnownValueSets.Left = lblFilterName.Right;
                ddKnownGoodValues.Left = pbKnownValueSets.Right;

            }
            else
            {
                pbKnownValueSets.Visible = false;
                ddKnownGoodValues.Visible = false;
            }
 

        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if(RequestDeletion != null)
                RequestDeletion();
        }

        private void lblFilterName_Click(object sender, EventArgs e)
        {

        }

        private bool _settingAKnownGoodValue = false;

        private void ddKnownGoodValues_SelectedIndexChanged(object sender, EventArgs e)
        {
            var set = ddKnownGoodValues.SelectedItem as ExtractionFilterParameterSet;

            _settingAKnownGoodValue = true;
            foreach (SimpleParameterUI p in parameterUis)
                p.SetValueTo(set);
            _settingAKnownGoodValue = false;
        }

        public IFilter CreateFilter(IFilterFactory factory ,IContainer filterContainer, IFilter[] alreadyExisting)
        {
            var importer = new FilterImporter(factory, null);
            var newFilter = importer.ImportFilter(_filter, alreadyExisting);
            
            foreach (SimpleParameterUI parameterUi in parameterUis)
                parameterUi.HandleSettingParameters(newFilter);

            //if there are known good values
            if (ddKnownGoodValues.SelectedItem != null && ddKnownGoodValues.SelectedItem as string != string.Empty)
                newFilter.Name += "_" + ddKnownGoodValues.SelectedItem;
            
           
            newFilter.FilterContainer_ID = filterContainer.ID;
            newFilter.SaveToDatabase();

            return newFilter;
        }
    }
}
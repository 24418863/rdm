﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.CommandExecution.AtomicCommands;
using CatalogueLibrary.Data;
using CatalogueManager.CommandExecution.AtomicCommands;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using DataExportLibrary.Data.LinkCreators;
using DataExportManager.ProjectUI;
using ReusableUIComponents.CommandExecution.AtomicCommands;
using ReusableUIComponents.Icons.IconProvision;

namespace DataExportManager.CommandExecution.AtomicCommands
{
    public class ExecuteCommandViewSelectedDatasetsExtractionSql:BasicUICommandExecution,IAtomicCommandWithTarget
    {
        private SelectedDataSets _selectedDataSet;

        public ExecuteCommandViewSelectedDatasetsExtractionSql(IActivateItems activator) : base(activator)
        {
        }

        public Image GetImage(IIconProvider iconProvider)
        {
            return iconProvider.GetImage(RDMPConcept.SQL,OverlayKind.Execute);
        }

        public IAtomicCommandWithTarget SetTarget(DatabaseEntity target)
        {
            _selectedDataSet = (SelectedDataSets) target;

            //must have datasets and have a cohort configured
            if(_selectedDataSet.ExtractionConfiguration.Cohort_ID == null)
                SetImpossible("No cohort has been selected for ExtractionConfiguration");

            return this;
        }

        public override void Execute()
        {
            base.Execute();
            Activator.Activate<ViewExtractionConfigurationSQLUI, SelectedDataSets>(_selectedDataSet);
        }
    }
}
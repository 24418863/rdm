// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using System.Windows.Forms;
using Rdmp.Core.DataExport.Data;
using Rdmp.UI.Icons.IconProvision;
using Rdmp.UI.ItemActivation;
using Rdmp.UI.SimpleDialogs;
using ReusableLibraryCode.CommandExecution.AtomicCommands;
using ReusableLibraryCode.Icons.IconProvision;

namespace Rdmp.UI.CommandExecution.AtomicCommands.CohortCreationCommands
{
    internal class ExecuteCommandImportAlreadyExistingCohort : BasicUICommandExecution,IAtomicCommand
    {
        private readonly ExternalCohortTable _externalCohortTable;

        public ExecuteCommandImportAlreadyExistingCohort(IActivateItems activator, ExternalCohortTable externalCohortTable):base(activator)
        {
            _externalCohortTable = externalCohortTable;
        }

        public override void Execute()
        {
            base.Execute();

            SelectWhichCohortToImportUI importDialog = new SelectWhichCohortToImportUI(Activator,_externalCohortTable);

            if (importDialog.ShowDialog() == DialogResult.OK)
            {
                int toAddID = importDialog.IDToImport;
                try
                {
                    var newCohort = new ExtractableCohort(Activator.RepositoryLocator.DataExportRepository, _externalCohortTable, toAddID);
                    Publish(_externalCohortTable);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message);
                }
            }
        }

        public override Image GetImage(IIconProvider iconProvider)
        {
            return iconProvider.GetImage(RDMPConcept.CohortAggregate, OverlayKind.Import);
        }
    }
}
// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using System.Linq;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.UI.Icons.IconProvision;
using Rdmp.UI.ItemActivation;
using ReusableLibraryCode.CommandExecution.AtomicCommands;
using ReusableLibraryCode.Icons.IconProvision;

namespace Rdmp.UI.CommandExecution.AtomicCommands
{
    internal class ExecuteCommandAddNewAggregateGraph : BasicUICommandExecution,IAtomicCommand
    {
        private readonly Catalogue _catalogue;

        public ExecuteCommandAddNewAggregateGraph(IActivateItems activator, Catalogue catalogue) : base(activator)
        {
            _catalogue = catalogue;

            if(_catalogue.GetAllExtractionInformation(ExtractionCategory.Any).All(ei=>ei.ColumnInfo == null))
                SetImpossible("Catalogue has no extractable columns");
        }

        public override string GetCommandHelp()
        {
            return "Add a new graph for understanding the data in a dataset e.g. number of records per year";
        }

        public override void Execute()
        {
            base.Execute();

            var newAggregate = new AggregateConfiguration(Activator.RepositoryLocator.CatalogueRepository,_catalogue,"New Aggregate " + Guid.NewGuid());
            Publish(_catalogue);
            Activate(newAggregate);
        }

        public override Image GetImage(IIconProvider iconProvider)
        {
            return iconProvider.GetImage(RDMPConcept.AggregateGraph, OverlayKind.Add);
        }
    }
}
﻿// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Governance;
using Rdmp.UI.ItemActivation;
using System.Linq;

namespace Rdmp.UI.CommandExecution.AtomicCommands
{
    internal class ExecuteCommandAddCatalogueToGovernancePeriod:BasicUICommandExecution
    {
        private GovernancePeriod _governancePeriod;
        private ICatalogue[] _catalogues;

        public ExecuteCommandAddCatalogueToGovernancePeriod(IActivateItems activator, GovernancePeriod governancePeriod, ICatalogue c):base(activator)
        {
            _governancePeriod = governancePeriod;
            _catalogues = new []{c};

            if(_governancePeriod.GovernedCatalogues.Contains(c))
                SetImpossible("Catalogue is already governed by that period");
        }
        public ExecuteCommandAddCatalogueToGovernancePeriod(IActivateItems activator, GovernancePeriod governancePeriod, ICatalogue[] catalogues):base(activator)
        {
            _governancePeriod = governancePeriod;
            _catalogues = catalogues;
            _catalogues = catalogues.Except(_governancePeriod.GovernedCatalogues).ToArray();
            
            if(!_catalogues.Any())
                SetImpossible("All Catalogues are already in the Governance Period");
        }

        public override void Execute()
        {
            base.Execute();

            foreach(var catalogue in _catalogues)
                Activator.RepositoryLocator.CatalogueRepository.GovernanceManager.Link(_governancePeriod,catalogue);

            Publish(_governancePeriod);
        }
    }
}
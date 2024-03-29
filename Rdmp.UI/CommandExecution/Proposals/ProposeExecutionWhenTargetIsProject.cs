// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using Rdmp.Core.DataExport.Data;
using Rdmp.UI.CommandExecution.AtomicCommands;
using Rdmp.UI.Copying.Commands;
using Rdmp.UI.ItemActivation;
using ReusableLibraryCode.CommandExecution;
using ReusableUIComponents.CommandExecution;

namespace Rdmp.UI.CommandExecution.Proposals
{
    class ProposeExecutionWhenTargetIsProject:RDMPCommandExecutionProposal<Project>
    {
        public ProposeExecutionWhenTargetIsProject(IActivateItems itemActivator) : base(itemActivator)
        {
        }

        public override bool CanActivate(Project target)
        {
            return true;
        }

        public override void Activate(Project target)
        {
            ItemActivator.Activate<ProjectUI.ProjectUI, Project>(target);
        }

        public override ICommandExecution ProposeExecution(ICommand cmd, Project project, InsertOption insertOption = InsertOption.Default)
        {
            //drop a cic on a Project to associate it with that project
            var cicCommand = cmd as CohortIdentificationConfigurationCommand;
            if(cicCommand != null)
                return new ExecuteCommandAssociateCohortIdentificationConfigurationWithProject(ItemActivator).SetTarget(cicCommand.CohortIdentificationConfiguration).SetTarget(project);

            var cataCommand = cmd as CatalogueCommand;
            if (cataCommand != null)
                return new ExecuteCommandMakeCatalogueProjectSpecific(ItemActivator).SetTarget(cataCommand.Catalogue).SetTarget(project);

            var file = cmd as FileCollectionCommand;

            if(file != null)
                return new ExecuteCommandCreateNewCatalogueByImportingFile(ItemActivator,file).SetTarget(project);

            var aggCommand = cmd as AggregateConfigurationCommand;
            
            if(aggCommand != null)
                return new ExecuteCommandCreateNewCatalogueByExecutingAnAggregateConfiguration(ItemActivator).SetTarget(project).SetTarget(aggCommand.Aggregate);


            return null;
        }
    }
}

// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Linq;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Providers.Nodes.LoadMetadataNodes;
using Rdmp.UI.CommandExecution.AtomicCommands;
using Rdmp.UI.Copying.Commands;
using Rdmp.UI.ItemActivation;
using ReusableLibraryCode.CommandExecution;
using ReusableUIComponents.CommandExecution;

namespace Rdmp.UI.CommandExecution.Proposals
{
    class ProposeExecutionWhenTargetIsLoadStageNode:RDMPCommandExecutionProposal<LoadStageNode>
    {
        public ProposeExecutionWhenTargetIsLoadStageNode(IActivateItems itemActivator) : base(itemActivator)
        {
        }
        
        public override bool CanActivate(LoadStageNode target)
        {
            return false;
        }

        public override void Activate(LoadStageNode target)
        {
            
        }

        public override ICommandExecution ProposeExecution(ICommand cmd, LoadStageNode targetStage, InsertOption insertOption = InsertOption.Default)
        {
            var sourceProcessTaskCommand = cmd as ProcessTaskCommand;
            var sourceFileTaskCommand = cmd as FileCollectionCommand;
            
            if (sourceProcessTaskCommand != null)
                return new ExecuteCommandChangeLoadStage(ItemActivator, sourceProcessTaskCommand, targetStage);

            if (sourceFileTaskCommand != null && sourceFileTaskCommand.Files.Length == 1)
            {

                var f = sourceFileTaskCommand.Files.Single();

                if(f.Extension == ".sql")
                    return new ExecuteCommandCreateNewProcessTask(ItemActivator, ProcessTaskType.SQLFile,targetStage.LoadMetadata, targetStage.LoadStage,f);


                if (f.Extension == ".exe")
                    return new ExecuteCommandCreateNewProcessTask(ItemActivator, ProcessTaskType.Executable, targetStage.LoadMetadata, targetStage.LoadStage, f);
            }


            return null;
        }
    }
}

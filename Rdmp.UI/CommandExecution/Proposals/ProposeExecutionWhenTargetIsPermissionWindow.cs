// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using Rdmp.Core.Curation.Data;
using Rdmp.UI.CommandExecution.AtomicCommands;
using Rdmp.UI.Copying.Commands;
using Rdmp.UI.DataLoadUIs.LoadMetadataUIs.LoadProgressAndCacheUIs;
using Rdmp.UI.ItemActivation;
using ReusableLibraryCode.CommandExecution;
using ReusableUIComponents.CommandExecution;

namespace Rdmp.UI.CommandExecution.Proposals
{
    class ProposeExecutionWhenTargetIsPermissionWindow : RDMPCommandExecutionProposal<PermissionWindow>
    {
        public ProposeExecutionWhenTargetIsPermissionWindow(IActivateItems itemActivator) : base(itemActivator)
        {
        }

        public override bool CanActivate(PermissionWindow target)
        {
            return true;
        }

        public override void Activate(PermissionWindow target)
        {
            ItemActivator.Activate<PermissionWindowUI, PermissionWindow>(target);
        }

        public override ICommandExecution ProposeExecution(ICommand cmd, PermissionWindow target, InsertOption insertOption = InsertOption.Default)
        {
            var cacheProgressCommand = cmd as CacheProgressCommand;
            if(cacheProgressCommand != null)
                return new ExecuteCommandSetPermissionWindow(ItemActivator,cacheProgressCommand.CacheProgress).SetTarget(target);

            return null;
        }
    }
}
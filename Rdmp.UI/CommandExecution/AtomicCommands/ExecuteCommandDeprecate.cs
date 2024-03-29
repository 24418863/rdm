// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using MapsDirectlyToDatabaseTable;
using Rdmp.Core.Curation.Data;
using Rdmp.UI.ItemActivation;
using ReusableLibraryCode.CommandExecution.AtomicCommands;

namespace Rdmp.UI.CommandExecution.AtomicCommands
{
    public class ExecuteCommandDeprecate : BasicUICommandExecution,IAtomicCommand
    {
        private readonly IMightBeDeprecated _o;
        private bool _desiredState;

        public ExecuteCommandDeprecate(IActivateItems itemActivator, IMightBeDeprecated o) : this(itemActivator,o,true)
        {
            
        }

        public ExecuteCommandDeprecate(IActivateItems itemActivator, IMightBeDeprecated o, bool desiredState) : base(itemActivator)
        {
            _o = o;
            _desiredState = desiredState;
        }

        public override string GetCommandName()
        {
            return _desiredState ? "Deprecate" : "UnDeprecate";
        }

        public override void Execute()
        {
            base.Execute();

            _o.IsDeprecated = _desiredState;
            _o.SaveToDatabase();
            Publish((DatabaseEntity)_o);
        }
    }
}
// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Windows.Forms;
using BrightIdeasSoftware;
using Rdmp.Core.Curation.Data;
using Rdmp.UI.ItemActivation;

namespace Rdmp.UI.Menus
{
    /// <summary>
    /// Constructor arguments for <see cref="RDMPContextMenuStrip"/>
    /// </summary>
    public class RDMPContextMenuStripArgs
    {
        public IActivateItems ItemActivator { get; set; }
        public object CurrentlyPinnedObject { get; set; }
        public IMasqueradeAs Masquerader { get; set; }
        
        public TreeListView Tree { get; set; }
        public object Model { get; set; }
        
        public RDMPContextMenuStripArgs(IActivateItems itemActivator)
        {
            ItemActivator = itemActivator;
        }

        public RDMPContextMenuStripArgs(IActivateItems itemActivator, TreeListView tree, object model):this(itemActivator)
        {
            Tree = tree;
            Model = model;
        }

        /// <summary>
        /// Returns the first Parent control of <see cref="Tree"/> in the Windows Forms Controls Parent hierarchy which is Type T
        /// 
        /// <para>returns null if no Parent is found of the supplied Type </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetTreeParentControlOfType<T>() where T : Control
        {
            var p = Tree.Parent;

            while (p != null)
            {
                if (p is T)
                    return (T)p;

                p = p.Parent;
            }

            return null;
        }
    }
}
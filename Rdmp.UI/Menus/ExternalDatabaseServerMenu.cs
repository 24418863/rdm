// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Windows.Forms;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Databases;
using Rdmp.Core.Logging;
using Rdmp.UI.CommandExecution.AtomicCommands;
using Rdmp.UI.DataViewing;
using Rdmp.UI.DataViewing.Collections.Arbitrary;
using Rdmp.UI.Icons.IconProvision;
using ReusableLibraryCode.DataAccess;

namespace Rdmp.UI.Menus
{
    class ExternalDatabaseServerMenu : RDMPContextMenuStrip
    {
        private readonly ExternalDatabaseServer _server;

        public ExternalDatabaseServerMenu(RDMPContextMenuStripArgs args, ExternalDatabaseServer server) : base(args, server)
        {
            _server = server;
            if (server.WasCreatedBy(new LoggingDatabasePatcher()))
            {
                var viewLogs = new ToolStripMenuItem("View Logs",CatalogueIcons.Logging);
                Add(new ExecuteCommandViewLoggedData(_activator, new LogViewerFilter(LoggingTables.DataLoadTask)), Keys.None,viewLogs);
                Add(new ExecuteCommandViewLoggedData(_activator, new LogViewerFilter(LoggingTables.DataLoadRun)), Keys.None, viewLogs);
                Add(new ExecuteCommandViewLoggedData(_activator, new LogViewerFilter(LoggingTables.FatalError)), Keys.None, viewLogs);
                Add(new ExecuteCommandViewLoggedData(_activator, new LogViewerFilter(LoggingTables.TableLoadRun)), Keys.None, viewLogs);
                Add(new ExecuteCommandViewLoggedData(_activator, new LogViewerFilter(LoggingTables.DataSource)), Keys.None, viewLogs);
                Add(new ExecuteCommandViewLoggedData(_activator, new LogViewerFilter(LoggingTables.ProgressLog)), Keys.None, viewLogs);

                viewLogs.DropDownItems.Add(new ToolStripSeparator());

                viewLogs.DropDownItems.Add(new ToolStripMenuItem("Query with SQL", CatalogueIcons.SQL, ExecuteSqlOnLoggingDatabase));
                
                Items.Add(viewLogs);
            }
        }

        private void ExecuteSqlOnLoggingDatabase(object sender, EventArgs e)
        {
            var collection = new ArbitraryTableExtractionUICollection(_server.Discover(DataAccessContext.Logging).ExpectTable("DataLoadTask"));
            _activator.Activate<ViewSQLAndResultsWithDataGridUI>(collection);
        }
    }
}

// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Data.SqlClient;
using System.Drawing;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Cohort;
using Rdmp.Core.Curation.Data.Defaults;
using Rdmp.Core.Databases;
using Rdmp.UI.Icons.IconProvision;
using Rdmp.UI.ItemActivation;
using Rdmp.UI.Versioning;
using ReusableLibraryCode.CommandExecution.AtomicCommands;
using ReusableLibraryCode.Icons.IconProvision;

namespace Rdmp.UI.CommandExecution.AtomicCommands
{
    public class ExecuteCommandCreateNewQueryCacheDatabase : BasicUICommandExecution,IAtomicCommand
    {
        private readonly CohortIdentificationConfiguration _cic;
        
        public ExecuteCommandCreateNewQueryCacheDatabase(IActivateItems activator, CohortIdentificationConfiguration configuration):base(activator)
        {
            _cic = configuration;
            if(_cic.QueryCachingServer_ID != null)
                SetImpossible("CohortIdentificationConfiguration already has a Query Cache configured");
        }

        public override void Execute()
        {
            base.Execute();

            var p = new QueryCachingPatcher();

            CreatePlatformDatabase createPlatform = new CreatePlatformDatabase(p);
            createPlatform.ShowDialog();

            if (!string.IsNullOrWhiteSpace(createPlatform.DatabaseConnectionString))
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(createPlatform.DatabaseConnectionString);

                var newServer = new ExternalDatabaseServer(Activator.RepositoryLocator.CatalogueRepository, "Caching Database", p);

                newServer.Server = builder.DataSource;
                newServer.Database = builder.InitialCatalog;

                //if there is a username/password
                if (!builder.IntegratedSecurity)
                {
                    newServer.Password = builder.Password;
                    newServer.Username = builder.UserID;
                }
                newServer.SaveToDatabase();

                _cic.QueryCachingServer_ID = newServer.ID;
                _cic.SaveToDatabase();

                SetDefaultIfNotExists(newServer,PermissableDefaults.CohortIdentificationQueryCachingServer_ID,true);
                
                Publish(_cic);
            }
        }

        public override Image GetImage(IIconProvider iconProvider)
        {
            return iconProvider.GetImage(RDMPConcept.ExternalDatabaseServer, OverlayKind.Add);
        }
    }
}
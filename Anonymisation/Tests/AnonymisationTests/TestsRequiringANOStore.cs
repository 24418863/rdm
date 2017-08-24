﻿using System;
using System.Data.SqlClient;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using MapsDirectlyToDatabaseTable.Versioning;
using NUnit.Framework;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using Tests.Common;

namespace AnonymisationTests
{
    public class TestsRequiringANOStore:DatabaseTests
    {
        protected ExternalDatabaseServer ANOStore_ExternalDatabaseServer { get; set; }
        protected SqlConnectionStringBuilder ANOStore_ConnectionStringBuilder { get; set; }
        protected string ANOStore_DatabaseName = TestDatabaseNames.GetConsistentName("ANOStore");

        [TestFixtureSetUp]
        public void Setup()
        {
            ANOStore_ConnectionStringBuilder = new SqlConnectionStringBuilder(ServerICanCreateRandomDatabasesAndTablesOn.ConnectionString);
            ANOStore_ConnectionStringBuilder.InitialCatalog = "";

            CreateANODatabase();

            CreateReferenceInCatalogueToANODatabase();

            var t = typeof (ANOStore.Class1);
            Console.WriteLine(t.Name);
        }

        private void DropANODatabase()
        {
            var database = new DiscoveredServer(ANOStore_ConnectionStringBuilder).ExpectDatabase(ANOStore_DatabaseName);
            if (database != null && database.Exists())
                database.ForceDrop();
        }

        [TestFixtureTearDown]
        public virtual void FixtureTearDown()
        {
            RemovePreExistingReference();

            // Remove the database from the server
            DropANODatabase();
        }

        private void CreateANODatabase()
        {
            DropANODatabase();

            var ano = typeof(ANOStore.Database.Class1);
            Console.WriteLine("Was ANOStore.Database.dll also loaded into memory:" + ano);

            var scriptCreate = new MasterDatabaseScriptExecutor(ANOStore_ConnectionStringBuilder.DataSource, ANOStore_DatabaseName, ANOStore_ConnectionStringBuilder.UserID, ANOStore_ConnectionStringBuilder.Password);
            scriptCreate.CreateAndPatchDatabase(typeof(ANOStore.Class1).Assembly, new ThrowImmediatelyCheckNotifier());
            ANOStore_ConnectionStringBuilder.InitialCatalog = ANOStore_DatabaseName;
        }

        private void CreateReferenceInCatalogueToANODatabase()
        {
            RemovePreExistingReference();

            //now create a new reference!
            ANOStore_ExternalDatabaseServer = new ExternalDatabaseServer(CatalogueRepository, ANOStore_DatabaseName,typeof(ANOStore.Class1).Assembly);
            ANOStore_ExternalDatabaseServer.Database = ANOStore_ConnectionStringBuilder.InitialCatalog;
            ANOStore_ExternalDatabaseServer.Server = ANOStore_ConnectionStringBuilder.DataSource;

            //may be null
            ANOStore_ExternalDatabaseServer.Username = ANOStore_ConnectionStringBuilder.UserID;
            ANOStore_ExternalDatabaseServer.Password = ANOStore_ConnectionStringBuilder.Password;

            ANOStore_ExternalDatabaseServer.SaveToDatabase();
            
        }

        private void RemovePreExistingReference()
        {
            //There will likely be an old reference to the external database server
            var preExisting = CatalogueRepository.GetAllObjects<ExternalDatabaseServer>().SingleOrDefault(e => e.Name.Equals(ANOStore_DatabaseName));

            if (preExisting == null) return;

            //Some child tests will likely create ANOTables that reference this server so we need to cleanup those for them so that we can cleanup the old server reference too
            foreach (var lingeringTablesReferencingServer in CatalogueRepository.GetAllObjects<ANOTable>().Where(a => a.Server_ID == preExisting.ID))
            {
                //unhook the anonymisation transform from any ColumnInfos using it
                foreach (ColumnInfo colWithANOTransform in CatalogueRepository.GetAllObjects<ColumnInfo>().Where(c => c.ANOTable_ID == lingeringTablesReferencingServer.ID))
                {
                    Console.WriteLine("Unhooked ColumnInfo " + colWithANOTransform + " from ANOTable " + lingeringTablesReferencingServer);
                    colWithANOTransform.ANOTable_ID = null;
                    colWithANOTransform.SaveToDatabase();
                }
                
                TruncateANOTable(lingeringTablesReferencingServer);
                lingeringTablesReferencingServer.DeleteInDatabase();
            }

            //now delete the old server reference
            preExisting.DeleteInDatabase();
        }

        protected void TruncateANOTable(ANOTable anoTable)
        {
            Console.WriteLine("Truncating table " + anoTable.TableName + " on server " + ANOStore_ExternalDatabaseServer);
            SqlConnection con = new SqlConnection(ANOStore_ConnectionStringBuilder.ConnectionString);
            con.Open();
            SqlCommand cmdDelete = new SqlCommand("if exists (select top 1 * from sys.tables where name ='" + anoTable.TableName + "') TRUNCATE TABLE " + anoTable.TableName, con);
            cmdDelete.ExecuteNonQuery();
            con.Close();
        
        }
    }
}
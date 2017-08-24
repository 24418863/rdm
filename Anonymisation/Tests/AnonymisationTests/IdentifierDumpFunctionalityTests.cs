﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.DataHelper;
using DataLoadEngine.DataFlowPipeline.Components.Anonymisation;
using Diagnostics.TestData;
using MapsDirectlyToDatabaseTable.Relationships;
using NUnit.Framework;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DataAccess;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using Tests.Common;

namespace AnonymisationTests
{
    public class IdentifierDumpFunctionalityTests:TestsRequiringFullAnonymisationSuite
    {
        private TableInfo tableInfoCreated;
        ColumnInfo[] columnInfosCreated;
        
        BulkTestsData _bulkData;

        [TestFixtureSetUp]
        public void SetupBulkTestData()
        {
            Console.WriteLine("Cleaning up remnants");
            Cleanup();

            Console.WriteLine("Setting up bulk test data");
            _bulkData = new BulkTestsData(RepositoryLocator.CatalogueRepository, DatabaseICanCreateRandomTablesIn);
            _bulkData.SetupTestData();
            
            Console.WriteLine("Importing to Catalogue");
            TableInfoImporter importer = new TableInfoImporter(CatalogueRepository, _bulkData.BulkDataBuilder.DataSource,
                _bulkData.BulkDataBuilder.InitialCatalog,
                BulkTestsData.BulkDataTable, DatabaseType.MicrosoftSQLServer,
                username: _bulkData.BulkDataBuilder.UserID,password: _bulkData.BulkDataBuilder.Password);

            importer.DoImport(out tableInfoCreated,out columnInfosCreated);
            
            Console.WriteLine("Imported TableInfo " + tableInfoCreated);
            Console.WriteLine("Imported ColumnInfos " + string.Join(",",columnInfosCreated.Select(c=>c.GetRuntimeName())));

            Assert.NotNull(tableInfoCreated);

            ColumnInfo chi = columnInfosCreated.Single(c => c.GetRuntimeName().Equals("chi"));

            Console.WriteLine("CHI is primary key? (expecting true):" + chi.IsPrimaryKey);
            Assert.IsTrue(chi.IsPrimaryKey);
        }


        #region tests that pass
        [Test]
        public void DumpAllIdentifiersInTable_Passes()
        {
            var preDiscardedColumn1 = new PreLoadDiscardedColumn(CatalogueRepository, tableInfoCreated, "surname")
            {
                Destination = DiscardedColumnDestination.StoreInIdentifiersDump,
                SqlDataType = "varchar(20)"
            };
            preDiscardedColumn1.SaveToDatabase();

            //give it the correct server
            tableInfoCreated.IdentifierDumpServer_ID = IdentifierDump_ExternalDatabaseServer.ID;
            tableInfoCreated.SaveToDatabase();

            IdentifierDumper dumper = new IdentifierDumper(tableInfoCreated);

            Dictionary<string,string> chiToSurnameDictionary = new Dictionary<string, string>();
            try
            {
                dumper.Check(new AcceptAllCheckNotifier());
                
                DataTable dt = _bulkData.GetDataTable(1000);

                Assert.AreEqual(1000,dt.Rows.Count);
                Assert.IsTrue(dt.Columns.Contains("surname"));

                //for checking the final ID table has the correct values in
                foreach (DataRow row in dt.Rows)
                    chiToSurnameDictionary.Add(row["chi"].ToString(), row["surname"] as string);

                dumper.CreateSTAGINGTable();
                dumper.DumpAllIdentifiersInTable(dt);
                dumper.DropStaging();

                //confirm that the surname column is no longer in the pipeline
                Assert.IsFalse(dt.Columns.Contains("surname"));

                //now look at the ids in the identifier dump and make sure they match what was in the pipeline before we sent it
                using(var con = new SqlConnection(IdentifierDump_ConnectionStringBuilder.ConnectionString))
                {
                    con.Open();

                    SqlCommand cmd = new SqlCommand("Select * from " + "ID_" + BulkTestsData.BulkDataTable,con);
                    SqlDataReader r = cmd.ExecuteReader();
                    
                    //make sure the values in the ID table match the ones we originally had in the pipeline
                    while (r.Read())
                        if (chiToSurnameDictionary[r["chi"].ToString()] == null )
                            Assert.IsTrue(r["surname"] == DBNull.Value);
                        else
                            Assert.AreEqual(chiToSurnameDictionary[r["chi"].ToString()], r["surname"]);
                    r.Close();

                    //leave the identifier dump in the way we found it (empty)
                    SqlCommand cmdDrop = new SqlCommand("DROP TABLE ID_" + BulkTestsData.BulkDataTable,con);
                    cmdDrop.ExecuteNonQuery();

                    SqlCommand cmdDropArchive = new SqlCommand("DROP TABLE ID_" + BulkTestsData.BulkDataTable + "_Archive", con);
                    cmdDropArchive.ExecuteNonQuery();
                }
            }
            finally
            {
                preDiscardedColumn1.DeleteInDatabase();
                tableInfoCreated.IdentifierDumpServer_ID = null;//reset it back to how it was when we found it
                tableInfoCreated.SaveToDatabase();

            }

        }


        #endregion


        #region tests that throw
        [Test]
        [ExpectedException(ExpectedMessage = "Column forename was found in the IdentifierDump table ID_BulkData but was not one of the primary keys or a PreLoadDiscardedColumn")]
        public void DumpAllIdentifiersInTable_UnexpectedColumnFoundInIdentifierDumpTable()
        {
            var preDiscardedColumn1 = new PreLoadDiscardedColumn(CatalogueRepository, tableInfoCreated, "surname");
            preDiscardedColumn1.Destination = DiscardedColumnDestination.StoreInIdentifiersDump;
            preDiscardedColumn1.SqlDataType = "varchar(20)";
            preDiscardedColumn1.SaveToDatabase();

            var preDiscardedColumn2 = new PreLoadDiscardedColumn(CatalogueRepository, tableInfoCreated, "forename");
            preDiscardedColumn2.Destination = DiscardedColumnDestination.StoreInIdentifiersDump;
            preDiscardedColumn2.SqlDataType = "varchar(50)";
            preDiscardedColumn2.SaveToDatabase();
            
            //give it the correct server
            tableInfoCreated.IdentifierDumpServer_ID = IdentifierDump_ExternalDatabaseServer.ID;
            tableInfoCreated.SaveToDatabase();

            IdentifierDumper dumper = new IdentifierDumper(tableInfoCreated);
            dumper.Check(new AcceptAllCheckNotifier());

            DiscoveredTable tableInDump = new DiscoveredServer(IdentifierDump_ConnectionStringBuilder).ExpectDatabase(IdentifierDump_DatabaseName).ExpectTable("ID_" + BulkTestsData.BulkDataTable);
            Assert.IsTrue(tableInDump.Exists(), "ID table did not exist");


            var columnsInDump = tableInDump.DiscoverColumns().Select(c=>c.GetRuntimeName()).ToArray();
            //works and creates table on server
            Assert.Contains("hic_validFrom",columnsInDump);
            Assert.Contains("forename", columnsInDump);
            Assert.Contains("chi", columnsInDump);
            Assert.Contains("surname", columnsInDump);

            //now delete it!
            preDiscardedColumn2.DeleteInDatabase();

            //now create a new dumper and watch it go crazy 
            IdentifierDumper dumper2 = new IdentifierDumper(tableInfoCreated);

            var thrower = new ThrowImmediatelyCheckNotifier();
            thrower.ThrowOnWarning = true;

            try
            {
                dumper2.Check(thrower);
            }
            finally
            {
                //Drop all this stuff
                using (var con = new SqlConnection(IdentifierDump_ConnectionStringBuilder.ConnectionString))
                {
                    con.Open();
                    
                    //leave the identifier dump in the way we found it (empty)
                    SqlCommand cmdDrop = new SqlCommand("DROP TABLE ID_" + BulkTestsData.BulkDataTable, con);
                    cmdDrop.ExecuteNonQuery();

                    SqlCommand cmdDropArchive = new SqlCommand("DROP TABLE ID_" + BulkTestsData.BulkDataTable + "_Archive", con);
                    cmdDropArchive.ExecuteNonQuery();
                }

                preDiscardedColumn1.DeleteInDatabase();
                tableInfoCreated.IdentifierDumpServer_ID = null;//reset it back to how it was when we found it
                tableInfoCreated.SaveToDatabase();

            }

        }

        [Test]
        [ExpectedException(ExpectedMessage = "IdentifierDumper STAGING insert (ID_BulkData_STAGING) failed, make sure you have called CreateSTAGINGTable() before trying to Dump identifiers (also you should call DropStagging() when you are done)")]
        public void IdentifierDumperCheckFails_StagingNotCalled()
        {
            var preDiscardedColumn1 = new PreLoadDiscardedColumn(CatalogueRepository, tableInfoCreated, "forename");
            preDiscardedColumn1.Destination = DiscardedColumnDestination.StoreInIdentifiersDump;
            preDiscardedColumn1.SqlDataType = "varchar(50)";
            preDiscardedColumn1.SaveToDatabase();

            //give it the correct server
            tableInfoCreated.IdentifierDumpServer_ID = IdentifierDump_ExternalDatabaseServer.ID;
            tableInfoCreated.SaveToDatabase();

            IdentifierDumper dumper = new IdentifierDumper(tableInfoCreated);
            try
            {
                dumper.Check(new AcceptAllCheckNotifier());
                dumper.DumpAllIdentifiersInTable(_bulkData.GetDataTable(10));
            }
            finally
            {
                preDiscardedColumn1.DeleteInDatabase();
                tableInfoCreated.IdentifierDumpServer_ID = null;//reset it back to how it was when we found it
                tableInfoCreated.SaveToDatabase();
            }
        }

        [Test]
        public void IdentifierDumperCheckFails_NoTableExists()
        {
            var preDiscardedColumn1 = new PreLoadDiscardedColumn(CatalogueRepository, tableInfoCreated, "forename");
            preDiscardedColumn1.Destination = DiscardedColumnDestination.StoreInIdentifiersDump;
            preDiscardedColumn1.SqlDataType = "varchar(50)";
            preDiscardedColumn1.SaveToDatabase();

            //give it the correct server
            tableInfoCreated.IdentifierDumpServer_ID = IdentifierDump_ExternalDatabaseServer.ID;
            tableInfoCreated.SaveToDatabase();

            var existingTable = DataAccessPortal.GetInstance()
                .ExpectDatabase(IdentifierDump_ExternalDatabaseServer, DataAccessContext.InternalDataProcessing)
                .ExpectTable("ID_BulkData");

            if(existingTable.Exists())
                existingTable.Drop();

            IdentifierDumper dumper = new IdentifierDumper(tableInfoCreated);

            try
            {
                ToMemoryCheckNotifier notifier = new ToMemoryCheckNotifier(new AcceptAllCheckNotifier());
                dumper.Check(notifier);

                Assert.IsTrue(notifier.Messages.Any(m=>
                    m.Result == CheckResult.Fail 
                    &&
                    m.Message.Contains("Table ID_BulkData was not found")));
            }
            finally
            {
                preDiscardedColumn1.DeleteInDatabase();
                tableInfoCreated.IdentifierDumpServer_ID = null;//reset it back to how it was when we found it
                tableInfoCreated.SaveToDatabase();
            }
        }

        [Test]
        public void IdentifierDumperCheckFails_ServerIsNotADumpServer()
        {
            var preDiscardedColumn1 = new PreLoadDiscardedColumn(CatalogueRepository, tableInfoCreated, "NationalSecurityNumber");
            preDiscardedColumn1.Destination = DiscardedColumnDestination.StoreInIdentifiersDump;
            preDiscardedColumn1.SqlDataType = "varchar(10)";
            preDiscardedColumn1.SaveToDatabase();
            
            //give it the WRONG server
            tableInfoCreated.IdentifierDumpServer_ID = ANOStore_ExternalDatabaseServer.ID;
            tableInfoCreated.SaveToDatabase();
            
            IdentifierDumper dumper = new IdentifierDumper(tableInfoCreated);
            try
            {
                dumper.Check(new ThrowImmediatelyCheckNotifier());
                Assert.Fail("Expected it to crash before now");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.StartsWith("Exception occurred when trying to find stored procedure sp_createIdentifierDump"));
                Assert.IsTrue(ex.InnerException.Message.StartsWith("Connected successfully to server"));
                Assert.IsTrue(ex.InnerException.Message.EndsWith(" but did not find the stored procedure sp_createIdentifierDump in the database (Possibly the ExternalDatabaseServer is not an IdentifierDump database?)"));
            }
            finally
            {
                preDiscardedColumn1.DeleteInDatabase();
                tableInfoCreated.IdentifierDumpServer_ID = null;//reset it back to how it was when we found it
                tableInfoCreated.SaveToDatabase();
            }

        }

        [Test]
        [ExpectedException(ExpectedMessage ="does not have a listed IdentifierDump ExternalDatabaseServer" ,MatchType = MessageMatch.Contains)]
        public void IdentifierDumperCheckFails_NoTableOnServerRejectChange()
        {
            var preDiscardedColumn1 = new PreLoadDiscardedColumn(CatalogueRepository, tableInfoCreated, "NationalSecurityNumber");
            try
            {
                preDiscardedColumn1.Destination = DiscardedColumnDestination.StoreInIdentifiersDump;
                preDiscardedColumn1.SqlDataType = "varchar(10)";
                preDiscardedColumn1.SaveToDatabase();

                IdentifierDumper dumper = new IdentifierDumper(tableInfoCreated);
           
                dumper.Check(new ThrowImmediatelyCheckNotifier());
            }
            finally
            {
                preDiscardedColumn1.DeleteInDatabase();
            }
        }

        [Test]
        public void IdentifierDumperCheckFails_LieAboutDatatype()
        {
            var preDiscardedColumn1 = new PreLoadDiscardedColumn(CatalogueRepository, tableInfoCreated, "forename");
            preDiscardedColumn1.Destination = DiscardedColumnDestination.StoreInIdentifiersDump;
            preDiscardedColumn1.SqlDataType = "varchar(50)";
            preDiscardedColumn1.SaveToDatabase();
            try
            {
                //give it the correct server
                tableInfoCreated.IdentifierDumpServer_ID = IdentifierDump_ExternalDatabaseServer.ID;
                tableInfoCreated.SaveToDatabase();

                IdentifierDumper dumper = new IdentifierDumper(tableInfoCreated);
            
                //table doesnt exist yet it should work
                dumper.Check(new AcceptAllCheckNotifier());
            
                //now it is varbinary
                preDiscardedColumn1.SqlDataType = "varbinary(200)";
                preDiscardedColumn1.SaveToDatabase();
             
                //get a new dumper because we have changed the pre load discarded column
                dumper = new IdentifierDumper(tableInfoCreated);
                //table doesnt exist yet it should work
                Exception ex = Assert.Throws<Exception>(()=>dumper.Check(new ThrowImmediatelyCheckNotifier()));

                Assert.IsTrue(ex.Message.Contains("has data type varbinary(200) in the Catalogue but appears as varchar(50) in the actual IdentifierDump"));
            }
            finally
            {
                preDiscardedColumn1.DeleteInDatabase();
                tableInfoCreated.IdentifierDumpServer_ID = null;//reset it back to how it was when we found it
                tableInfoCreated.SaveToDatabase();
            }
            
        }

        #endregion

        [TestFixtureTearDown]
        public void Cleanup()
        {
            foreach (var cata in CatalogueRepository.GetAllObjects<Catalogue>().Where(c => c.Name.Equals(BulkTestsData.BulkDataTable)))
                cata.DeleteInDatabase();
            
            foreach (TableInfo toCleanup in CatalogueRepository.GetAllObjects<TableInfo>().Where(t => t.GetRuntimeName().Equals(BulkTestsData.BulkDataTable)))
            {
                foreach (PreLoadDiscardedColumn column in toCleanup.PreLoadDiscardedColumns)
                    column.DeleteInDatabase();


                var credentials = toCleanup.GetCredentialsIfExists(DataAccessContext.InternalDataProcessing);
                toCleanup.DeleteInDatabase();

                if(credentials != null)
                    try
                    {
                        credentials.DeleteInDatabase();
                    }
                    catch (CredentialsInUseException e)
                    {
                        Console.WriteLine("Ignored credentials in use exception :" + e);
                    }
            }

            if (_bulkData != null)
                _bulkData.Destroy();
        }
    }
}
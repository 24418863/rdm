﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using CatalogueLibrary.Data.DataLoad;
using DataLoadEngine.DataFlowPipeline.Components.Anonymisation;
using NUnit.Framework;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using ReusableLibraryCode.Progress;
using Tests.Common;

namespace AnonymisationTests
{
    public class ANOTableTests:TestsRequiringANOStore
    {
        Regex _anochiPattern = new Regex(@"\d{10}_A");

        #region Create New ANOTables
        [Test]
        [TestCase("varchar(1)")]
        [TestCase("int")]
        [TestCase("tinyint")]
        [TestCase("bit")]
        public void CreateAnANOTable_PushAs(string datatypeForPush)
        {
            DiscoveredServer server = new DiscoveredServer(ANOStore_ConnectionStringBuilder);
            DiscoveredDatabase database = server.GetCurrentDatabase();

            var anoTable = GetANOTable();
            Assert.AreEqual("ANOMyTable", anoTable.TableName);
            anoTable.NumberOfCharactersToUseInAnonymousRepresentation =20;
            anoTable.NumberOfIntegersToUseInAnonymousRepresentation = 20;
            anoTable.PushToANOServerAsNewTable(datatypeForPush, new ThrowImmediatelyCheckNotifier());

            var discoveredTable = database.DiscoverTables(false).SingleOrDefault(t => t.GetRuntimeName().Equals("ANOMyTable"));
            
            //server should have 
            Assert.NotNull(discoveredTable);
            Assert.IsTrue(discoveredTable.Exists());

            //yes that's right hte table name and column name are the same here \|/
            Assert.AreEqual(datatypeForPush, discoveredTable.DiscoverColumn("MyTable").DataType.SQLType);

            //20 + 20 + _ + A
            Assert.AreEqual("varchar(42)", discoveredTable.DiscoverColumn("ANOMyTable").DataType.SQLType);

            anoTable.DeleteInDatabase();
        }

        [Test]
        public void CreateAnANOTable_Revertable()
        {
            var anoTable = GetANOTable();

            anoTable.NumberOfCharactersToUseInAnonymousRepresentation = 63;
            anoTable.RevertToDatabaseState();
            Assert.AreEqual(1,anoTable.NumberOfCharactersToUseInAnonymousRepresentation);
            anoTable.DeleteInDatabase();
        }

        [Test]
        public void CreateAnANOTable_Check()
        {
            var anoTable = GetANOTable();
            Assert.AreEqual("ANOMyTable", anoTable.TableName);
            anoTable.Check(new AcceptAllCheckNotifier());
            anoTable.DeleteInDatabase();
        }

        [Test]
        [ExpectedException(ExpectedMessage="ix_suffixMustBeUnique", MatchType = MessageMatch.Contains)]
        public void DuplicateSuffix_Throws()
        {
            var anoTable = GetANOTable();
            try
            {
                new ANOTable(CatalogueRepository, anoTable.Server, "DuplicateSuffix", anoTable.Suffix);
            }
            finally
            {
                anoTable.DeleteInDatabase();
            }
        }
        
        [Test]
        [ExpectedException(ExpectedMessage = "NumberOfCharactersToUseInAnonymousRepresentation cannot be negative")]
        public void CreateAnANOTable_CharCountNegative()
        {
            var anoTable = GetANOTable();
            try
            {
                anoTable.NumberOfCharactersToUseInAnonymousRepresentation = -500;
                anoTable.SaveToDatabase();
            }
            finally
            {
                anoTable.DeleteInDatabase();
            }
            
        }
        
        [Test]
        [ExpectedException(ExpectedMessage = "NumberOfIntegersToUseInAnonymousRepresentation cannot be negative")]
        public void CreateAnANOTable_IntCountNegative()
        {
            ANOTable anoTable = GetANOTable(); ;

            try
            {
                anoTable.NumberOfIntegersToUseInAnonymousRepresentation = -500;
                anoTable.SaveToDatabase();
            }
            finally
            {
                anoTable.DeleteInDatabase();
            }
            
        }

        [Test]
        [ExpectedException(ExpectedMessage = "Anonymous representations must have at least 1 integer or character")]
        public void CreateAnANOTable_TotalCountZero()
        {
            var anoTable = GetANOTable();
            try
            {
                anoTable.NumberOfIntegersToUseInAnonymousRepresentation = 0;
                anoTable.NumberOfCharactersToUseInAnonymousRepresentation = 0;
                anoTable.SaveToDatabase();
            }
            finally
            {
                anoTable.DeleteInDatabase();
            }
        }
        #endregion

        [Test]
        public void SubstituteANOIdentifiers_2CHINumbers()
        {
            var anoTable = GetANOTable();
            anoTable.NumberOfCharactersToUseInAnonymousRepresentation = 0;
            anoTable.NumberOfIntegersToUseInAnonymousRepresentation = 10;
            anoTable.PushToANOServerAsNewTable("varchar(10)",new ThrowImmediatelyCheckNotifier());


            DataTable dt = new DataTable();
            dt.Columns.Add("CHI");
            dt.Columns.Add("ANOCHI");

            dt.Rows.Add("0101010101",DBNull.Value);//duplicates
            dt.Rows.Add("0101010102",DBNull.Value);
            dt.Rows.Add("0101010101",DBNull.Value);//duplicates

            ANOTransformer transformer = new ANOTransformer(anoTable, new ToConsoleDataLoadEventReceiver());
            transformer.Transform(dt,dt.Columns["CHI"],dt.Columns["ANOCHI"]);

            Assert.IsTrue((string) dt.Rows[0][0] == "0101010101");
            Assert.IsTrue(_anochiPattern.IsMatch((string) dt.Rows[0][1]));//should be 10 digits and then _A
            Assert.AreEqual(dt.Rows[0][1], dt.Rows[2][1]);//because of duplication these should both be the same

            Console.WriteLine("ANO identifiers created were:" + dt.Rows[0][1] + "," +dt.Rows[1][1]);

            TruncateANOTable(anoTable);

            //now test previews
            transformer.Transform(dt,dt.Columns["CHI"],dt.Columns["ANOCHI"], true);
            var val1 = dt.Rows[0][1];

            transformer.Transform(dt, dt.Columns["CHI"], dt.Columns["ANOCHI"], true);
            var val2 = dt.Rows[0][1];

            transformer.Transform(dt, dt.Columns["CHI"], dt.Columns["ANOCHI"], true);
            var val3 = dt.Rows[0][1];

            //should always be different
            Assert.AreNotEqual(val1,val2);
            Assert.AreNotEqual(val1, val3);

            //now test repeatability
            transformer.Transform(dt, dt.Columns["CHI"], dt.Columns["ANOCHI"], false);
            var val4 = dt.Rows[0][1];

            transformer.Transform(dt, dt.Columns["CHI"], dt.Columns["ANOCHI"], false);
            var val5 = dt.Rows[0][1];

            transformer.Transform(dt, dt.Columns["CHI"], dt.Columns["ANOCHI"], false);
            var val6 = dt.Rows[0][1];
            Assert.AreEqual(val4,val5);
            Assert.AreEqual(val4, val6);

            TruncateANOTable(anoTable);
        
            anoTable.DeleteInDatabase();
        }

        [Test]
        public void SubstituteANOIdentifiers_PreviewWithoutPush()
        {
            
            var anoTable = GetANOTable();
            anoTable.NumberOfCharactersToUseInAnonymousRepresentation = 0;
            anoTable.NumberOfIntegersToUseInAnonymousRepresentation = 10;

            DiscoveredTable ANOtable = new DiscoveredServer(ANOStore_ConnectionStringBuilder).ExpectDatabase(ANOStore_ExternalDatabaseServer.Database).ExpectTable(anoTable.TableName);

            //should not exist yet
            Assert.False(ANOtable.Exists());
            
            DataTable dt = new DataTable();
            dt.Columns.Add("CHI");
            dt.Columns.Add("ANOCHI");
            dt.Rows.Add("0101010101", DBNull.Value);
            ANOTransformer transformer = new ANOTransformer(anoTable, new ToConsoleDataLoadEventReceiver());
            transformer.Transform(dt, dt.Columns["CHI"], dt.Columns["ANOCHI"], true);

            Assert.IsTrue(_anochiPattern.IsMatch((string)dt.Rows[0][1]));//should be 10 digits and then _A
            
            //still not exist yet
            Assert.False(ANOtable.Exists());

            anoTable.DeleteInDatabase();
        }


        [Test]
        public void SubstituteANOIdentifiers_BulkTest()
        {
            int batchSize = 10000;

            var anoTable = GetANOTable();
            anoTable.NumberOfCharactersToUseInAnonymousRepresentation = 0;
            anoTable.NumberOfIntegersToUseInAnonymousRepresentation = 10;
            anoTable.PushToANOServerAsNewTable("varchar(10)", new ThrowImmediatelyCheckNotifier());

            
            Stopwatch sw = new Stopwatch();
            sw.Start();

            DataTable dt = new DataTable();
            dt.Columns.Add("CHI");
            dt.Columns.Add("ANOCHI");

            Random r = new Random();

            HashSet<string> uniqueSourceSet = new HashSet<string>();


            for (int i = 0; i < batchSize; i++)
            {
                var val = r.NextDouble() * 9999999999;
                val = Math.Round(val);
                string valAsString = val.ToString();
                
                while (valAsString.Length < 10)
                    valAsString = "0" + valAsString;

                if (!uniqueSourceSet.Contains(valAsString))
                    uniqueSourceSet.Add(valAsString);

                dt.Rows.Add(valAsString, DBNull.Value);//duplicates    
            }
            Console.WriteLine("Time to allocate in C# memory:"+sw.Elapsed);
            Console.WriteLine("Allocated " + dt.Rows.Count + " identifiers (" + uniqueSourceSet.Count() + " unique ones)");

            sw.Reset();
            sw.Start();

            ANOTransformer transformer = new ANOTransformer(anoTable, new ToConsoleDataLoadEventReceiver());
            transformer.Transform(dt, dt.Columns["CHI"], dt.Columns["ANOCHI"]);
            Console.WriteLine("Time to perform SQL transform and allocation:" + sw.Elapsed);

            sw.Reset();
            sw.Start();
            HashSet<string> uniqueSet = new HashSet<string>();

            foreach (DataRow row in dt.Rows)
            {
                var ANOid= row["ANOCHI"].ToString();
                if (!uniqueSet.Contains(ANOid))
                    uniqueSet.Add(ANOid);

                Assert.IsTrue(_anochiPattern.IsMatch(ANOid));
            }

            Console.WriteLine("Allocated " + uniqueSet.Count + " anonymous identifiers");


            SqlConnection con = new SqlConnection(ANOStore_ConnectionStringBuilder.ConnectionString);
            con.Open();
            
            SqlCommand cmd = new SqlCommand("Select count(*) from ANOMyTable",con);
            int numberOfRows = Convert.ToInt32(cmd.ExecuteScalar());

            //should be the same number of unique identifiers in memory as in the database
            Assert.AreEqual(uniqueSet.Count,numberOfRows);
            Console.WriteLine("Found " + numberOfRows + " unique ones");

            SqlCommand cmdNulls = new SqlCommand("select count(*) from ANOMyTable where ANOMyTable is null",con);
            int nulls = Convert.ToInt32(cmdNulls.ExecuteScalar());
            Assert.AreEqual(0,nulls);
            Console.WriteLine("Found " + nulls + " null ANO identifiers");

            con.Close();
            sw.Stop();
            Console.WriteLine("Time to evaluate results:" + sw.Elapsed);
            TruncateANOTable(anoTable);

            anoTable.DeleteInDatabase();
        }

        /// <summary>
        /// Creates a new ANOTable called ANOMyTable in the Data Catalogue (and cleans up any old copy kicking around), you will need to set it's properties and
        /// call PushToANOServerAsNewTable if you want to use it with an ANOTransformer
        /// </summary>
        /// <returns></returns>
        protected ANOTable GetANOTable()
        {
            const string name = "ANOMyTable";

            var toCleanup = CatalogueRepository.GetAllObjects<ANOTable>().SingleOrDefault(a => a.TableName.Equals(name));

            if (toCleanup != null)
                toCleanup.DeleteInDatabase();

            return new ANOTable(CatalogueRepository, ANOStore_ExternalDatabaseServer, name, "A");
        }
    }
}
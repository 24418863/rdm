﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using CatalogueLibrary.Repositories;
using NUnit.Framework;
using ReusableLibraryCode;
using ReusableLibraryCode.DataAccess;
using Tests.Common;

namespace CatalogueLibraryTests.Integration.DataAccess
{
    public class TestDataAccess:DatabaseTests
    {
       
        #region Distinct Connection String (from Collection tests - Failing)

        [Test]
        [ExpectedException(ExpectedMessage = "collection could not agree on a single Password", MatchType = MessageMatch.Contains)]
        public void TestDistinctCredentials_PasswordMismatch()
        {
            List<TestAccessPoint>  testPoints = new List<TestAccessPoint>();

            testPoints.Add(new TestAccessPoint("frank","bob","username","mypas"));
            testPoints.Add(new TestAccessPoint("frank","bob","username","mydifferentPass"));

            //call this
            var result = DataAccessPortal.GetInstance().ExpectDistinctServer(testPoints.ToArray(), DataAccessContext.InternalDataProcessing, true);

        }

        [Test]
        [ExpectedException(ExpectedMessage = "collection could not agree whether to use Credentials", MatchType = MessageMatch.Contains)]
        public void TestDistinctCredentials_UsernamePasswordAreNull()
        {
            List<TestAccessPoint> testPoints = new List<TestAccessPoint>();

            testPoints.Add(new TestAccessPoint("frank", "bob", null, null));
            testPoints.Add(new TestAccessPoint("frank", "bob", "username", "mydifferentPass"));

            //call this
            var result = DataAccessPortal.GetInstance().ExpectDistinctServer(testPoints.ToArray(), DataAccessContext.InternalDataProcessing, true);

        }

        [Test]
        [ExpectedException(ExpectedMessage = "collection could not agree on a single Username", MatchType = MessageMatch.Contains)]
        public void TestDistinctCredentials_UsernameMismatch()
        {
            List<TestAccessPoint> testPoints = new List<TestAccessPoint>();

            testPoints.Add(new TestAccessPoint("frank", "bob", "usernameasdasd", "mydifferentpass"));
            testPoints.Add(new TestAccessPoint("frank", "bob", "username", "mydifferentPass"));

            //call this
            var result = DataAccessPortal.GetInstance().ExpectDistinctServer(testPoints.ToArray(), DataAccessContext.InternalDataProcessing, true);

        }

        #endregion

        #region Distinct Connection String (from Collection tests - Passing)

        [Test]
        public void TestDistinctCredentials_WrappedDatabaseName()
        {
            List<TestAccessPoint> testPoints = new List<TestAccessPoint>();

            testPoints.Add(new TestAccessPoint("frank", "[bob's Database]", "username", "mypas"));
            testPoints.Add(new TestAccessPoint("frank", "bob's Database", "username", "mypas"));
            //call this
            var result = DataAccessPortal.GetInstance().ExpectDistinctServer(testPoints.ToArray(), DataAccessContext.InternalDataProcessing, true);

            //test result
            Assert.AreEqual("bob's Database", result.Builder["Initial Catalog"]);
        }

        [Test]
        public void TestDistinctCredentials_PasswordMatch()
        {
            List<TestAccessPoint> testPoints = new List<TestAccessPoint>();

            testPoints.Add(new TestAccessPoint("frank", "bob", "username", "mypas"));
            testPoints.Add(new TestAccessPoint("frank", "bob", "username", "mypas"));

            //call this
            var result = DataAccessPortal.GetInstance().ExpectDistinctServer(testPoints.ToArray(), DataAccessContext.InternalDataProcessing, true);

            //test result
            Assert.AreEqual("mypas", result.Builder["Password"]);

        }
        #endregion

        [Test]
        public void AsyncTest()
        {
            List<Thread> threads = new List<Thread>();


            for (int i = 0; i < 30; i++)
                threads.Add(new Thread(MessWithCatalogue));

            foreach (Thread t in threads)
                t.Start();

            while(threads.Any(t=>t.ThreadState != ThreadState.Stopped))
                Thread.Sleep(100);

            for (int index = 0; index < asyncExceptions.Count; index++)
            {
                Console.WriteLine("Exception " + index);
                Exception asyncException = asyncExceptions[index];
                Console.WriteLine(ExceptionHelper.ExceptionToListOfInnerMessages(asyncException, true));
            }
            Assert.IsEmpty(asyncExceptions);
        }

        private List<Exception> asyncExceptions = new List<Exception>();

        private void MessWithCatalogue()
        {
            try
            {
                var repository = new CatalogueRepository(CatalogueRepository.ConnectionStringBuilder);
                var cata = new Catalogue(repository, "bob");
                cata.Name = "Fuss";
                cata.SaveToDatabase();
                cata.DeleteInDatabase();
            }
            catch (Exception ex)
            {
                asyncExceptions.Add(ex);
            }
        }


        /// <summary>
        /// Real life test case where TableInfo is the AccessPoint not just the test class
        /// </summary>
        [Test]
        public void TestGettingConnectionStrings()
        {
            foreach (TableInfo tbl in CatalogueRepository.GetAllObjects<TableInfo>().Where(table => table.Name.ToLower().Equals("bob")))
                tbl.DeleteInDatabase();

            foreach (var c in CatalogueRepository.GetAllObjects<DataAccessCredentials>().Where(cred=>cred.Name.ToLower().Equals("bob")))
                c.DeleteInDatabase();
            
            //test it with TableInfos
            TableInfo t = new TableInfo(CatalogueRepository, "Bob");
            try
            {
                t.Server = "fish";
                t.Database = "bobsDatabase";
                t.SaveToDatabase();

                //t has no credentials 
                var server = DataAccessPortal.GetInstance().ExpectServer(t, DataAccessContext.InternalDataProcessing);

                Assert.AreEqual(typeof(SqlConnectionStringBuilder), server.Builder.GetType());
                Assert.AreEqual("fish", ((SqlConnectionStringBuilder)server.Builder).DataSource);
                Assert.AreEqual("bobsDatabase", ((SqlConnectionStringBuilder)server.Builder).InitialCatalog);
                Assert.AreEqual(true, ((SqlConnectionStringBuilder)server.Builder).IntegratedSecurity);

                var creds = new DataAccessCredentials(CatalogueRepository, "Bob");
                try
                {
                    t.SetCredentials(creds, DataAccessContext.InternalDataProcessing, true);
                    creds.Username = "frank";
                    creds.Password = "bobsPassword";
                    creds.SaveToDatabase();


                    ////t has some credentials now
                    server = DataAccessPortal.GetInstance().ExpectServer(t, DataAccessContext.InternalDataProcessing);

                    Assert.AreEqual(typeof(SqlConnectionStringBuilder), server.Builder.GetType());
                    Assert.AreEqual("fish", ((SqlConnectionStringBuilder)server.Builder).DataSource);
                    Assert.AreEqual("bobsDatabase", ((SqlConnectionStringBuilder)server.Builder).InitialCatalog);
                    Assert.AreEqual("frank", ((SqlConnectionStringBuilder)server.Builder).UserID);
                    Assert.AreEqual("bobsPassword", ((SqlConnectionStringBuilder)server.Builder).Password);
                    Assert.AreEqual(false, ((SqlConnectionStringBuilder)server.Builder).IntegratedSecurity);
                }
                finally
                {
                    var linker = new TableInfoToCredentialsLinker(CatalogueRepository);
                    linker.BreakAllLinksBetween(creds, t);
                    creds.DeleteInDatabase();
                }

            }
            finally
            {
                t.DeleteInDatabase();

            }
        }

        
        internal class TestAccessPoint:IDataAccessPoint,IDataAccessCredentials
        {
            public string Server { get; set; }
            public string Database { get; set; }
            public DatabaseType DatabaseType { get; set; }

            public string Username { get; set; }
            public string Password { get; set; }

            public TestAccessPoint(string server, string database, string username, string password)
            {
                Server = server;
                Database = database;
                Username = username;
                Password = password;
            }

            public IDataAccessCredentials GetCredentialsIfExists(DataAccessContext context)
            {
                if (Username != null)
                    return this;

                return null;
            }

            
            public string GetDecryptedPassword()
            {
                return Password;
            }

            public override string ToString()
            {
                return Server + Database;
            }
        }


    }
}
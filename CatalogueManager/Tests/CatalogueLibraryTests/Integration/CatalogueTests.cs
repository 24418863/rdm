﻿using System;
using System.Collections.Generic;
using System.Linq;
using CatalogueLibrary.Data;
using NUnit.Framework;
using Rhino.Mocks.Constraints;
using Tests.Common;

namespace CatalogueLibraryTests.Integration
{
    public class CatalogueTests : DatabaseTests
    {
        [Test]
        public void getlist_listCatalogues_greaterThanOne()
        {
            Catalogue catalogueWithId = new Catalogue(CatalogueRepository, "bob");
            Catalogue[] catas = CatalogueRepository.GetAllCatalogues();

            Assert.IsTrue(catas.Length > 0);

            catalogueWithId.DeleteInDatabase();
        }

        [Test]
        public void SettingPropertyViaRelationshipDoesntSave_NoticeHowYouHaveToCacheThePropertyCatalogueToSetIt()
        {
            Catalogue c = new Catalogue(CatalogueRepository,"frank");
            CatalogueItem ci = new CatalogueItem(CatalogueRepository,c,"bob");


            var cata = ci.Catalogue;
            cata.Name = "fish2";
            cata.SaveToDatabase();
            Assert.AreEqual("fish2", ci.Catalogue.Name);

            ci.Catalogue.Name = "fish";
            ci.Catalogue.SaveToDatabase();
            Assert.AreNotEqual("fish",ci.Catalogue.Name);

            c.DeleteInDatabase();
        }

        [Test]
        public void MaxLengthsSetTest()
        {
            Catalogue c = new Catalogue(CatalogueRepository, "bob");

            try
            {
                Assert.AreEqual(255, Catalogue.Administrative_contact_email_MaxLength);
                Assert.AreEqual(int.MaxValue,Catalogue.Description_MaxLength);

                CatalogueItem ci = new CatalogueItem(CatalogueRepository, c, "Fisny");

                try
                {
                    Assert.AreEqual(255,CatalogueItem.Agg_method_MaxLength);    
                }
                finally 
                {
                    ci.DeleteInDatabase();
                }
                

            }
            finally 
            {
                c.DeleteInDatabase();
            }
        }

        
        [Test]
        public void update_changeNameOfCatalogue_passes()
        {
            //create a new one
            var cata = new Catalogue(CatalogueRepository, "fishing");
            int expectedID = cata.ID;

            //find it and change it's name
            Catalogue[] catas = CatalogueRepository.GetAllCatalogues().ToArray();

            foreach (var catalogue in catas)
            {
                if (catalogue.ID == expectedID)
                {
                    catalogue.Name = "fish";
                    catalogue.SaveToDatabase();
                }
            }

            //find it again and see if it's name has changed - then delete it so we don't polute the db
            Catalogue[] catasAfter = CatalogueRepository.GetAllCatalogues().ToArray();

            foreach (var catalogue in catasAfter)
            {
                if (catalogue.ID == expectedID)
                {
                    Assert.AreEqual(catalogue.Name, "fish");
                    catalogue.DeleteInDatabase();
                }
            }
        }

        [Test]
        public void update_changeAllProperties_pass()
        {
            //create a new one
            var cata = new Catalogue(CatalogueRepository, "fishing");
            int expectedID = cata.ID;

            //find it and change it's name
            Catalogue[] catas = CatalogueRepository.GetAllCatalogues().ToArray();

            foreach (var catalogue in catas)
            {
                if (catalogue.ID == expectedID)
                {
                    catalogue.Access_options = "backwards,frontwards";
                    catalogue.API_access_URL = new Uri("http://API.html");
                    catalogue.Acronym = "abc";
                    catalogue.Attribution_citation = "belongs to dave";
                    catalogue.Browse_URL = new Uri("http://browse.html");
                    catalogue.Bulk_Download_URL = new Uri("http://bulk.html");
                    catalogue.Contact_details = "thomasnind";
                    catalogue.Geographical_coverage = "fullspectrum";
                    catalogue.Resource_owner = "blackhole";
                    catalogue.Description = "exciting stuff of great excitement";
                    catalogue.Detail_Page_URL = new Uri("http://detail.html");
                    catalogue.Last_revision_date = DateTime.Parse("01/01/01");
                    catalogue.Name = "kaptainshield";
                    catalogue.Background_summary = "£50 preferred";
                    catalogue.Periodicity = Catalogue.CataloguePeriodicity.Monthly;
                    catalogue.Query_tool_URL = new Uri("http://querier.html");
                    catalogue.Source_URL =  new Uri("http://blackholeSun.html");
                    catalogue.Time_coverage = "comprehensive";
                    catalogue.Search_keywords = "excitement,fishmongery";
                    catalogue.Type = Catalogue.CatalogueType.ResearchStudy;
                    catalogue.Update_freq = "Every darmn second!";
                    catalogue.Update_sched = "periodically on request";

                    catalogue.Country_of_origin = "United Kingdom";
                    catalogue.Data_standards = "Highly Standardised";
                    catalogue.Administrative_contact_address = "Candyland";
                    catalogue.Administrative_contact_email = "big@brother.com";
                    catalogue.Administrative_contact_name = "Uncle Sam";
                    catalogue.Administrative_contact_telephone = "12345 67890";
                    catalogue.Explicit_consent = true;
                    catalogue.Ethics_approver = "Tayside Supernatural Department";
                    catalogue.Source_of_data_collection = "Invented by Unit Test";
                    catalogue.SubjectNumbers = "100,000,000";

                    catalogue.SaveToDatabase();
                }
            }



            //find it again and see if it has changed - then delete it so we don't polute the db
            Catalogue[] catasAfter = CatalogueRepository.GetAllCatalogues().ToArray();

            foreach (var catalogue in catasAfter)
            {
                if (catalogue.ID == expectedID)
                {
                    Assert.AreEqual(catalogue.Access_options , "backwards,frontwards");
                    Assert.AreEqual(catalogue.API_access_URL , new Uri("http://API.html"));
                    Assert.AreEqual(catalogue.Acronym , "abc");
                    Assert.AreEqual(catalogue.Attribution_citation , "belongs to dave");
                    Assert.AreEqual(catalogue.Browse_URL , new Uri("http://browse.html"));
                    Assert.AreEqual(catalogue.Bulk_Download_URL , new Uri("http://bulk.html"));
                    Assert.AreEqual(catalogue.Contact_details , "thomasnind");
                    Assert.AreEqual(catalogue.Geographical_coverage, "fullspectrum");
                    Assert.AreEqual(catalogue.Resource_owner, "blackhole");
                    Assert.AreEqual(catalogue.Description , "exciting stuff of great excitement");
                    Assert.AreEqual(catalogue.Detail_Page_URL , new Uri("http://detail.html"));
                    Assert.AreEqual(catalogue.Last_revision_date , DateTime.Parse("01/01/01"));
                    Assert.AreEqual(catalogue.Name , "kaptainshield");
                    Assert.AreEqual(catalogue.Background_summary, "£50 preferred");
                    Assert.AreEqual(catalogue.Periodicity , Catalogue.CataloguePeriodicity.Monthly);
                    Assert.AreEqual(catalogue.Query_tool_URL , new Uri("http://querier.html"));
                    Assert.AreEqual(catalogue.Source_URL , new Uri("http://blackholeSun.html"));
                    Assert.AreEqual(catalogue.Time_coverage , "comprehensive");
                    Assert.AreEqual(catalogue.Search_keywords, "excitement,fishmongery");
                    Assert.AreEqual(catalogue.Type , Catalogue.CatalogueType.ResearchStudy);
                    Assert.AreEqual(catalogue.Update_freq , "Every darmn second!");
                    Assert.AreEqual(catalogue.Update_sched , "periodically on request");


                    Assert.AreEqual(catalogue.Country_of_origin , "United Kingdom");
                    Assert.AreEqual(catalogue.Data_standards , "Highly Standardised");
                    Assert.AreEqual(catalogue.Administrative_contact_address , "Candyland");
                    Assert.AreEqual(catalogue.Administrative_contact_email , "big@brother.com");
                    Assert.AreEqual(catalogue.Administrative_contact_name , "Uncle Sam");
                    Assert.AreEqual(catalogue.Administrative_contact_telephone , "12345 67890");
                    Assert.AreEqual(catalogue.Explicit_consent , true);
                    Assert.AreEqual(catalogue.Ethics_approver , "Tayside Supernatural Department");
                    Assert.AreEqual(catalogue.Source_of_data_collection , "Invented by Unit Test");
                    Assert.AreEqual(catalogue.SubjectNumbers, "100,000,000");


                    catalogue.DeleteInDatabase();
                }
            }
        }

        [Test]
        public void create_blankConstructorCatalogue_createsNewInDatabase()
        {
            int before = CatalogueRepository.GetAllCatalogues().Count();

            var newCatalogue = new Catalogue(CatalogueRepository, "fishing");
            int expectedID = newCatalogue.ID;

            Assert.IsTrue(expectedID > 1);


            Catalogue[] catasAfter = CatalogueRepository.GetAllCatalogues().ToArray();
            int after = catasAfter.Count();

            Assert.AreEqual(before, after - 1);

            int numberDeleted = 0;
            foreach (Catalogue cata in catasAfter)
            {
                if (cata.ID == expectedID)
                {
                    cata.DeleteInDatabase();
                    numberDeleted++;
                }
            }

            Assert.AreEqual(numberDeleted, 1);
        }

        [Test]
        public void GetCatalogueWithID_InvalidID_throwsException()
        {
            Assert.Throws<KeyNotFoundException>(() => CatalogueRepository.GetObjectByID<Catalogue>(-1));
        }

        [Test]
        public void GetCatalogueWithID_validID_pass()
        {
            Catalogue c = new Catalogue(CatalogueRepository, "TEST");

            Assert.NotNull(c);
            Assert.True(c.Name == "TEST");
            
            c.DeleteInDatabase();
        }

        [Test]
        public void GetServerFromExtractionInformation_ValidThenTwoDifferentServersThenNoServer()
        {
            CatalogueItem cataItem1;
            CatalogueItem cataItem2;
            ExtractionInformation cataItem1ExtractionInformation;
            ExtractionInformation cataItem2ExtractionInformation;

            TableInfo table1;
            TableInfo table2;
            ColumnInfo column1;
            ColumnInfo column2;

            var cata = new Catalogue(CatalogueRepository, "Dave");

            try
            {
                
                cataItem1 = new CatalogueItem(CatalogueRepository, cata, "Item1");
                cataItem2 = new CatalogueItem(CatalogueRepository, cata, "Item2");
            }
            catch (Exception)
            {
                cata.DeleteInDatabase();
                throw;
            }

            try
            {
                table1 = new TableInfo(CatalogueRepository, "TestTable1");
                table2 = new TableInfo(CatalogueRepository, "TestTable2");
                
                //this is the most important line, where we setup that there are 2 tables both on the server consus involved in this extraction
                table1.Server = "CONSUS";
                table2.Server = "CONSUS";

                table1.SaveToDatabase();
                table2.SaveToDatabase();
            }
            catch (Exception)
            {
                cataItem1.DeleteInDatabase();
                cataItem2.DeleteInDatabase();
                cata.DeleteInDatabase();
                
                throw;
            }


            try
            {
                column1 = new ColumnInfo(CatalogueRepository, "Item1","VARCHAR(100)",table1);
                column2 = new ColumnInfo(CatalogueRepository, "Item2", "VARCHAR(100)", table2);
            }
            catch (Exception)
            {
                cataItem1.DeleteInDatabase();
                cataItem2.DeleteInDatabase();
                cata.DeleteInDatabase();
                
                table1.DeleteInDatabase();
                table2.DeleteInDatabase();
                throw;
            }

            try
            {
                cataItem1.SetColumnInfo(column1);
                cataItem2.SetColumnInfo(column2);

                cataItem1ExtractionInformation = new ExtractionInformation(CatalogueRepository, cataItem1, column1, "CONSUS..Column1 AS Col1");
                cataItem2ExtractionInformation = new ExtractionInformation(CatalogueRepository, cataItem2, column2, "CONSUS..Column2 AS Col2");
            }
            catch (Exception)
            {
                cataItem1.DeleteInDatabase();
                cataItem2.DeleteInDatabase();
                cata.DeleteInDatabase();
                
                column1.DeleteInDatabase();
                column2.DeleteInDatabase();
                table1.DeleteInDatabase();
                table2.DeleteInDatabase();
                throw;
            }


            try
            {
             
                //the test! finally!!!
                Assert.AreEqual(cata.GetServerFromExtractionInformation(ExtractionCategory.Any),"CONSUS");

                //now try creating a mismatch, one Table is listed as CONUS and the other is listed as JANUS
                table1.Server = "JANUS";
                table1.SaveToDatabase();
                try
                {
                    string server = cata.GetServerFromExtractionInformation(ExtractionCategory.Any);
                    throw new Exception("Should have bombed out here because we have two different servers included in the same extraction");
                }
                catch (Exception ex)
                {
                    Assert.IsTrue(ex.Message.StartsWith("Found multiple servers"));
                }

                //now try creating a catastrophic failure mode (similar to a resonance cascade) where one of the servers is null!!!
                table1.Server = null;
                table1.SaveToDatabase();
                try
                {
                    string server = cata.GetServerFromExtractionInformation(ExtractionCategory.Any);
                    
                    //thats not fine
                    throw new Exception("Should have bombed out here because we have a null server");
                }
                catch (NullReferenceException)
                {
                    //thats fine, it's what we expected
                }

            }
            finally
            {
                //clean up
                cataItem1ExtractionInformation.DeleteInDatabase();
                cataItem2ExtractionInformation.DeleteInDatabase();

                //always delete items before catalogues since the catalogue will Cascade otherwise and your reference will not be found in the db
                cataItem1.DeleteInDatabase();
                cataItem2.DeleteInDatabase();
                cata.DeleteInDatabase();
                
                column1.DeleteInDatabase();
                column2.DeleteInDatabase();
                table1.DeleteInDatabase();
                table2.DeleteInDatabase();
            }
        }


        [Test]
        public void TestGetTablesAndLookupTables()
        {
            //One catalogue
            Catalogue cata = new Catalogue(CatalogueRepository, "TestGetTablesAndLookupTables");

            //6 virtual columns
            CatalogueItem ci1 = new CatalogueItem(CatalogueRepository, cata, "Col1");
            CatalogueItem ci2 = new CatalogueItem(CatalogueRepository, cata, "Col2");
            CatalogueItem ci3 = new CatalogueItem(CatalogueRepository, cata, "Col3");
            CatalogueItem ci4 = new CatalogueItem(CatalogueRepository, cata, "Col4");
            CatalogueItem ci5 = new CatalogueItem(CatalogueRepository, cata, "Description");
            CatalogueItem ci6 = new CatalogueItem(CatalogueRepository, cata, "Code");

            //2 columns come from table 1
            TableInfo t1 = new TableInfo(CatalogueRepository, "Table1");
            ColumnInfo t1_c1 = new ColumnInfo(CatalogueRepository, "Col1","varchar(10)",t1);
            ColumnInfo t1_c2 = new ColumnInfo(CatalogueRepository, "Col2", "int", t1);

            //2 columns come from table 2
            TableInfo t2 = new TableInfo(CatalogueRepository, "Table2");
            ColumnInfo t2_c1 = new ColumnInfo(CatalogueRepository, "Col3", "varchar(10)", t2);
            ColumnInfo t2_c2 = new ColumnInfo(CatalogueRepository, "Col4", "int", t2);

            //2 columns come from the lookup table
            TableInfo t3 = new TableInfo(CatalogueRepository, "Table3");
            ColumnInfo t3_c1 = new ColumnInfo(CatalogueRepository, "Description", "varchar(10)", t3);
            ColumnInfo t3_c2 = new ColumnInfo(CatalogueRepository, "Code", "int", t3);

            //wire up virtual columns to underlying columns
            ci1.SetColumnInfo(t1_c1);
            ci2.SetColumnInfo( t1_c2);
            ci3.SetColumnInfo( t2_c1);
            ci4.SetColumnInfo( t2_c2);
            ci5.SetColumnInfo( t3_c1);
            ci6.SetColumnInfo( t3_c2);

            //configure the lookup relationship
            var lookup = new Lookup(CatalogueRepository, t3_c1, t1_c2, t3_c2,ExtractionJoinType.Left, "");
            try
            {
                var allTables = cata.GetTableInfoList(true).ToArray();
                Assert.Contains(t1,allTables);
                Assert.Contains(t2, allTables);
                Assert.Contains(t3, allTables);
            
                var normalTablesOnly = cata.GetTableInfoList(false).ToArray();
                Assert.AreEqual(2,normalTablesOnly.Length);
                Assert.Contains(t1,normalTablesOnly);
                Assert.Contains(t2, normalTablesOnly);

                var lookupTablesOnly = cata.GetLookupTableInfoList();
                Assert.AreEqual(1,lookupTablesOnly.Length);
                Assert.Contains(t3,lookupTablesOnly);

                List<TableInfo> normalTables, lookupTables;
                cata.GetTableInfos(out normalTables, out lookupTables);
                Assert.AreEqual(2,normalTables.Count);
                Assert.AreEqual(1, lookupTables.Count);

                Assert.Contains(t1,normalTables);
                Assert.Contains(t2, normalTables);
                Assert.Contains(t3,lookupTables);
            }
            finally
            {
                lookup.DeleteInDatabase();
                
                t1.DeleteInDatabase();
                t2.DeleteInDatabase();
                t3.DeleteInDatabase();

                cata.DeleteInDatabase();
            }
        }

        [Test]
        public void CatalogueFolder_DefaultIsRoot()
        {
            var c = new Catalogue(CatalogueRepository, "bob");
            try
            {
                Assert.AreEqual("\\",c.Folder.Path);
            }
            finally
            {
                c.DeleteInDatabase();
            }
        }
        [Test]
        public void CatalogueFolder_ChangeAndSave()
        {
            var c = new Catalogue(CatalogueRepository, "bob"); 
            try
            {
                c.Folder.Path = "\\Research\\Important";
                Assert.AreEqual("\\research\\important\\", c.Folder.Path);
                c.SaveToDatabase();

                var c2 = CatalogueRepository.GetObjectByID<Catalogue>(c.ID);
                Assert.AreEqual("\\research\\important\\", c2.Folder.Path);
            }
            finally
            {
                c.DeleteInDatabase();
            }
        }


        [Test]
        [ExpectedException(ExpectedMessage = @"All catalogue paths must start with \ but Catalogue bob had an attempt to set it's folder to :fish")]
        public void CatalogueFolder_CannotSetToNonRoot()
        {
            var c = new Catalogue(CatalogueRepository, "bob");
            try
            {
                c.Folder.Path = "fish";
            }
            finally
            {
                c.DeleteInDatabase();
            }
        }

        [Test]
        [ExpectedException(ExpectedMessage = @"An attempt was made to set Catalogue bob Folder to null, every Catalogue must have a folder, set it to \ if you want the root")]
        public void CatalogueFolder_CannotSetToNull()
        {
            var c = new Catalogue(CatalogueRepository, "bob");
            try
            {
                c.Folder.Path = null;
            }
            finally
            {
                c.DeleteInDatabase();
            }
        }
        
        [Test]
        [ExpectedException(ExpectedMessage = @"Catalogue paths cannot contain double slashes '\\', Catalogue bob had an attempt to set it's folder to :\\bob\\")]
        public void CatalogueFolder_CannotHaveDoubleSlashes()
        {
            var c = new Catalogue(CatalogueRepository, "bob");
            try
            {
                //notice the @ symbol that makes the double slashes actual double slashes - common error we might make and what this test is designed to prevent
                c.Folder.Path = @"\\bob\\";
                c.SaveToDatabase();
            }
            finally
            {
                c.DeleteInDatabase();
            }
        }

        [Test]
        public void CatalogueFolder_Subfoldering()
        {
            var c1 = new Catalogue(CatalogueRepository, "C1");
            var c2 = new Catalogue(CatalogueRepository, "C2");

            try
            {
                c1.Folder.Path = "\\Research\\";
                Assert.AreEqual("\\research\\", c1.Folder.Path);
                c1.SaveToDatabase();

                c2.Folder.Path = "\\Research\\Methodology";
                Assert.AreEqual( "\\research\\methodology\\",c2.Folder.Path);

                c2.SaveToDatabase();

                Assert.IsTrue(c2.Folder.IsSubFolderOf(c1.Folder));

            }
            finally
            {
                c1.DeleteInDatabase();
                c2.DeleteInDatabase();
            }
        }
        [Test]
        public void CatalogueFolder_SubfolderingAdvanced()
        {
            var c1 = new Catalogue(CatalogueRepository, "C1");
            var c2 = new Catalogue(CatalogueRepository, "C2");
            var c3 = new Catalogue(CatalogueRepository, "C3");
            var c4 = new Catalogue(CatalogueRepository, "C4");
            var c5 = new Catalogue(CatalogueRepository, "C5");
            var c6 = new Catalogue(CatalogueRepository, "C6");


            // 
            // Pass in 
            // CatalogueA - \2005\Research\
            // CatalogueB - \2006\Research\
            // 
            // This is Root (\)
            // Returns:
            //     \2005\ - empty 
            //     \2006\ - empty
            // 

            try
            {
                c1.Folder.Path = @"\2005\Research\Current";
                c1.SaveToDatabase();

                c2.Folder.Path = @"\2005\Research\Previous";
                c2.SaveToDatabase();


                c3.Folder.Path = @"\2001\Research\Current";
                c3.SaveToDatabase();

                c4.Folder.Path = @"\Homeland\Research\Current";
                c4.SaveToDatabase();
                
                c5.Folder.Path = @"\Homeland\Research\Current";
                c5.SaveToDatabase();
                
                c6.Folder.Path = @"\Homeland\Research\Current";
                c6.SaveToDatabase();

                var collection = new[] {c1, c2, c3, c4,c5,c6};

                var results = CatalogueFolder.Root.GetImmediateSubFoldersUsing(collection);

                Assert.AreEqual(3,results.Length);
                CatalogueFolder TwoThousandFive = results.Single(f => f.Path.Equals(@"\2005\"));
                CatalogueFolder TwoThousandOne = results.Single(f => f.Path.Equals(@"\2001\"));
                CatalogueFolder Homeland = results.Single(f => f.Path.Equals(@"\homeland\"));
                
                Assert.AreEqual(1,Homeland.GetImmediateSubFoldersUsing(collection).Length);
                Assert.AreEqual(1, Homeland.GetImmediateSubFoldersUsing(collection).Count(f=>f.Path.Equals(@"\homeland\research\")));

                Assert.AreEqual(1, TwoThousandOne.GetImmediateSubFoldersUsing(collection).Length);
                Assert.AreEqual(1, TwoThousandOne.GetImmediateSubFoldersUsing(collection).Count(f => f.Path.Equals(@"\2001\research\")));

                CatalogueFolder[] finalResult = TwoThousandFive.GetImmediateSubFoldersUsing(collection).Single().GetImmediateSubFoldersUsing(collection);
                Assert.AreEqual(2, finalResult.Length);
                Assert.AreEqual(1, finalResult.Count(c => c.Path.Equals(@"\2005\research\current\")));
                Assert.AreEqual(1, finalResult.Count(c => c.Path.Equals(@"\2005\research\previous\")));

                Assert.AreEqual(0,finalResult[0].GetImmediateSubFoldersUsing(collection).Length);
            }
            finally 
            {
                c1.DeleteInDatabase();
                c2.DeleteInDatabase();
                c3.DeleteInDatabase();
                c4.DeleteInDatabase();
                c5.DeleteInDatabase();
                c6.DeleteInDatabase();
                
            }
        }

        [Test]
        public void RelatedCatalogueTest_NoCatalogues()
        {
            TableInfo t = new TableInfo(CatalogueRepository,"MyTable");
            try
            {
                Assert.AreEqual(0,t.GetAllRelatedCatalogues().Length);
            }
            finally
            {
                t.DeleteInDatabase();
            }
            
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void RelatedCatalogueTest_OneCatalogue(bool createExtractionInformation)
        {
            TableInfo t = new TableInfo(CatalogueRepository, "MyTable");
            ColumnInfo c = new ColumnInfo(CatalogueRepository,"MyCol","varchar(10)",t);
            
            Catalogue cata = new Catalogue(CatalogueRepository,"MyCata");
            CatalogueItem ci = new CatalogueItem(CatalogueRepository,cata,"MyCataItem");

            try
            {
                if (createExtractionInformation)
                    new ExtractionInformation(CatalogueRepository, ci, c, "dbo.SomeFunc('Bob') as MySelectLine");
                else
                    ci.SetColumnInfo(c);

                var catas = t.GetAllRelatedCatalogues();
                Assert.AreEqual(1, catas.Length);
                Assert.AreEqual(cata,catas[0]);
            }
            finally
            {
                ci.DeleteInDatabase();
                cata.DeleteInDatabase();
                t.DeleteInDatabase();
            }
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void RelatedCatalogueTest_TwoCatalogues_TwoColumnsEach(bool createExtractionInformation)
        {
            TableInfo t = new TableInfo(CatalogueRepository, "MyTable");
            ColumnInfo c1 = new ColumnInfo(CatalogueRepository, "MyCol1", "varchar(10)", t);
            ColumnInfo c2 = new ColumnInfo(CatalogueRepository, "MyCol2", "varchar(10)", t);
            
            Catalogue cata1 = new Catalogue(CatalogueRepository, "cata1");
            CatalogueItem ci1_1 = new CatalogueItem(CatalogueRepository, cata1, "MyCataItem1_1");
            CatalogueItem ci1_2 = new CatalogueItem(CatalogueRepository, cata1, "MyCataItem1_2");

            Catalogue cata2 = new Catalogue(CatalogueRepository, "cata2");
            CatalogueItem ci2_1 = new CatalogueItem(CatalogueRepository, cata2, "MyCataItem2_1");
            CatalogueItem ci2_2 = new CatalogueItem(CatalogueRepository, cata2, "MyCataItem2_2");
            try
            {
                if (createExtractionInformation)
                {
                    new ExtractionInformation(CatalogueRepository, ci1_1, c1, "dbo.SomeFunc('Bob') as MySelectLine");
                    new ExtractionInformation(CatalogueRepository, ci1_2, c2, "dbo.SomeFunc('Bob') as MySelectLine");
                    new ExtractionInformation(CatalogueRepository, ci2_1, c2, "dbo.SomeFunc('Bob') as MySelectLine");
                    new ExtractionInformation(CatalogueRepository, ci2_2, c1, "dbo.SomeFunc('Bob') as MySelectLine");
                }
                else
                {
                    ci1_1.SetColumnInfo(c1);
                    ci1_2.SetColumnInfo(c2);
                    ci2_1.SetColumnInfo(c2);
                    ci2_2.SetColumnInfo(c1);
                    
                }
                
                

                var catas = t.GetAllRelatedCatalogues();
                Assert.AreEqual(2, catas.Length);
                Assert.IsTrue(catas.Contains(cata1));
                Assert.IsTrue(catas.Contains(cata2));
            }
            finally
            {
                cata1.DeleteInDatabase();
                cata2.DeleteInDatabase();
                t.DeleteInDatabase();
            }

        }
    }
}
    
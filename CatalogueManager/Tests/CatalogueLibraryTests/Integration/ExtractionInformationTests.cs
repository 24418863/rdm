﻿using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.Repositories;
using NUnit.Framework;
using Tests.Common;

namespace CatalogueLibraryTests.Integration
{
    public class ExtractionInformationTests : DatabaseTests
    {
        ///////////////Create the things that we are going to create relationships between /////////////////

        Catalogue cata;
        CatalogueItem cataItem;
        TableInfo ti;
        ColumnInfo columnInfo;

        [SetUp]
        public void SetupExtraction()
        {

            cata = new Catalogue(CatalogueRepository, "ExtractionInformationTestsCatalogue");
            cataItem = new CatalogueItem(CatalogueRepository, cata, "QuadlzorVelocity");
            ti = new TableInfo(CatalogueRepository, "HighEnergyShizzle");
            columnInfo = new ColumnInfo(CatalogueRepository, "VelocityOfMatter", "int", ti);

            ////////////Check the creation worked ok
            Assert.IsNotNull(cata); //catalogue
            Assert.IsNotNull(cataItem);

            Assert.IsNotNull(ti); //underlying table stuff
            Assert.IsNotNull(columnInfo);

            ////////////// Create links between stuff and check they were created successfully //////////////

            //create a link between catalogue item lazor and velocity column
            cataItem.SetColumnInfo(columnInfo);
                
        }
        [TearDown]
        public void DeleteSetupObjects()
        {
            if(cataItem != null)
                cataItem.DeleteInDatabase();

            cata.DeleteInDatabase();

            if(columnInfo != null)
                columnInfo.DeleteInDatabase();

            if(ti != null)
                ti.DeleteInDatabase();
        }

        [Test]
        public void BasicIDsAreCorrect()
        {
            ColumnInfo firstLinked = cataItem.ColumnInfo;
            Assert.IsTrue(firstLinked != null);
            Assert.IsTrue(firstLinked.ID == columnInfo.ID);
        }


        [Test]
        public void test_creating_ExtractionFilter()
        {

            ExtractionInformation extractInfo = null;
            ExtractionFilter filterFastThings = null;
            ExtractionFilterParameter parameter = null;

            try
            {
                //define extraction information
                extractInfo = new ExtractionInformation(CatalogueRepository, cataItem, columnInfo, "ROUND(VelocityOfMatter,2) VelocityOfMatterRounded");

                //define filter and parameter
                filterFastThings = new ExtractionFilter(CatalogueRepository, "FastThings", extractInfo)
                {
                    WhereSQL = "VelocityOfMatter > @X",
                    Description = "Query to identify things that travel faster than X miles per hour!"
                };
                filterFastThings.SaveToDatabase();
                Assert.AreEqual(filterFastThings.Name, "FastThings");

                parameter = new ExtractionFilterParameter(CatalogueRepository, "DECLARE @X INT", filterFastThings);

                Assert.IsNotNull(parameter);
                Assert.AreEqual(parameter.ParameterName ,"@X");

                parameter.Value = "500";
                parameter.SaveToDatabase();

                ExtractionFilterParameter afterSave = CatalogueRepository.GetObjectByID<ExtractionFilterParameter>(parameter.ID);
                Assert.AreEqual(afterSave.Value ,"500");


                ExtractionFilter filterFastThings_NewCopyFromDB = CatalogueRepository.GetObjectByID<ExtractionFilter>(filterFastThings.ID);

                Assert.AreEqual(filterFastThings.ID, filterFastThings_NewCopyFromDB.ID);
                Assert.AreEqual(filterFastThings.Description, filterFastThings_NewCopyFromDB.Description);
                Assert.AreEqual(filterFastThings.Name, filterFastThings_NewCopyFromDB.Name);
                Assert.AreEqual(filterFastThings.WhereSQL, filterFastThings_NewCopyFromDB.WhereSQL);
            }
            finally
            {

                if (parameter != null)
                    parameter.DeleteInDatabase();

                //filters are children of extraction info with CASCADE DELETE so have to delete this one first if we want to test it programatically (although we could just skip deleting it since SQL will handle it anyway)
                if (filterFastThings != null)
                    filterFastThings.DeleteInDatabase();

                if(extractInfo != null)
                    extractInfo.DeleteInDatabase();
            }
            


        }

        [Test]
        public void test_creating_ExtractionInformation()
        {
            
            
            ExtractionInformation extractInfo =null;

            try
            {
           
                //define extraction information
                //change some values and then save it
                extractInfo = new ExtractionInformation(CatalogueRepository, cataItem, columnInfo, "dave")
                {
                    Order = 123,
                    ExtractionCategory = ExtractionCategory.Supplemental
                };
                extractInfo.SaveToDatabase();
                
                //confirm the insert worked
                Assert.AreEqual(extractInfo.SelectSQL,"dave");

                //fetch the extraction information via the linked CatalogueItem - ColumnInfo pair (i.e. we are testing the alternate route to fetch ExtractionInformation - by ID or by colum/item pair)
                ExtractionInformation extractInfo2_CameFromLinker = cataItem.ExtractionInformation;
                Assert.AreEqual(extractInfo.ID, extractInfo2_CameFromLinker.ID);
                Assert.AreEqual(extractInfo.SelectSQL, extractInfo2_CameFromLinker.SelectSQL);

                //make sure it saves properly
                Assert.AreEqual(extractInfo2_CameFromLinker.Order,123 );
                Assert.AreEqual( extractInfo2_CameFromLinker.ExtractionCategory,ExtractionCategory.Supplemental);

            }
            finally 
            {
                
                if (extractInfo != null)
                    extractInfo.DeleteInDatabase();
                
            }
        }
    }
}
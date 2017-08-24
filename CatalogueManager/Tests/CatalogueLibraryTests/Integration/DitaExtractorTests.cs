﻿using System;
using System.IO;
using System.Linq;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using NUnit.Framework;
using Tests.Common;

namespace CatalogueLibraryTests.Integration
{
    class DitaExtractorTests : DatabaseTests
    {
        private Exception _setupException = null;

        private TestDirectoryHelper _directoryHelper;

        protected override void SetUp()
        {
            try
            {
                _directoryHelper = new TestDirectoryHelper(GetType());

                base.SetUp();

                _directoryHelper.SetUp();

                Random random = new Random();
                
                //delete all catalogues with duplicate names
                Catalogue[] catalogues = CatalogueRepository.GetAllCatalogues().ToArray();

                foreach (var cata in catalogues)
                    if (catalogues.Count(c => c.Name.Equals(cata.Name)) > 1)
                    {
                        Console.WriteLine("Deleteing Catalogue Called " + cata.Name + " (because there are multiple Catalogues with this name) in database at end of ConnectionString:" + CatalogueRepository.ConnectionString);
                        cata.DeleteInDatabase();
                    }

                //make sure all Catalogues have acroynms, if they dont then assign them a super random one
                foreach (Catalogue cata in CatalogueRepository.GetAllCatalogues(true))
                    if (string.IsNullOrWhiteSpace(cata.Acronym))
                    {
                        cata.Acronym = "RANDOMACRONYM_" + random.Next(10000);
                        cata.SaveToDatabase();
                    }
            }
            catch (Exception e)
            {
                _setupException = e;
            }
        }

        [TestFixtureTearDown]
        protected void TearDown()
        {
            _directoryHelper.TearDown();
        }

        [SetUp]
        protected void BeforeEachTest()
        {
            if (_setupException != null)
            {
                Console.WriteLine("TestFixtureSetUp failed in {0} - {1}", GetType(), _setupException.Message);
                throw _setupException;
            }

            _directoryHelper.DeleteAllEntriesInDir();
        }

        [Test]
        public void DitaExtractorConstructor_ExtractTestCatalogue_FilesExist()
        {
            var testDir = _directoryHelper.Directory;

            //get rid of any old copies lying around
            Catalogue oldCatalogueVersion = CatalogueRepository.GetAllCatalogues().SingleOrDefault(c => c.Name.Equals("DitaExtractorConstructor_ExtractTestCatalogue_FilesExist"));
            if(oldCatalogueVersion != null)
                oldCatalogueVersion.DeleteInDatabase();

            Catalogue ditaTestCatalogue = new Catalogue(CatalogueRepository, "DitaExtractorConstructor_ExtractTestCatalogue_FilesExist");//name of Catalogue

            ditaTestCatalogue.Acronym = "DITA_TEST";
            ditaTestCatalogue.Description =
                "Test catalogue for the unit test DitaExtractorConstructor_ExtractTestCatalogue_FilesExist in file " +
                typeof (DitaExtractorTests).FullName + ".cs";
            ditaTestCatalogue.SaveToDatabase();


            try
            {
                DitaCatalogueExtractor extractor = new DitaCatalogueExtractor(CatalogueRepository, testDir);

                extractor.Extract();

                //make sure the root mapping files exist for navigating around
                Assert.IsTrue(File.Exists(Path.Combine(testDir.FullName, "hic_data_catalogue.ditamap")));
                Assert.IsTrue(File.Exists(Path.Combine(testDir.FullName, "introduction.dita")));
                Assert.IsTrue(File.Exists(Path.Combine(testDir.FullName, "dataset.dita")));

                //make sure the catalogue we created is there
                FileInfo ditaCatalogueAsDotDitaFile = new FileInfo(Path.Combine(testDir.FullName, "ditaextractorconstructor_extracttestcatalogue_filesexist.dita"));//name of Dita file (for the Catalogue we just created)
                Assert.IsTrue(ditaCatalogueAsDotDitaFile.Exists);
                Assert.IsTrue(File.ReadAllText(ditaCatalogueAsDotDitaFile.FullName).Contains(ditaTestCatalogue.Description));

            }
            finally 
            {
                ditaTestCatalogue.DeleteInDatabase();
                foreach (var file in testDir.GetFiles())
                    file.Delete();
            }
        }

        [Test]
        [ExpectedException(ExpectedMessage = "Dita Extraction requires that each catalogue have a unique Acronym, the catalogue UnitTestCatalogue is missing an Acronym")]
        public void CreateCatalogueWithNoAcronym_CrashesDITAExtractor()
        {
            var testDir = _directoryHelper.Directory;

            try
            {
                //create a new Catalogue in the test datbaase that doesnt have a acronym (should crash Dita Extractor)
                Catalogue myNewCatalogue = new Catalogue(CatalogueRepository, "UnitTestCatalogue");
                myNewCatalogue.Acronym = "";
                myNewCatalogue.SaveToDatabase();

                try
                {
                    DitaCatalogueExtractor extractor = new DitaCatalogueExtractor(CatalogueRepository, testDir);
                    extractor.Extract();
                }
                finally
                {
                    myNewCatalogue.DeleteInDatabase();
                }

            }
            finally
            {
                foreach (var file in testDir.GetFiles())
                    file.Delete();
            }
            
        }

    }
}
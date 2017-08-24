﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using MapsDirectlyToDatabaseTable;
using NUnit.Framework;
using ReusableLibraryCode;
using Tests.Common;

namespace CatalogueLibraryTests.Integration
{
    class ColumnInfoTests : DatabaseTests
    {

 

        [Test]
        public void CreateNewColumnInfoInDatabase_NewColumns_NewColumnsAreEqualAfterSave()
        {
            TableInfo parent = null;
            ColumnInfo child=null;

            try
            {
                parent = new TableInfo(CatalogueRepository, "CHI");
                child = new ColumnInfo(CatalogueRepository, "chi", "varchar(10)", parent)
                {
                    Description = "The community health index, 10 digits of which the first 6 are date of birth",
                    Status = ColumnInfo.ColumnStatus.Active,
                    RegexPattern = "\\d*",
                    ValidationRules = "Last digit must be odd for gents and even for ladies"
                };

                child.SaveToDatabase();

                ColumnInfo childAfter = CatalogueRepository.GetObjectByID<ColumnInfo>(child.ID);

                Assert.AreEqual(child.Name, childAfter.Name);
                Assert.AreEqual(child.Description, childAfter.Description);
                Assert.AreEqual(child.Status, childAfter.Status);
                Assert.AreEqual(child.RegexPattern, childAfter.RegexPattern);
                Assert.AreEqual(child.ValidationRules, childAfter.ValidationRules); 

                //find max lengths for UI
                FieldInfo targetMaxLength = child.GetType().GetField("Description_MaxLength");
                Assert.IsNotNull(targetMaxLength);
                targetMaxLength = child.GetType().GetField("RegexPattern_MaxLength");
                Assert.IsNotNull(targetMaxLength);
                targetMaxLength = child.GetType().GetField("ValidationRules_MaxLength");
                Assert.IsNotNull(targetMaxLength);


            }
            finally 
            {
                child.DeleteInDatabase();
                parent.DeleteInDatabase();
            }
            

        }

        [Test]
        public void GetAllColumnInfos_moreThan1_pass()
        {

            TableInfo parent = new TableInfo(CatalogueRepository, "Slalom");

            try
            {
                var ci = new ColumnInfo(CatalogueRepository, "MyAwesomeColumn","varchar(1000)", parent);
           
                try
                {
                    Assert.IsTrue(CatalogueRepository.GetAllObjectsWithParent<ColumnInfo>(parent).Count() ==1);
                }
                finally
                {
                    ci.DeleteInDatabase();
                }
            }
            finally
            {
                parent.DeleteInDatabase();
            }
        }

        [Test]
        public void CreateNewColumnInfoInDatabase_valid_pass()
        {
            TableInfo parent = new TableInfo(CatalogueRepository, "Lazors");
            ColumnInfo columnInfo = new ColumnInfo(CatalogueRepository, "Lazor Reflection Vol","varchar(1000)",parent);

            Assert.NotNull(columnInfo);

            columnInfo.DeleteInDatabase();

            var ex = Assert.Throws<KeyNotFoundException>(() => CatalogueRepository.GetObjectByID<ColumnInfo>(columnInfo.ID));
            Assert.IsTrue(ex.Message.StartsWith("Could not find ColumnInfo with ID " + columnInfo.ID), ex.Message);

            parent.DeleteInDatabase();
        }

        [Test]
        public void update_changeAllProperties_pass()
        {
            TableInfo parent = new TableInfo(CatalogueRepository, "Rokkits");
            ColumnInfo column = new ColumnInfo(CatalogueRepository, "ExplosiveVol","varchar(1000)", parent)
            {
                Digitisation_specs = "Highly digitizable",
                Format = "Jpeg",
                Name = "mycol",
                Source = "Bazooka",
                Data_type = "Whatever"
            };

            column.SaveToDatabase();

            ColumnInfo columnAfter = CatalogueRepository.GetObjectByID<ColumnInfo>(column.ID);

            Assert.IsTrue(columnAfter.Digitisation_specs == "Highly digitizable");
            Assert.IsTrue(columnAfter.Format == "Jpeg");
            Assert.IsTrue(columnAfter.Name == "mycol");
            Assert.IsTrue(columnAfter.Source == "Bazooka");
            Assert.IsTrue(columnAfter.Data_type == "Whatever");

            columnAfter.DeleteInDatabase();
            parent.DeleteInDatabase();
        }

        [Test]
        public void GetColumnCollationType_TableCatalogueColumnBrowseURL_EqualToLatin1General()
        {
            string collation = UsefulStuff.GetInstance()
                       .GetColumnCollationType(CatalogueRepository.ConnectionString, "Catalogue", "Browse_URL");

            Assert.IsTrue(collation.StartsWith("Latin1_General"));
        }

        [Test]
        public void  Test_GetRAWStageTypeWhenPreLoadDiscardedDilution()
        {
            TableInfo parent = new TableInfo(CatalogueRepository, "Rokkits");
            ColumnInfo column = new ColumnInfo(CatalogueRepository, "MyCol", "varchar(4)", parent);

            var discard = new PreLoadDiscardedColumn(CatalogueRepository, parent, "MyCol");
            discard.SqlDataType = "varchar(10)";
            discard.Destination = DiscardedColumnDestination.Dilute;
            discard.SaveToDatabase();

            Assert.AreEqual("varchar(4)", column.GetRuntimeDataType(LoadStage.PostLoad));
            Assert.AreEqual("varchar(4)", column.GetRuntimeDataType(LoadStage.AdjustStaging));
            Assert.AreEqual("varchar(10)", column.GetRuntimeDataType(LoadStage.AdjustRaw));

            discard.DeleteInDatabase();
            parent.DeleteInDatabase();
       
        }
    }
}
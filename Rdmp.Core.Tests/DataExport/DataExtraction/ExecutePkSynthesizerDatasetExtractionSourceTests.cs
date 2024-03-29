// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FAnsi.Discovery;
using FAnsi.Discovery.TypeTranslation;
using NUnit.Framework;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.DataExport.DataExtraction.Commands;
using Rdmp.Core.DataExport.DataExtraction.Pipeline.Sources;
using Rdmp.Core.DataExport.DataExtraction.UserPicks;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.Repositories.Managers;
using ReusableLibraryCode.Progress;
using Tests.Common.Scenarios;

namespace Rdmp.Core.Tests.DataExport.DataExtraction
{
    public class ExecutePkSynthesizerDatasetExtractionSourceTests : TestsRequiringAnExtractionConfiguration
    {
        //C24D365B7C271E2C1BC884B5801C2961
        Regex reghex = new Regex(@"^HASHED: [A-F\d]{32}");

        [SetUp]
        public void SetHash()
        {
            DataExportRepository.DataExportPropertyManager.SetValue(DataExportProperty.HashingAlgorithmPattern, "CONCAT('HASHED: ',{0})");
        }

        [Test]
        public void Test_CatalogueItems_ExtractionInformationPrimaryKey_IsRespected()
        {
            var request = SetupExtractDatasetCommand("ExtractionInformationPrimaryKey_IsRespected", new[] { "DateOfBirth" });

            var source = new ExecutePkSynthesizerDatasetExtractionSource();
            source.PreInitialize(request, new ThrowImmediatelyDataLoadEventListener());
            var chunk = source.GetChunk(new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());

            Assert.That(chunk.PrimaryKey, Is.Not.Null);
            Assert.That(chunk.Columns.Cast<DataColumn>().ToList(), Has.Count.EqualTo(_columnInfos.Count())); // NO new column added
            Assert.That(chunk.PrimaryKey, Has.Length.EqualTo(1));
            Assert.That(chunk.PrimaryKey.First().ColumnName, Is.EqualTo("DateOfBirth"));
        }

        [Test]
        public void Test_CatalogueItems_ExtractionInformationMultiPrimaryKey_IsRespected()
        {
            var request = SetupExtractDatasetCommand("ExtractionInformationMultiPrimaryKey_IsRespected", new[] { "PrivateID", "DateOfBirth" });

            var source = new ExecutePkSynthesizerDatasetExtractionSource();
            source.PreInitialize(request, new ThrowImmediatelyDataLoadEventListener());
            var chunk = source.GetChunk(new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());

            Assert.That(chunk.PrimaryKey, Is.Not.Null);
            Assert.That(chunk.Columns.Cast<DataColumn>().ToList(), Has.Count.EqualTo(_columnInfos.Count()));
            Assert.That(chunk.PrimaryKey, Has.Length.EqualTo(2));
            Assert.That(chunk.PrimaryKey.First().ColumnName, Is.EqualTo("ReleaseID"));
        }

        [Test]
        public void Test_CatalogueItems_NonExtractedPrimaryKey_AreRespected()
        {
            var request = SetupExtractDatasetCommand("NonExtractedPrimaryKey_AreRespected", new string[] { }, pkColumnInfos: new [] { "DateOfBirth" });

            var source = new ExecutePkSynthesizerDatasetExtractionSource();
            source.PreInitialize(request, new ThrowImmediatelyDataLoadEventListener());
            var chunk = source.GetChunk(new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());

            Assert.That(chunk.PrimaryKey, Is.Not.Null);
            Assert.That(chunk.Columns.Cast<DataColumn>().ToList(), Has.Count.EqualTo(_columnInfos.Count() + 1)); // synth PK is added
            Assert.That(chunk.PrimaryKey, Has.Length.EqualTo(1));
            Assert.That(chunk.PrimaryKey.First().ColumnName, Is.EqualTo("SynthesizedPk"));

            var firstvalue = chunk.Rows[0]["SynthesizedPk"].ToString();
            Assert.IsTrue(reghex.IsMatch(firstvalue));
        }

        [Test]
        public void Test_CatalogueItems_NonExtractedPrimaryKey_MultiTable_PksAreMerged()
        {
            var request = SetupExtractDatasetCommand("MultiTable_PksAreMerged", new string[] { }, new[] { "DateOfBirth" }, true, true);
            
            var source = new ExecutePkSynthesizerDatasetExtractionSource();
            source.PreInitialize(request, new ThrowImmediatelyDataLoadEventListener());
            var chunk = source.GetChunk(new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());

            Assert.That(chunk.PrimaryKey, Is.Not.Null);
            Assert.That(chunk.Columns.Cast<DataColumn>().ToList(), Has.Count.EqualTo(_columnInfos.Count() + 3)); // the "desc" column is added to the existing ones
            Assert.That(chunk.PrimaryKey, Has.Length.EqualTo(1));
            Assert.That(chunk.PrimaryKey.First().ColumnName, Is.EqualTo("SynthesizedPk"));

            var firstvalue = chunk.Rows[0]["SynthesizedPk"].ToString();
            Assert.IsTrue(reghex.IsMatch(firstvalue));

            DiscoveredDatabaseICanCreateRandomTablesIn.ExpectTable("SimpleLookup").Drop();
            DiscoveredDatabaseICanCreateRandomTablesIn.ExpectTable("SimpleJoin").Drop();
        }

        [Test]
        public void Test_CatalogueItems_NonExtractedPrimaryKey_LookupsOnly_IsRespected()
        {
            var request = SetupExtractDatasetCommand("LookupsOnly_IsRespected", new string[] { }, pkColumnInfos: new[] { "DateOfBirth" }, withLookup: true);

            var source = new ExecutePkSynthesizerDatasetExtractionSource();
            source.PreInitialize(request, new ThrowImmediatelyDataLoadEventListener());
            var chunk = source.GetChunk(new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());

            Assert.That(chunk.PrimaryKey, Is.Not.Null);
            Assert.That(chunk.Columns.Cast<DataColumn>().ToList(), Has.Count.EqualTo(_columnInfos.Count() + 2)); // the "desc" column is added to the existing ones + the SynthPk
            Assert.That(chunk.PrimaryKey, Has.Length.EqualTo(1));
            Assert.That(chunk.PrimaryKey.First().ColumnName, Is.EqualTo("SynthesizedPk"));

            var firstvalue = chunk.Rows[0]["SynthesizedPk"].ToString();
            Assert.IsTrue(reghex.IsMatch(firstvalue));

            DiscoveredDatabaseICanCreateRandomTablesIn.ExpectTable("SimpleLookup").Drop();
        }
        
        private void SetupJoin()
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("Name");
            dt.Columns.Add("Description");
            
            dt.Rows.Add(new object[] { "Dave", "Is a maniac" });

            var tbl = DiscoveredDatabaseICanCreateRandomTablesIn.CreateTable("SimpleJoin", dt, new[] { new DatabaseColumnRequest("Name", new DatabaseTypeRequest(typeof(string), 50)) { IsPrimaryKey = true } });

            var lookupCata = Import(tbl);

            ExtractionInformation fkEi = _catalogue.GetAllExtractionInformation(ExtractionCategory.Any).Single(n => n.GetRuntimeName() == "Name");
            ColumnInfo pk = lookupCata.GetTableInfoList(false).Single().ColumnInfos.Single(n => n.GetRuntimeName() == "Name");
            
            new JoinInfo(CatalogueRepository,fkEi.ColumnInfo, pk, ExtractionJoinType.Left, null);

            var ci = new CatalogueItem(CatalogueRepository, _catalogue, "Name_2");
            var ei = new ExtractionInformation(CatalogueRepository, ci, pk, pk.Name)
            {
                Alias = "Name_2"
            };
            ei.SaveToDatabase();
        }

        private void SetupLookupTable()
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("Name");
            dt.Columns.Add("Description");

            dt.Rows.Add(new object[] { "Dave", "Is a maniac" });
            
            var tbl = DiscoveredDatabaseICanCreateRandomTablesIn.CreateTable("SimpleLookup", dt, new[] { new DatabaseColumnRequest("Name", new DatabaseTypeRequest(typeof(string), 50)) });

            var lookupCata = Import(tbl);

            ExtractionInformation fkEi = _catalogue.GetAllExtractionInformation(ExtractionCategory.Any).Single(n => n.GetRuntimeName() == "Name");
            ColumnInfo pk = lookupCata.GetTableInfoList(false).Single().ColumnInfos.Single(n => n.GetRuntimeName() == "Name");

            ColumnInfo descLine1 = lookupCata.GetTableInfoList(false).Single().ColumnInfos.Single(n => n.GetRuntimeName() == "Description");

            var cmd = new ExecuteCommandCreateLookup(CatalogueRepository, fkEi, descLine1, pk, null, true); 
            cmd.Execute();
        }

        private ExtractDatasetCommand SetupExtractDatasetCommand(string testTableName, string[] pkExtractionColumns, string[] pkColumnInfos = null, bool withLookup = false, bool withJoin = false)
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("PrivateID");
            dt.Columns.Add("Name");
            dt.Columns.Add("DateOfBirth");

            if (pkColumnInfos != null)
                dt.PrimaryKey =
                    dt.Columns.Cast<DataColumn>().Where(col => pkColumnInfos.Contains(col.ColumnName)).ToArray();

            dt.Rows.Add(new object[] { _cohortKeysGenerated.Keys.First(), "Dave", "2001-01-01" });

            var tbl = DiscoveredDatabaseICanCreateRandomTablesIn.CreateTable(testTableName, 
                dt, 
                new[] { new DatabaseColumnRequest("Name", new DatabaseTypeRequest(typeof(string), 50))});

            TableInfo tableInfo;
            ColumnInfo[] columnInfos;
            CatalogueItem[] cataItems;
            ExtractionInformation[] extractionInformations;
            _catalogue = Import(tbl, out tableInfo, out columnInfos, out cataItems, out extractionInformations);

            ExtractionInformation privateID = extractionInformations.First(e => e.GetRuntimeName().Equals("PrivateID"));
            privateID.IsExtractionIdentifier = true;
            privateID.SaveToDatabase();

            if (withLookup)
                SetupLookupTable();

            if (withJoin)
                SetupJoin();

            _catalogue.ClearAllInjections();
            extractionInformations = _catalogue.GetAllExtractionInformation(ExtractionCategory.Any);

            foreach (var pkExtractionColumn in pkExtractionColumns)
            {
                ExtractionInformation column = extractionInformations.First(e => e.GetRuntimeName().Equals(pkExtractionColumn));
                column.IsPrimaryKey = true;
                column.SaveToDatabase();
            }

            ExtractionConfiguration configuration;
            IExtractableDataSet extractableDataSet;
            Project project;

            SetupDataExport(testTableName, _catalogue,
                            out configuration, out extractableDataSet, out project);

            configuration.Cohort_ID = _extractableCohort.ID;
            configuration.SaveToDatabase();

            return new ExtractDatasetCommand( configuration, new ExtractableDatasetBundle(extractableDataSet));
        }

        private void SetupDataExport(string testDbName, Catalogue catalogue, out ExtractionConfiguration extractionConfiguration, out IExtractableDataSet extractableDataSet, out Project project)
        {
            extractableDataSet = new ExtractableDataSet(DataExportRepository, catalogue);

            project = new Project(DataExportRepository, testDbName);
            project.ProjectNumber = 1;

            Directory.CreateDirectory(@"C:\temp\");
            project.ExtractionDirectory = @"C:\temp\";

            project.SaveToDatabase();

            extractionConfiguration = new ExtractionConfiguration(DataExportRepository, project);
            extractionConfiguration.AddDatasetToConfiguration(extractableDataSet);

            foreach (var ei in _catalogue.GetAllExtractionInformation(ExtractionCategory.Supplemental))
            {
                extractionConfiguration.AddColumnToExtraction(extractableDataSet, ei);   
            }
        }
    }
}
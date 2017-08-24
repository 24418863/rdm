﻿using CatalogueLibrary.Repositories;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.Data.DataTables.DataSetPackages;
using ReusableUIComponents.Copying;

namespace RDMPObjectVisualisation.Copying.Commands
{
    public class ExtractableDataSetCommand : ICommand
    {
        public ExtractableDataSet[] ExtractableDataSets { get; set; }

        public ExtractableDataSetCommand(ExtractableDataSet extractableDataSet)
        {
            ExtractableDataSets = new ExtractableDataSet[]{extractableDataSet};
        }

        public ExtractableDataSetCommand(ExtractableDataSet[] extractableDataSetArray)
        {
            ExtractableDataSets = extractableDataSetArray;
        }

        public ExtractableDataSetCommand(ExtractableDataSetPackage extractableDataSetPackage)
        {
            var repository = (IDataExportRepository) extractableDataSetPackage.Repository;
            var packagecontents = new ExtractableDataSetPackageContents(repository);
            ExtractableDataSets = packagecontents.GetAllDataSets(extractableDataSetPackage,repository.GetAllObjects<ExtractableDataSet>());
        }

        public string GetSqlString()
        {
            return null;
        }
    }
}
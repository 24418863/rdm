using System.Collections.Generic;
using CatalogueLibrary.Data;
using CatalogueLibrary.QueryBuilding;
using CatalogueLibrary.Repositories;
using DataExportLibrary.Interfaces.Data.DataTables;
using DataExportLibrary.Interfaces.ExtractionTime.UserPicks;

namespace DataExportLibrary.Interfaces.ExtractionTime.Commands
{
    public interface IExtractDatasetCommand:IExtractCommand
    {
        IRDMPPlatformRepositoryServiceLocator RepositoryLocator { get;}

        IExtractableCohort ExtractableCohort { get; set; }
        ICatalogue Catalogue { get; }
        IExtractionDirectory Directory { get; set; }
        IExtractableDatasetBundle DatasetBundle { get; }
        List<IColumn> ColumnsToExtract { get; set; }

        ISqlQueryBuilder QueryBuilder { get; set; }

    }
}
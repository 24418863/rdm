﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Runtime.CompilerServices;
using CatalogueLibrary.DataHelper;
using CatalogueLibrary.Repositories;
using MapsDirectlyToDatabaseTable;
using ReusableLibraryCode;
using ReusableUIComponents.Annotations;

namespace CatalogueLibrary.Data
{
    public enum ExtractionCategory
    {
        Core,
        Supplemental,
        SpecialApprovalRequired,
        Internal,
        Deprecated,
        Any

    }

    /// <summary>
    /// Describes in a single line of SELECT SQL a transform to perform on an underlying ColumnInfo.  ExtractionInformation is the technical implementation 
    /// of what is described by a CatalogueItem.  Most ExtractionInformations in your database will just be direct extraction (verbatim) of the ColumnInfo
    /// however you might have simple transformations e.g. 'UPPER([MyDatabase]..[Users].[Name]' or even call complex SQL scalar functions for example
    /// 'fn_CleanDrugCode([Prescribing]..[Items].[DrugCode])'
    /// 
    /// Note that alias is stored separately because it is useful for GetRuntimeName().  Also note that you should not have newlines in your SelectSQL 
    /// since this will likely confuse QueryBuilder.
    /// 
    /// The interface ExtractionInformationUI handles all of these requirements transparentely.  Also recorded in ExtractionInformation is ExtractionCategory
    /// which lets you flag the sensitivity of the data being extracted e.g. SpecialApprovalRequired
    /// 
    /// Finally one ExtractionInformation (and only one) in each CatalogueItem set (of parent Catalogue) must be flagged as IsExtractionIdentifier.  This 
    /// is the column which will be joined against cohorts in data extraction linkages.  This should be the private identifier you use to identify people
    /// in your datasets (e.g. Community Health Index or NHS Number).
    /// </summary>
    public class ExtractionInformation : ConcreteColumn, IDeleteable, IComparable, IHasDependencies
    {
        private ColumnInfo _columnInfo;
        private CatalogueItem _catalogueItem;
        private bool _columnInfoFoundToBeNull = false;

        public static int SelectSQL_MaxLength = -1;
        
        

        #region Properties 
        
        private int _catalogueItemID;
        private ExtractionCategory _extractionCategory;

        //For other properties see ConcreteColumn
        public int CatalogueItem_ID
        {
            get { return _catalogueItemID; }
            set { SetField(ref _catalogueItemID , value); }
        }

        public ExtractionCategory ExtractionCategory
        {
            get { return _extractionCategory; }
            set
            {
                if (_extractionCategory == ExtractionCategory.Any)
                    throw new ArgumentException("Any is only usable as an extraction argument and cannot be assigned to an ExtractionInformation");

                SetField(ref _extractionCategory, value);
            }
        }
        
        #endregion

        #region Relationships
        //These fields are fetched (cached version) from lookup link table - ExtractionInformation can only exist where there is a relationship between a CatalogueItem and a ColumnInfo
        [NoMappingToDatabase]
        public CatalogueItem CatalogueItem
        {
            get
            {
                //Cache answer the first time it is requested
                if(_catalogueItem == null)
                    _catalogueItem = Repository.GetObjectByID<CatalogueItem>(CatalogueItem_ID);

                return _catalogueItem;
            }
        }

        [NoMappingToDatabase]
        public override ColumnInfo ColumnInfo
        {
            get
            {
                //Cache answer the first time it is requested
                if (_columnInfo == null && !_columnInfoFoundToBeNull)
                {
                    //The cached answer
                    _columnInfo = CatalogueItem.ColumnInfo;

                    //oh oh! it's null! flag that it was found to be null in order to prevent constantly trying to work it out every time ColumnInfo property is interrogated.
                    if (_columnInfo == null)
                        _columnInfoFoundToBeNull = true;
                }
                
                return _columnInfo;
            }
        }

        [NoMappingToDatabase]
        public IEnumerable<ExtractionFilter> ExtractionFilters {
            get { return Repository.GetAllObjectsWithParent<ExtractionFilter>(this); }
        }
        #endregion


        public ExtractionInformation(ICatalogueRepository repository, CatalogueItem catalogueItem, ColumnInfo column, string selectSQL)
        {
            repository.InsertAndHydrate(this, new Dictionary<string, object>
            {
                {"SelectSQL", string.IsNullOrWhiteSpace(selectSQL) ? column.Name : selectSQL},
                {"Order", 1},
                {"ExtractionCategory", "Core"},
                {"CatalogueItem_ID",catalogueItem.ID}
            });

            if (catalogueItem.ColumnInfo_ID == null)
                repository.SaveSpecificPropertyOnlyToDatabase(catalogueItem, "ColumnInfo_ID", column.ID);
            else
                if (catalogueItem.ColumnInfo_ID != column.ID)
                    throw new ArgumentException("Cannot create an ExtractionInformation for CatalogueItem " +
                                                catalogueItem + " with ColumnInfo " + column +
                                                " because the CatalogueItem is already associated with a different ColumnInfo: " +
                                                catalogueItem.ColumnInfo);
            
        }

        public ExtractionInformation(ICatalogueRepository repository, DbDataReader r): base(repository, r)
        {
            SelectSQL = r["SelectSQL"].ToString();

            ExtractionCategory cat;
            if (ExtractionCategory.TryParse(r["ExtractionCategory"].ToString(), out cat))
                ExtractionCategory = cat;
            else
                throw new Exception("Unrecognised ExtractionCategory \"" + r["ExtractionCategory"] + "\"");

            Order = int.Parse(r["Order"].ToString());

            Alias = r["Alias"] as string;

            HashOnDataRelease = (bool)r["HashOnDataRelease"];
            IsExtractionIdentifier = (bool) r["IsExtractionIdentifier"];
            IsPrimaryKey = (bool) r["IsPrimaryKey"];
            CatalogueItem_ID = (int) r["CatalogueItem_ID"];


        }

        public override string ToString()
        {
            //prefer alias, then prefer catalogue name
            if (!string.IsNullOrWhiteSpace(Alias))
                return Alias;
            try
            {
                return GetRuntimeName();
            }
            catch (Exception)
            {
                return "BROKEN ExtractionInformation:" + SelectSQL;
            }
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }
        
        public int CompareTo(object obj)
        {
            if(obj is ExtractionInformation)
            {
                return this.Order - (obj as ExtractionInformation).Order;
            }
            else
                return 0;
        }

        public IHasDependencies[] GetObjectsThisDependsOn()
        {
            return new IHasDependencies[] {ColumnInfo};
        }

        public IHasDependencies[] GetObjectsDependingOnThis()
        {
            List<IHasDependencies> dependencies = new List<IHasDependencies>();
            
            dependencies.AddRange(ExtractionFilters);
            dependencies.Add(CatalogueItem);

            return dependencies.ToArray();
        }

        public bool IsProperTransform()
        {
            if (string.IsNullOrWhiteSpace(SelectSQL))
                return false;

            if (ColumnInfo == null)
                return false;

            //if the selct sql is different from the column underlying it then it is a proper transform (not just a copy paste)
            return !SelectSQL.Equals(ColumnInfo.Name);
        }
    }
}
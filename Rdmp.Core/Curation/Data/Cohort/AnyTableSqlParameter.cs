// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using FAnsi.Discovery;
using FAnsi.Discovery.QuerySyntax;
using MapsDirectlyToDatabaseTable;
using MapsDirectlyToDatabaseTable.Attributes;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.Curation.Data.Referencing;
using Rdmp.Core.QueryBuilding.SyntaxChecking;
using Rdmp.Core.Repositories;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;

namespace Rdmp.Core.Curation.Data.Cohort
{
    /// <summary>
    /// Allows you to override ALL instances of a given named parameter e.g. @studyStartDate in ALL AggregateFilterParameters in a given CohortIdentificationConfiguration
    /// with a single value.  This allows you to have multiple filters in different datasets that all use @studyStartDate parameter but override it globally for the configuration
    /// so that you don't have to manually update every parameter when you want to change your study criteria.  For this to work all AggregateFilterParameters must have the same name
    /// and datatype AND comment! as the study filters (see CohortQueryBuilder).
    /// </summary>
    public class AnyTableSqlParameter : ReferenceOtherObjectDatabaseEntity, ISqlParameter,IHasDependencies
    {
        #region Database Properties
        private string _parameterSQL;
        private string _value;
        private string _comment;
        
        /// <inheritdoc/>
        [Sql]
        public string ParameterSQL
        {
            get { return _parameterSQL; }
            set { SetField(ref  _parameterSQL, value); }
        }

        /// <inheritdoc/>
        [Sql]
        public string Value
        {
            get { return _value; }
            set { SetField(ref  _value, value); }
        }

        /// <inheritdoc/>
        public string Comment
        {
            get { return _comment; }
            set { SetField(ref  _comment, value); }
        }

        #endregion

        /// <inheritdoc/>
        [NoMappingToDatabase]
        public string ParameterName
        {
            get { return QuerySyntaxHelper.GetParameterNameFromDeclarationSQL(ParameterSQL); }
        }

        /// <summary>
        /// Declares that a new <see cref="ISqlParameter"/> (e.g. 'DECLARE @bob as varchar(10)') exists for the parent database object.  The object
        /// should be of a type which passes <see cref="IsSupportedType"/>.  When the object is used for query generation by an <see cref="QueryBuilding.ISqlQueryBuilder"/>
        /// then the parameter will be used 
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="parent"></param>
        /// <param name="parameterSQL"></param>
        public AnyTableSqlParameter(ICatalogueRepository repository, IMapsDirectlyToDatabaseTable parent, string parameterSQL)
        {
            repository.InsertAndHydrate(this,new Dictionary<string, object>
            {
                {"ReferencedObjectID",parent.ID},
                {"ReferencedObjectType",parent.GetType().Name},
                {"ReferencedObjectRepositoryType",parent.Repository.GetType().Name},
                {"ParameterSQL", parameterSQL},
            });
        }

        internal AnyTableSqlParameter(ICatalogueRepository repository, DbDataReader r)
            : base(repository, r)
        {
            Value = r["Value"] as string;
            ParameterSQL = r["ParameterSQL"] as string;
            Comment = r["Comment"] as string;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ParameterName;
        }

        /// <inheritdoc cref="ParameterSyntaxChecker"/>
        public void Check(ICheckNotifier notifier)
        {
            new ParameterSyntaxChecker(this).Check(notifier);
        }

        /// <inheritdoc/>
        public IQuerySyntaxHelper GetQuerySyntaxHelper()
        {
            var parentWithQuerySyntaxHelper = GetOwnerIfAny() as IHasQuerySyntaxHelper;

            if (parentWithQuerySyntaxHelper == null)
                throw new AmbiguousDatabaseTypeException("Could not figure out what the query syntax helper is for " + this);

            return parentWithQuerySyntaxHelper.GetQuerySyntaxHelper();
        }

        /// <summary>
        /// Returns true if the Type (which should implement <see cref="IMapsDirectlyToDatabaseTable"/>) is one which is designed to store it's <see cref="ISqlParameter"/>
        /// in this table.  Only supported objects will have parameters sought here by <see cref="QueryBuilding.ISqlQueryBuilder"/>s.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <seealso cref="DescribeUseCaseForParent"/>
        public static bool IsSupportedType(Type type)
        {
            return DescribeUseCaseForParent(type) != null;
        }

        /// <summary>
        /// Describes how the <see cref="ISqlParameter"/>s declared in this table will be used with parents of the supplied Type (See <see cref="ReferenceOtherObjectDatabaseEntity.ReferencedObjectType"/>).
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <seealso cref="IsSupportedType"/>
        public static string DescribeUseCaseForParent(Type type)
        {
            if (type == typeof(CohortIdentificationConfiguration))
                return "SQLParameters at this level are global for a given cohort identification configuration task e.g. @StudyWindowStartDate which could then be used by 10 datasets within that configuration";

            if (type == typeof (AggregateConfiguration))
                return "SQLParameters at this level are intended for fulfilling table valued function parameters and centralising parameter declarations across multiple AggregateFilter(s) within a single AggregateConfiguration (note that while these are 'global' with respect to the filters, if the AggregateConfiguration is part of a multiple configuration CohortIdentificationConfiguration then this is less 'global' than those declared at that level)";

            if (type == typeof(TableInfo))
                return "SQLParameters at this level are intended for fulfilling table valued function parameters, note that these should/can be overridden later on e.g. in Extraction/Cohort generation.  This value is intended to give a baseline result which can be run through DataQualityEngine and Checking etc";

            return null;
        }

        /// <summary>
        /// Returns the parent object that declares this paramter (see <see cref="ReferenceOtherObjectDatabaseEntity.ReferencedObjectID"/> and <see cref="ReferenceOtherObjectDatabaseEntity.ReferencedObjectType"/>)
        /// </summary>
        /// <returns></returns>
        public IMapsDirectlyToDatabaseTable GetOwnerIfAny()
        {
            var type = typeof (Catalogue).Assembly.GetTypes().Single(t=>t.Name.Equals(ReferencedObjectType));

            return Repository.GetObjectByID(type,ReferencedObjectID);
        }

        /// <inheritdoc/>
        public IHasDependencies[] GetObjectsThisDependsOn()
        {
            return new IHasDependencies[0];
        }

        /// <inheritdoc/>
        public IHasDependencies[] GetObjectsDependingOnThis()
        {
            var parent = GetOwnerIfAny() as IHasDependencies;

            if (parent != null)
                return new[] {parent};

            return new IHasDependencies[0];
        }
    }
}

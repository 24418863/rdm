// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using MapsDirectlyToDatabaseTable;
using MapsDirectlyToDatabaseTable.Attributes;
using Rdmp.Core.Curation.Data.Cache;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Repositories;
using ReusableLibraryCode.Annotations;

namespace Rdmp.Core.Curation.Data
{
    /// <inheritdoc cref="ILoadProgress"/>
    public class LoadProgress : DatabaseEntity, ILoadProgress
    {
        #region Database Properties
        private bool _isDisabled;
        private string _name;
        private DateTime? _originDate;
        private string _loadPeriodicity;
        private DateTime? _dataLoadProgress;
        private int _loadMetadata_ID;
        private int _defaultNumberOfDaysToLoadEachTime;

        /// <inheritdoc/>
        public bool IsDisabled
        {
            get { return _isDisabled; }
            set { SetField(ref _isDisabled, value); }
        }
        /// <inheritdoc/>
        [NotNull]
        [Unique]
        public string Name
        {
            get { return _name; }
            set { SetField(ref _name, value); }
        }
        /// <inheritdoc/>
        public DateTime? OriginDate
        {
            get { return _originDate; }
            set { SetField(ref _originDate, value); }
        }

        /// <summary>
        /// Not used
        /// </summary>
        [Obsolete("Do not use")]
        public string LoadPeriodicity
        {
            get { return _loadPeriodicity; }
            set { SetField(ref _loadPeriodicity, value); }
        }
        /// <inheritdoc/>
        public DateTime? DataLoadProgress
        {
            get { return _dataLoadProgress; }
            set { SetField(ref _dataLoadProgress, value); }
        }
        /// <inheritdoc/>
        public int LoadMetadata_ID
        {
            get { return _loadMetadata_ID; }
            set { SetField(ref _loadMetadata_ID, value); }
        }

        /// <inheritdoc/>
        public int DefaultNumberOfDaysToLoadEachTime
        {
            get { return _defaultNumberOfDaysToLoadEachTime; }
            set { SetField(ref _defaultNumberOfDaysToLoadEachTime, value); }
        }

        #endregion
        #region Relationships
        /// <inheritdoc/>
        [NoMappingToDatabase]
        public ILoadMetadata LoadMetadata { get { return Repository.GetObjectByID<LoadMetadata>(LoadMetadata_ID); }}

        /// <inheritdoc/>
        [NoMappingToDatabase]
        public ICacheProgress CacheProgress
        {
            get
            {
                return Repository.GetAllObjectsWithParent<CacheProgress>(this).SingleOrDefault();
            }
        }
#endregion

        /// <inheritdoc cref="ILoadProgress"/>
        public LoadProgress(ICatalogueRepository repository, LoadMetadata parent)
        {
            repository.InsertAndHydrate(this,  
            new Dictionary<string, object>()
            {
                {"Name", Guid.NewGuid().ToString()},
                {"LoadMetadata_ID", parent.ID}
            });
        }

        internal LoadProgress(ICatalogueRepository repository, DbDataReader r)
            : base(repository, r)
        {
            Name = r["Name"] as string;
            OriginDate = ObjectToNullableDateTime(r["OriginDate"]);
            DataLoadProgress = ObjectToNullableDateTime(r["DataLoadProgress"]);
            LoadMetadata_ID = int.Parse(r["LoadMetaData_ID"].ToString());
            _loadPeriodicity = r["LoadPeriodicity"].ToString();
            IsDisabled = Convert.ToBoolean(r["IsDisabled"]);
            DefaultNumberOfDaysToLoadEachTime = Convert.ToInt32(r["DefaultNumberOfDaysToLoadEachTime"]);
        }
        
        /// <inheritdoc/>
        public override string ToString()
        {
            return Name + " ID=" + ID;
        }
    }
}

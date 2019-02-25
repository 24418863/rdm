// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data.Common;
using FAnsi.Discovery;

namespace HIC.Logging.PastEvents
{
    /// <summary>
    /// Readonly audit of a table that was loaded as part of a historical data load (See HIC.Logging.ArchivalDataLoadInfo).
    /// </summary>
    public class ArchivalTableLoadInfo : IArchivalLoggingRecordOfPastEvent, IComparable
    {
        public ArchivalDataLoadInfo Parent { get; private set; }

        private readonly DiscoveredDatabase _loggingDatabase;

        public int ID { get; internal set; }
        public DateTime Start { get; internal set; }
        public DateTime? End { get; internal set; }
        public string TargetTable { get; internal set; }
        public int? Inserts { get; internal set; }
        public int? Deletes { get; internal set; }
        public int? Updates { get; internal set; }
        public string Notes { get; internal set; }

        public List<ArchivalDataSource> DataSources { get { return _knownDataSource.Value; }}

        readonly Lazy<List<ArchivalDataSource>> _knownDataSource;

        public ArchivalTableLoadInfo(ArchivalDataLoadInfo parent, DbDataReader r,DiscoveredDatabase loggingDatabase)
        {
            Parent = parent;
            _loggingDatabase = loggingDatabase;

            ID = Convert.ToInt32(r["ID"]);
            Start = (DateTime)r["startTime"];

            var e = r["endTime"];
            if (e == null || e == DBNull.Value)
                End = null;
            else
                End = (DateTime)e;

            TargetTable = (string)r["targetTable"];

            Inserts = ToNullableInt(r["inserts"]);
            Updates = ToNullableInt(r["updates"]);
            Deletes = ToNullableInt(r["deletes"]);
            Notes = r["notes"] as string;

            _knownDataSource = new Lazy<List<ArchivalDataSource>>(GetDataSources);
        }
        private List<ArchivalDataSource> GetDataSources()
        {
            List<ArchivalDataSource> toReturn = new List<ArchivalDataSource>();

            using (var con = _loggingDatabase.Server.GetConnection())
            {
                con.Open();

                var cmd = _loggingDatabase.Server.GetCommand("SELECT * FROM DataSource WHERE tableLoadRunID=" + ID, con);
                var r = cmd.ExecuteReader();

                while (r.Read())
                    toReturn.Add(new ArchivalDataSource(r));
            }

            return toReturn;
        }
        private int? ToNullableInt(object i)
        {
            if (i == null || i == DBNull.Value)
                return null;

            return Convert.ToInt32(i);

        }
        
        public override string ToString()
        {
            return Start + " - " + TargetTable + " (Inserts=" + Inserts + ",Updates=" + Updates + ",Deletes=" + Deletes +")";
        }

        public int CompareTo(object obj)
        {
            var other = obj as ArchivalTableLoadInfo;
            if (other != null)
                if (Start == other.Start)
                    return 0;
                else
                    return Start > other.Start ? 1 : -1;

            return System.String.Compare(ToString(), obj.ToString(), System.StringComparison.Ordinal);
        }
    }
}
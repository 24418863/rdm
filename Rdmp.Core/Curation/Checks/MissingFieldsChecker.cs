// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FAnsi.Discovery;
using MapsDirectlyToDatabaseTable;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Spontaneous;
using ReusableLibraryCode.Checks;

namespace Rdmp.Core.Curation.Checks
{
    /// <summary>
    /// Checks that the table definitions match the class definitions for all <see cref="DatabaseEntity"/>
    /// </summary>
    public class MissingFieldsChecker : ICheckable
    {
        private readonly TableRepository _repository;

        public MissingFieldsChecker(TableRepository repository)
        {
            this._repository = repository;
        }
        
        public void Check(ICheckNotifier notifier)
        {
            var server = _repository.DiscoveredServer;
            if(!server.Exists())
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Could not reach server",CheckResult.Fail));
                return;
            }
                
            var db = server.GetCurrentDatabase();
            if(!db.Exists())
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Could not find database " + db ,CheckResult.Fail));
                return;
            }
                
            var tables = db.DiscoverTables(false);

            foreach (Type type in _repository.GetCompatibleTypes())
                CheckEntities(notifier, type, tables);
        }

        /// <summary>
        /// Checks the object representation (Type) is perfectly synched with the underlying database (table must exist with matching name and all parameters must be column names with no unmapped fields)
        /// </summary>
        /// <param name="notifier"></param>
        /// <param name="type"></param>
        /// <param name="tables"></param>
        private void CheckEntities(ICheckNotifier notifier, Type type, DiscoveredTable[] tables)
        {
            if(type.IsInterface)
                return;

            if (type.IsAbstract)
                return;

            if (typeof(SpontaneousObject).IsAssignableFrom(type))
                return;

            if(type.Name.StartsWith("Spontaneous"))
                return;

            //make sure argument was IMaps..
            if(!typeof(IMapsDirectlyToDatabaseTable).IsAssignableFrom(type))
                throw new ArgumentException("Type " + type.Name + " passed into method was not an IMapsDirectlyToDatabaseTable");

            //make sure table exists with exact same name as class
            DiscoveredTable table = tables.SingleOrDefault(t => t.GetRuntimeName().Equals(type.Name));

            if (table == null)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Could not find Table called " + type.Name +" (which implements IMapsDirectlyToDatabaseTable)",CheckResult.Fail, null));
                return;   
            }
            
            notifier.OnCheckPerformed(new CheckEventArgs("Found Table " + type.Name, CheckResult.Success, null));
            
            
            //get columns from underlying database table
            DiscoveredColumn[] columns = table.DiscoverColumns();

            //get the properties that are not explicitly set as not mapping to database
            PropertyInfo[] properties = TableRepository.GetPropertyInfos(type);

            //this is part of the interface and hence doesnt exist in the underlying data table
            properties = properties.Where(p => !p.Name.Equals("UpdateCommand")).ToArray();

            //find columns in database where there are no properties with the same name
            IEnumerable<DiscoveredColumn> missingProperties = columns.Where(col => !properties.Any(p => p.Name.Equals(col.GetRuntimeName())));
            IEnumerable<PropertyInfo> missingDatabaseFields = properties.Where(p => !columns.Any(col => col.GetRuntimeName().Equals(p.Name)));

            bool problems = false;
            foreach (DiscoveredColumn missingProperty in missingProperties)
            {
                notifier.OnCheckPerformed(new CheckEventArgs(
                    "Missing property " + missingProperty + " on class definition " + type.FullName + ", the underlying table contains this field but the class does not", CheckResult.Fail,
                    null));
                problems = true;
            }

            foreach (PropertyInfo missingDatabaseField in missingDatabaseFields)
            {
                notifier.OnCheckPerformed(new CheckEventArgs(
                    "Missing field in database table " + table + " when compared to class definition " + type.FullName 
                    + " property was called " + missingDatabaseField.Name 
                    + " and was of type " + missingDatabaseField.PropertyType
                    + ((typeof(Enum).IsAssignableFrom(missingDatabaseField.PropertyType)?"(An Enum)":""))
                    , CheckResult.Warning,
                    null));
                problems = true;
            }

            //Check nullability
            foreach (PropertyInfo nonNullableProperty in properties.Where(property=> property.PropertyType.IsEnum || property.PropertyType.IsValueType))
            {

                //it is something like int? i.e. a nullable value type
                if (Nullable.GetUnderlyingType(nonNullableProperty.PropertyType) != null)
                    continue;
                
                try
                {
                    var col = table.DiscoverColumn(nonNullableProperty.Name);

                    if (col.AllowNulls)
                        notifier.OnCheckPerformed(new CheckEventArgs("Column " + nonNullableProperty.Name + " in table " + table + " allows nulls but is mapped to a ValueType or Enum", CheckResult.Warning, null));
                }
                catch (Exception e)
                {
                    notifier.OnCheckPerformed(new CheckEventArgs("Could not check nullability of column " + nonNullableProperty.Name + " in table " + table,CheckResult.Fail, e));
                }
            }

            if (!problems)
                notifier.OnCheckPerformed(new CheckEventArgs("All fields present and correct in Type/Table " + table,CheckResult.Success,null));
        }
    }
}

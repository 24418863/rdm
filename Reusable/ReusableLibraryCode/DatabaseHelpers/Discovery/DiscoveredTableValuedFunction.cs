﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReusableLibraryCode.DatabaseHelpers.Discovery
{
    public class DiscoveredTableValuedFunction : DiscoveredTable
    {
        private string _functionName;
        
        //constructor
        public DiscoveredTableValuedFunction(DiscoveredDatabase database, string functionName, IQuerySyntaxHelper querySyntaxHelper):base(database,functionName,querySyntaxHelper)
        {
            _functionName = functionName;
        }

        public override bool Exists(IManagedTransaction transaction = null)
        {
            return Database.DiscoverTableValuedFunctions(transaction).Any(f=>f.GetRuntimeName().Equals(GetRuntimeName()));
        }

        public override string GetRuntimeName()
        {
            return _querySyntaxHelper.GetRuntimeName(_functionName);
        }

        public override string GetFullyQualifiedName()
        {
            //This is pretty inefficient that we have to go discover these in order to tell you how to invoke the table!
            string parameters = string.Join(",", DiscoverParameters().Select(p => p.ParameterName));

            //Note that we do not give the parameters values, the client must decide appropriate values and put them in correspondingly named variables
            return Database.GetRuntimeName() + ".." + GetRuntimeName() + "(" + parameters + ")";
        }

        public override DiscoveredColumn[] DiscoverColumns(IManagedTransaction managedTransaction = null)
        {
            using (var connection = Database.Server.GetManagedConnection(managedTransaction))
                return Helper.DiscoverColumns(this, connection, Database.GetRuntimeName(), GetRuntimeName());
        }

        public override string ToString()
        {
            return _functionName;
        }

        public override void Drop(IManagedTransaction transaction = null)
        {
            using (var connection = Database.Server.GetManagedConnection(transaction))
                Helper.DropFunction(connection.Connection, this, connection.Transaction);
        }

        public DiscoveredParameter[] DiscoverParameters(ManagedTransaction transaction = null)
        {
            using (var connection = Database.Server.GetManagedConnection(transaction))
                return Helper.DiscoverTableValuedFunctionParameters(connection.Connection, this, connection.Transaction);
        }
    }
}
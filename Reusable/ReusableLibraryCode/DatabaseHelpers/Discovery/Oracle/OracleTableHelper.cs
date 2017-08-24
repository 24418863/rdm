﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Oracle.ManagedDataAccess.Client;

namespace ReusableLibraryCode.DatabaseHelpers.Discovery.Oracle
{
    public class OracleTableHelper : IDiscoveredTableHelper
    {

        public string GetTopXSqlForTable(IHasFullyQualifiedNameToo table, int topX)
        {
            return "SELECT * FROM " + table.GetFullyQualifiedName() + " WHERE ROWNUM <= " + topX;
        }

        public DiscoveredColumn[] DiscoverColumns(DiscoveredTable discoveredTable, IManagedConnection connection, string database, string tableName)
        {
            List<DiscoveredColumn> columns = new List<DiscoveredColumn>();

            

                DbCommand cmd = DatabaseCommandHelper.GetCommand(@"SELECT *
FROM   all_tab_cols
WHERE  table_name = :table_name
", connection.Connection);
                cmd.Transaction = connection.Transaction;

                DbParameter p = new OracleParameter("table_name", OracleDbType.Varchar2);
                p.Value = tableName;
                cmd.Parameters.Add(p);

                using (var r = cmd.ExecuteReader())
                {
                    if (!r.HasRows)
                        throw new Exception("Could not find any columns for table " + tableName +
                                            " in database " + database);

                    while (r.Read())
                    {

                        var toAdd = new DiscoveredColumn(discoveredTable, (string)r["COLUMN_NAME"], r["NULLABLE"].ToString() != "N") { Format = r["CHARACTER_SET_NAME"] as string };
                        toAdd.DataType = new DiscoveredDataType(r, GetSQLType_From_all_tab_cols_Result(r), toAdd);
                        columns.Add(toAdd);
                    }

                }

                //get primary key information 
                cmd = new OracleCommand(@"SELECT cols.table_name, cols.column_name, cols.position, cons.status, cons.owner
FROM all_constraints cons, all_cons_columns cols
WHERE cols.table_name = :table_name
AND cons.constraint_type = 'P'
AND cons.constraint_name = cols.constraint_name
AND cons.owner = cols.owner
ORDER BY cols.table_name, cols.position", (OracleConnection) connection.Connection);
                cmd.Transaction = connection.Transaction;


                p = new OracleParameter("table_name",OracleDbType.Varchar2);
                p.Value = tableName;
                cmd.Parameters.Add(p);


                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        columns.Single(c => c.GetRuntimeName().Equals(r["COLUMN_NAME"])).IsPrimaryKey = true;//mark all primary keys as primary
                }

                return columns.ToArray();
        }


        public IDiscoveredColumnHelper GetColumnHelper()
        {
            return new OracleColumnHelper();
        }

        public void DropTable(DbConnection connection, DiscoveredTable table, DbTransaction dbTransaction = null)
        {
            if (dbTransaction != null)
                throw new NotSupportedException("It looks like you are trying to drop a dataabase within a transaction, Oracle does not support transactions at the DDL layer.  If I were to drop this then it wouldn't ever be coming back");

            var cmd = new OracleCommand("DROP TABLE " +table.GetFullyQualifiedName(), (OracleConnection)connection);
            cmd.Transaction = dbTransaction as OracleTransaction;
            cmd.ExecuteNonQuery();
        }

        public void DropColumn(DbConnection connection, DiscoveredTable discoveredTable, DiscoveredColumn columnToDrop,
            DbTransaction dbTransaction)
        {
            throw new NotImplementedException();
        }

        public int GetRowCount(DbConnection connection, IHasFullyQualifiedNameToo table, DbTransaction dbTransaction = null)
        {
            var cmd = new OracleCommand("select count(*) from " + table.GetFullyQualifiedName(), (OracleConnection) connection);
            cmd.Transaction = dbTransaction as OracleTransaction;
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public string WrapStatementWithIfTableExistanceMatches(bool existanceDesiredForExecution, StringLiteralSqlInContext bodySql, string tableName)
        {
            //make it dynamic if it isn't already
            bodySql.Escape(new OracleQuerySyntaxHelper());

            //ensure name sanitisation incase user passes in a fully expressed name
            tableName = new OracleQuerySyntaxHelper().GetRuntimeName(tableName);

            return string.Format(@"
declare
nCount NUMBER;
v_sql LONG;

begin
SELECT count(*) into nCount FROM dba_tables where table_name = '{0}';
IF(nCount {1} 0)
THEN
v_sql:='{2}';
execute immediate v_sql;

END IF;
end;
", 
       tableName,
       existanceDesiredForExecution?">":"=",
       bodySql.Sql
       );


            


        }



        private string SensibleTypeFromOracleType(DbDataReader r)
        {
            int? precision = null;
            int? scale = null;

            if (r["DATA_SCALE"] != DBNull.Value)
                scale = Convert.ToInt32(r["DATA_SCALE"]);
            if (r["DATA_PRECISION"] != DBNull.Value)
                precision = Convert.ToInt32(r["DATA_PRECISION"]);


            switch (r["DATA_TYPE"] as string)
            {
                case "VARCHAR2": return "varchar";
                case "NUMBER":
                    if (scale == 0 && precision == null)
                        return "int";
                    else if (precision != null && scale != null)
                        return "decimal";
                    else
                        throw new Exception(
                            string.Format("Found Oracle NUMBER datatype with scale {0} and precision {1}, did not know what datatype to use to represent it",
                            scale != null ? scale.ToString() : "DBNull.Value",
                            precision != null ? precision.ToString() : "DBNull.Value"));
                case "FLOAT":
                    return "double";
                default:
                    return r["DATA_TYPE"].ToString().ToLower();
            }
        }

        private string GetSQLType_From_all_tab_cols_Result(DbDataReader r)
        {
            string columnType = SensibleTypeFromOracleType(r);

            string lengthQualifier = "";
            
            if (UsefulStuff.HasPrecisionAndScale(columnType))
                lengthQualifier = "(" + r["DATA_PRECISION"] + "," + r["DATA_SCALE"] + ")";
            else
                if (UsefulStuff.RequiresLength(columnType))
                    lengthQualifier = "(" + r["DATA_LENGTH"] + ")";

            return columnType + lengthQualifier;
        }

        public void DropFunction(DbConnection connection, DiscoveredTableValuedFunction functionToDrop, DbTransaction dbTransaction)
        {
            throw new NotImplementedException();
        }

        public DiscoveredColumn[] DiscoverColumns(DiscoveredTableValuedFunction discoveredTableValuedFunction,
            IManagedConnection connection, string database, string tableName)
        {
            throw new NotImplementedException();
        }

        public DiscoveredParameter[] DiscoverTableValuedFunctionParameters(DbConnection connection,
            DiscoveredTableValuedFunction discoveredTableValuedFunction, DbTransaction transaction)
        {
            throw new NotImplementedException();
        }
    }
}
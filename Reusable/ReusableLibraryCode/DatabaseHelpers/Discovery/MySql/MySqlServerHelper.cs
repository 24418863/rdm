﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using MySql.Data.MySqlClient;

namespace ReusableLibraryCode.DatabaseHelpers.Discovery.MySql
{
    public class MySqlServerHelper : DiscoveredServerHelper
    {
        public MySqlServerHelper() : base(DatabaseType.MYSQLServer)
        {
        }

        protected override string ServerKeyName { get { return "Server"; } }
        protected override string DatabaseKeyName { get { return "Database"; } }

        #region Up Typing
        public override DbCommand GetCommand(string s, DbConnection con, DbTransaction transaction = null)
        {
            return new MySqlCommand(s.Replace("[", "`").Replace("]", "`"), con as MySqlConnection, transaction as MySqlTransaction);
        }

        public override DbDataAdapter GetDataAdapter(DbCommand cmd)
        {
            return new MySqlDataAdapter(cmd as MySqlCommand);
        }

        public override DbCommandBuilder GetCommandBuilder(DbCommand cmd)
        {
            return new MySqlCommandBuilder((MySqlDataAdapter) GetDataAdapter(cmd));
        }

        public override DbParameter GetParameter(string parameterName)
        {
            return new MySqlParameter(parameterName,null);
        }

        public override DbConnection GetConnection(DbConnectionStringBuilder builder)
        {
            return new MySqlConnection(builder.ConnectionString);
        }

        public override DbConnectionStringBuilder GetConnectionStringBuilder(string connectionString)
        {
            return new MySqlConnectionStringBuilder(connectionString);
        }
        #endregion

        public override DbConnectionStringBuilder GetConnectionStringBuilder(string server, string database, string username, string password)
        {
            var toReturn = new MySqlConnectionStringBuilder() {Server = server, Database = database };
            if (!string.IsNullOrWhiteSpace(username))
            {
                toReturn.UserID = username;
                toReturn.Password = password;
            }
            else
                toReturn.IntegratedSecurity = true;

            return toReturn;
        }

        public override DbConnectionStringBuilder ChangeDatabase(DbConnectionStringBuilder builder, string newDatabase)
        {
            builder[DatabaseKeyName] = newDatabase;
            return builder;
        }

        public override DbConnectionStringBuilder EnableAsync(DbConnectionStringBuilder builder)
        {
            return builder; //no special stuff required?
        }

        public override IDiscoveredDatabaseHelper GetDatabaseHelper()
        {
            return new MySqlDatabaseHelper();
        }

        public override IQuerySyntaxHelper GetQuerySyntaxHelper()
        {
            return new MySqlQuerySyntaxHelper();
        }

        public override void CreateDatabase(DbConnectionStringBuilder builder, IHasRuntimeName newDatabaseName)
        {
            var b = new MySqlConnectionStringBuilder(builder.ConnectionString);
            b.Database = null;

            using(var con = new MySqlConnection(b.ConnectionString))
            {
                con.Open();
                GetCommand("CREATE DATABASE " + newDatabaseName.GetRuntimeName(),con).ExecuteNonQuery();
            }
        }

        public override Dictionary<string, string> DescribeServer(DbConnectionStringBuilder builder)
        {
            throw new NotImplementedException();
        }

        public override bool RespondsWithinTime(DbConnectionStringBuilder builder, int timeoutInSeconds, out Exception exception)
        {
            throw new NotImplementedException();
        }

        public override string[] ListDatabases(DbConnectionStringBuilder builder)
        {
            var b = new MySqlConnectionStringBuilder(builder.ConnectionString);
            b.Database = null;

            using (var con = new MySqlConnection(b.ConnectionString))
            {
                con.Open();
                return ListDatabases(con);
            }
        }
        public override string[] ListDatabases(DbConnection con)
        {
            var cmd = GetCommand("show databases;", con); //already comes as single column called Database

            var r = cmd.ExecuteReader();

            List<string> databases = new List<string>();

            while (r.Read())
                databases.Add((string)r["Database"]);

            con.Close();
            return databases.ToArray();
        }
    }
}
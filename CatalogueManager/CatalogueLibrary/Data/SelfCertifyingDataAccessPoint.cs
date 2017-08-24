﻿using CatalogueLibrary.Repositories;
using MapsDirectlyToDatabaseTable;
using ReusableLibraryCode;
using ReusableLibraryCode.DataAccess;

namespace CatalogueLibrary.Data
{
    public class SelfCertifyingDataAccessPoint : EncryptedPasswordHost, IDataAccessCredentials, IDataAccessPoint
    {
        /// <summary>
        /// Normally to open a connection to an IDataAccessPoint (location of server/database) you also need an optional IDataAccessCredentials (username and encrypted password).  These
        /// are usually two separate objects e.g. TableInfo and DataAccessCredentials (optional - if ommmited connections use integrated/windows security).  
        /// 
        /// Instead of doing that however, you can use this class to store all the bits in one object that implements both interfaces.  It can then be used with a DataAccessPortal.
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="databaseType"></param>
        public SelfCertifyingDataAccessPoint(CatalogueRepository repository, DatabaseType databaseType) : base(repository)
        {
            DatabaseType = databaseType;
        }

        public string Server { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }

        [NoMappingToDatabase]
        public DatabaseType DatabaseType { get; set; }

        public IDataAccessCredentials GetCredentialsIfExists(DataAccessContext context)
        {
            //this class is not configured with a username so pretend like we don't have any credentials
            if (string.IsNullOrWhiteSpace(Username))
                return null;

            //this class is it's own credentials
            return this;
        }
    }
}
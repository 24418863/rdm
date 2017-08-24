﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data.Aggregation;
using CatalogueLibrary.Data.Cohort;
using CatalogueLibrary.QueryBuilding;
using NUnit.Framework;
using Tests.Common;

namespace CatalogueLibraryTests.Integration.TableValuedFunctionTests
{
    public class AggregationTests :DatabaseTests
    {
        TestableTableValuedFunction _function = new TestableTableValuedFunction();

        [SetUp]
        public void CreateTestData()
        {
            _function.Create(DatabaseICanCreateRandomTablesIn, CatalogueRepository);
        }
        
        [Test]
        public void GenerateAggregateManuallyTest()
        {
            //do a count * on the query builder
            AggregateBuilder queryBuilder = new AggregateBuilder("", "count(*)", null,new[] { _function.TableInfoCreated });

            Assert.IsTrue(queryBuilder.SQL.Contains(@"SELECT"));
            Assert.IsTrue(queryBuilder.SQL.Contains(@"count(*)"));

            Assert.IsTrue(queryBuilder.SQL.Contains(@"DECLARE @name AS varchar(50);"));
            Assert.IsTrue(queryBuilder.SQL.Contains(@"SET @name='fish';"));

            Assert.IsTrue(queryBuilder.SQL.Contains("..MyAwesomeFunction(@startNumber,@stopNumber,@name) AS MyAwesomeFunction"));

            Console.WriteLine(queryBuilder.SQL);
        }
        
        [Test]
        public void GenerateAggregateViaAggregateConfigurationTest()
        {
            var agg = new AggregateConfiguration(CatalogueRepository, _function.Cata, "MyExcitingAggregate");
            
            try
            {
                agg.HavingSQL = "count(*)>1";
                agg.SaveToDatabase();

                var aggregateForcedJoin = new AggregateForcedJoin(CatalogueRepository);
                aggregateForcedJoin.CreateLinkBetween(agg, _function.TableInfoCreated);

                AggregateBuilder queryBuilder = agg.GetQueryBuilder();
                
                Assert.AreEqual(
                    @"DECLARE @startNumber AS int;
SET @startNumber=5;
DECLARE @stopNumber AS int;
SET @stopNumber=10;
DECLARE @name AS varchar(50);
SET @name='fish';
/*MyExcitingAggregate*/
SELECT 
count(*)
FROM 
[" + TestDatabaseNames.Prefix +@"ScratchArea]..MyAwesomeFunction(@startNumber,@stopNumber,@name) AS MyAwesomeFunction
HAVING
count(*)>1", queryBuilder.SQL);
                
            }
            finally
            {
                agg.DeleteInDatabase();
            }
        }

        [Test]
        public void GenerateAggregateUsingOverridenParametersTest()
        {
            var agg = new AggregateConfiguration(CatalogueRepository, _function.Cata, "MyExcitingAggregate");

            try
            {
                var param = new AnyTableSqlParameter(CatalogueRepository, agg, "DECLARE @name AS varchar(50)");
                param.Value = "'lobster'";
                param.SaveToDatabase();

                var aggregateForcedJoin = new AggregateForcedJoin(CatalogueRepository);
                aggregateForcedJoin.CreateLinkBetween(agg, _function.TableInfoCreated);

                //do a count * on the query builder
                AggregateBuilder queryBuilder = agg.GetQueryBuilder();

                Assert.IsTrue(queryBuilder.SQL.Contains(@"SELECT"));
                Assert.IsTrue(queryBuilder.SQL.Contains(@"count(*)"));

                //should have this version of things 
                Assert.IsTrue(queryBuilder.SQL.Contains(@"DECLARE @name AS varchar(50);"));
                Assert.IsTrue(queryBuilder.SQL.Contains(@"SET @name='lobster';"));

                //isntead of this verison of things
                Assert.IsFalse(queryBuilder.SQL.Contains(@"SET @name='fish';"));

                Assert.IsTrue(queryBuilder.SQL.Contains("..MyAwesomeFunction(@startNumber,@stopNumber,@name) AS MyAwesomeFunction"));

                Console.WriteLine(queryBuilder.SQL);
            }
            finally
            {
                agg.DeleteInDatabase();
            }
        }
        [TearDown]
        public void Destroy()
        {
            _function.Destroy();
        }
    }
}
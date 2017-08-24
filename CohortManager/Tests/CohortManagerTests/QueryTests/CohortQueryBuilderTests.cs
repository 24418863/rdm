﻿using System;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Aggregation;
using CatalogueLibrary.FilterImporting;
using CatalogueLibrary.FilterImporting.Construction;
using CohortManagerLibrary.QueryBuilding;
using NUnit.Framework;
using Tests.Common;

namespace CohortManagerTests.QueryTests
{
    public class CohortQueryBuilderTests : CohortIdentificationTests
    {
        private readonly string _scratchDatabaseName = TestDatabaseNames.GetConsistentName("ScratchArea");
        
        [Test]
        public void TestGettingAggregateJustFromConfig_DistinctCHISelect()
        {
            CohortQueryBuilder builder = new CohortQueryBuilder(aggregate1,null);

            Assert.AreEqual(@"/*UnitTestAggregate1*/
SELECT distinct
[" + _scratchDatabaseName + @"]..[BulkData].[chi]
FROM 
[" + _scratchDatabaseName + @"]..[BulkData]", builder.SQL);
        }
        
        [Test]
        public void TestGettingAggregateJustFromConfig_SelectStar()
        {
            CohortQueryBuilder builder = new CohortQueryBuilder(aggregate1, null);

            Assert.AreEqual(@"	/*UnitTestAggregate1*/
	SELECT top 1000
	*
	FROM 
	[" + _scratchDatabaseName + @"]..[BulkData]", builder.GetDatasetSampleSQL());
        }
        [Test]
        public void TestGettingAggregateSQLFromEntirity()
        {
            CohortQueryBuilder builder = new CohortQueryBuilder(cohortIdentificationConfiguration);


            Assert.AreEqual(null, aggregate1.GetCohortAggregateContainerIfAny());

            //set the order so that 2 comes before 1
            rootcontainer.AddChild(aggregate2, 1);
            rootcontainer.AddChild(aggregate1, 5);

            Assert.AreEqual(rootcontainer,aggregate1.GetCohortAggregateContainerIfAny());
            try
            {
                Assert.AreEqual(@"(
	/*UnitTestAggregate2*/
	SELECT distinct
	[" + _scratchDatabaseName + @"]..[BulkData].[chi]
	FROM 
	[" + _scratchDatabaseName + @"]..[BulkData]

	EXCEPT

	/*UnitTestAggregate1*/
	SELECT distinct
	[" + _scratchDatabaseName + @"]..[BulkData].[chi]
	FROM 
	[" + _scratchDatabaseName + @"]..[BulkData]
)".Trim(), builder.SQL.Trim());
            }
            finally 
            {
                rootcontainer.RemoveChild(aggregate1);
                rootcontainer.RemoveChild(aggregate2);
            }
        }

        [Test]
        public void TestOrdering_AggregateThenContainer()
        {
            CohortQueryBuilder builder = new CohortQueryBuilder(cohortIdentificationConfiguration);

            //set the order so that a configuration is in position 1 
            rootcontainer.AddChild(aggregate1, 1);

            //then a container in position 2
            container1.Order = 2;
            container1.SaveToDatabase();
            rootcontainer.AddChild(container1);

            //container 1 contains both other aggregates
            container1.AddChild(aggregate2, 1);
            container1.AddChild(aggregate3, 2);

            

            try
            {
                var allConfigurations = rootcontainer.GetAllAggregateConfigurationsRecursively();
                Assert.IsTrue(allConfigurations.Contains(aggregate1));
                Assert.IsTrue(allConfigurations.Contains(aggregate2));
                Assert.IsTrue(allConfigurations.Contains(aggregate3));

                Assert.AreEqual(@"(
	/*UnitTestAggregate1*/
	SELECT distinct
	[" + _scratchDatabaseName + @"]..[BulkData].[chi]
	FROM 
	[" + _scratchDatabaseName + @"]..[BulkData]

	EXCEPT


	(
		/*UnitTestAggregate2*/
		SELECT distinct
		[" + _scratchDatabaseName + @"]..[BulkData].[chi]
		FROM 
		[" + _scratchDatabaseName + @"]..[BulkData]

		UNION

		/*UnitTestAggregate3*/
		SELECT distinct
		[" + _scratchDatabaseName + @"]..[BulkData].[chi]
		FROM 
		[" + _scratchDatabaseName + @"]..[BulkData]
	)

)".Trim(), builder.SQL.Trim());
            }
            finally
            {
                container1.RemoveChild(aggregate2);
                container1.RemoveChild(aggregate3);
                rootcontainer.RemoveChild(aggregate1);
            }
        }

        [Test]
        public void TestOrdering_ContainerThenAggregate()
        {
            CohortQueryBuilder builder = new CohortQueryBuilder(cohortIdentificationConfiguration);

            //set the order so that a configuration is in position 1 
            rootcontainer.AddChild(aggregate1, 2);

            //then a container in position 2
            container1.Order = 1;
            container1.SaveToDatabase();
            rootcontainer.AddChild(container1);

            //container 1 contains both other aggregates
            container1.AddChild(aggregate2, 1);
            container1.AddChild(aggregate3, 2);
            
            try
            {
                Assert.AreEqual(@"(

	(
		/*UnitTestAggregate2*/
		SELECT distinct
		[" + _scratchDatabaseName + @"]..[BulkData].[chi]
		FROM 
		[" + _scratchDatabaseName + @"]..[BulkData]

		UNION

		/*UnitTestAggregate3*/
		SELECT distinct
		[" + _scratchDatabaseName + @"]..[BulkData].[chi]
		FROM 
		[" + _scratchDatabaseName + @"]..[BulkData]
	)


	EXCEPT

	/*UnitTestAggregate1*/
	SELECT distinct
	[" + _scratchDatabaseName + @"]..[BulkData].[chi]
	FROM 
	[" + _scratchDatabaseName + @"]..[BulkData]
)".Trim(), builder.SQL.Trim());
            }
            finally
            {
                container1.RemoveChild(aggregate2);
                container1.RemoveChild(aggregate3);
                rootcontainer.RemoveChild(aggregate1);
            }
        }

        [Test]
        public void TestGettingAggregateSQLFromEntirity_IncludingParametersAtTop()
        {
            CohortQueryBuilder builder = new CohortQueryBuilder(cohortIdentificationConfiguration);

            //setup a filter (all filters must be in a container so the container is a default AND container)
            var AND = new AggregateFilterContainer(CatalogueRepository,FilterContainerOperation.AND);
            var filter = new AggregateFilter(CatalogueRepository,"hithere",AND);

            //give the filter an implicit parameter requiring bit of SQL
            filter.WhereSQL = "1=@abracadabra";
            filter.SaveToDatabase();
            
            
            //get it to create the parameters for us
            new ParameterCreator(new AggregateFilterFactory(CatalogueRepository), null, null).CreateAll(filter, null);

            //get the parameter it just created, set it's value and save it
            var param = (AggregateFilterParameter) filter.GetAllParameters().Single();
            param.Value = "1";
            param.ParameterSQL = "DECLARE @abracadabra AS int";
            param.SaveToDatabase();

            //Make aggregate1 use the filter we just setup
            aggregate1.RootFilterContainer_ID = AND.ID;
            aggregate1.SaveToDatabase();
            
            //set the order so that 2 comes before 1
            rootcontainer.AddChild(aggregate2, 1);
            rootcontainer.AddChild(aggregate1, 5);
            
            try
            {
                Assert.AreEqual(@"DECLARE @abracadabra AS int;
SET @abracadabra=1;

(
	/*UnitTestAggregate2*/
	SELECT distinct
	[" + _scratchDatabaseName + @"]..[BulkData].[chi]
	FROM 
	[" + _scratchDatabaseName + @"]..[BulkData]

	EXCEPT

	/*UnitTestAggregate1*/
	SELECT distinct
	[" + _scratchDatabaseName + @"]..[BulkData].[chi]
	FROM 
	[" + _scratchDatabaseName + @"]..[BulkData]
	WHERE
	(
	/*hithere*/
	1=@abracadabra
	)
)
", builder.SQL);


                CohortQueryBuilder builder2 = new CohortQueryBuilder(aggregate1, null);
                Assert.AreEqual(@"DECLARE @abracadabra AS int;
SET @abracadabra=1;
/*UnitTestAggregate1*/
SELECT distinct
[" + _scratchDatabaseName + @"]..[BulkData].[chi]
FROM 
[" + _scratchDatabaseName + @"]..[BulkData]
WHERE
(
/*hithere*/
1=@abracadabra
)", builder2.SQL);


                string selectStar = new CohortQueryBuilder(aggregate1,null).GetDatasetSampleSQL();

                Assert.AreEqual(@"DECLARE @abracadabra AS int;
SET @abracadabra=1;

	/*UnitTestAggregate1*/
	SELECT top 1000
	*
	FROM 
	["+TestDatabaseNames.Prefix+@"ScratchArea]..[BulkData]
	WHERE
	(
	/*hithere*/
	1=@abracadabra
	)", selectStar);
            }
            finally
            {
                filter.DeleteInDatabase();
                AND.DeleteInDatabase();

                rootcontainer.RemoveChild(aggregate1);
                rootcontainer.RemoveChild(aggregate2);

            }
        }


        [Test]
        public void TestGettingAggregateSQLFromEntirity_StopEarly()
        {
            rootcontainer.AddChild(aggregate1,1);
            rootcontainer.AddChild(aggregate2,2);
            rootcontainer.AddChild(aggregate3,3);
            
            CohortQueryBuilder builder = new CohortQueryBuilder(rootcontainer, null);
            
            builder.StopContainerWhenYouReach = aggregate2;
            try
            {
                Assert.AreEqual(@"
(
	/*UnitTestAggregate1*/
	SELECT distinct
	[" + _scratchDatabaseName + @"]..[BulkData].[chi]
	FROM 
	[" + _scratchDatabaseName + @"]..[BulkData]

	EXCEPT

	/*UnitTestAggregate2*/
	SELECT distinct
	[" + _scratchDatabaseName + @"]..[BulkData].[chi]
	FROM 
	[" + _scratchDatabaseName + @"]..[BulkData]
)
", builder.SQL);


                CohortQueryBuilder builder2 = new CohortQueryBuilder(rootcontainer, null);
            builder2.StopContainerWhenYouReach = null;
            Assert.AreEqual(@"
(
	/*UnitTestAggregate1*/
	SELECT distinct
	[" + _scratchDatabaseName + @"]..[BulkData].[chi]
	FROM 
	[" + _scratchDatabaseName + @"]..[BulkData]

	EXCEPT

	/*UnitTestAggregate2*/
	SELECT distinct
	[" + _scratchDatabaseName + @"]..[BulkData].[chi]
	FROM 
	[" + _scratchDatabaseName + @"]..[BulkData]

	EXCEPT

	/*UnitTestAggregate3*/
	SELECT distinct
	[" + _scratchDatabaseName + @"]..[BulkData].[chi]
	FROM 
	[" + _scratchDatabaseName + @"]..[BulkData]
)
", builder2.SQL);
            }
            finally
            {
                rootcontainer.RemoveChild(aggregate1);
                rootcontainer.RemoveChild(aggregate2);
                rootcontainer.RemoveChild(aggregate3);

            }
        }

        [Test]
        public void TestGettingAggregateSQLFromEntirity_StopEarlyContainer()
        {
            rootcontainer.AddChild(aggregate1, -5);

            container1.AddChild(aggregate2, 2);
            container1.AddChild(aggregate3, 3);
            
            rootcontainer.AddChild(container1);

            AggregateConfiguration aggregate4 = new AggregateConfiguration(CatalogueRepository, testData.catalogue, "UnitTestAggregate4");
            new AggregateDimension(CatalogueRepository, testData.extractionInformations.Single(e => e.GetRuntimeName().Equals("chi")), aggregate4);

            rootcontainer.AddChild(aggregate4,5);
            CohortQueryBuilder builder = new CohortQueryBuilder(rootcontainer, null);

            //Looks like:
            /*
             * 
            EXCEPT
            Aggregate 1
              UNION            <-----We tell it to stop after this container
               Aggregate2
               Aggregate3
            Aggregate 4
            */
            builder.StopContainerWhenYouReach = container1;
            try
            {
                Assert.AreEqual(@"
(
	/*UnitTestAggregate1*/
	SELECT distinct
	[" + _scratchDatabaseName + @"]..[BulkData].[chi]
	FROM 
	[" + _scratchDatabaseName + @"]..[BulkData]

	EXCEPT


	(
		/*UnitTestAggregate2*/
		SELECT distinct
		[" + _scratchDatabaseName + @"]..[BulkData].[chi]
		FROM 
		[" + _scratchDatabaseName + @"]..[BulkData]

		UNION

		/*UnitTestAggregate3*/
		SELECT distinct
		[" + _scratchDatabaseName + @"]..[BulkData].[chi]
		FROM 
		[" + _scratchDatabaseName + @"]..[BulkData]
	)

)
", builder.SQL);
            }
            finally
            {
                rootcontainer.RemoveChild(aggregate1);
                rootcontainer.RemoveChild(aggregate4);
                container1.RemoveChild(aggregate2);
                container1.RemoveChild(aggregate3);

                aggregate4.DeleteInDatabase();
            }
        }

        [Test]
        public void TestHavingSQL()
        {
            rootcontainer.AddChild(aggregate1, -5);

            container1.AddChild(aggregate2, 2);
            container1.AddChild(aggregate3, 3);

            aggregate2.HavingSQL = "count(*)>1";
            aggregate2.SaveToDatabase();
            aggregate1.HavingSQL = "SUM(Result)>10";
            aggregate1.SaveToDatabase();
            try
            {

                rootcontainer.AddChild(container1);

                CohortQueryBuilder builder = new CohortQueryBuilder(rootcontainer, null);
                Assert.AreEqual(@"
(
	/*UnitTestAggregate1*/
	SELECT distinct
	[" + _scratchDatabaseName + @"]..[BulkData].[chi]
	FROM 
	[" + _scratchDatabaseName + @"]..[BulkData]
	group by 
	[" + _scratchDatabaseName + @"]..[BulkData].[chi]
	HAVING
	SUM(Result)>10

	EXCEPT


	(
		/*UnitTestAggregate2*/
		SELECT distinct
		[" + _scratchDatabaseName + @"]..[BulkData].[chi]
		FROM 
		[" + _scratchDatabaseName + @"]..[BulkData]
		group by 
		[" + _scratchDatabaseName + @"]..[BulkData].[chi]
		HAVING
		count(*)>1

		UNION

		/*UnitTestAggregate3*/
		SELECT distinct
		[" + _scratchDatabaseName + @"]..[BulkData].[chi]
		FROM 
		[" + _scratchDatabaseName + @"]..[BulkData]
	)

)
", builder.SQL);

            }
            finally
            {
                rootcontainer.RemoveChild(aggregate1);

                container1.RemoveChild(aggregate2);
                container1.RemoveChild(aggregate3);

                aggregate2.HavingSQL = null;
                aggregate2.SaveToDatabase();
                aggregate1.HavingSQL = null;
                aggregate1.SaveToDatabase();
            }
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void TestGettingAggregateSQLFromEntirity_TwoFilterParametersPerDataset(bool valuesAreSame)
        {
            CohortQueryBuilder builder = new CohortQueryBuilder(cohortIdentificationConfiguration);

            //setup a filter (all filters must be in a container so the container is a default AND container)
            var AND1 = new AggregateFilterContainer(CatalogueRepository,FilterContainerOperation.AND);
            var filter1_1 = new AggregateFilter(CatalogueRepository,"filter1_1",AND1);
            var filter1_2 = new AggregateFilter(CatalogueRepository,"filter1_2",AND1);

             var AND2 = new AggregateFilterContainer(CatalogueRepository,FilterContainerOperation.AND);
            var filter2_1 = new AggregateFilter(CatalogueRepository,"filter2_1",AND2);
            var filter2_2 = new AggregateFilter(CatalogueRepository,"filter2_2",AND2);

            //give the filter an implicit parameter requiring bit of SQL
            foreach (var filter in new IFilter[]{filter1_1,filter1_2,filter2_1,filter2_2})
            {
                filter.WhereSQL = "@bob = 'bob'";
                filter.SaveToDatabase();     
                //get it to create the parameters for us
                new ParameterCreator(new AggregateFilterFactory(CatalogueRepository), null, null).CreateAll(filter, null);
                
                //get the parameter it just created, set it's value and save it
                var param = (AggregateFilterParameter) filter.GetAllParameters().Single();
                param.Value = "'Boom!'";
                param.ParameterSQL = "DECLARE @bob AS varchar(10)";
                
                //if test case is different values then we change the values of the parameters
                if (!valuesAreSame && (filter.Equals(filter2_1) || Equals(filter, filter2_2)))
                    param.Value = "'Grenades Are Go'";

                param.SaveToDatabase();
            }
            
            //Make aggregate1 use the filter set we just setup
            aggregate1.RootFilterContainer_ID = AND1.ID;
            aggregate1.SaveToDatabase();

             //Make aggregate3 use the other filter set we just setup
            aggregate2.RootFilterContainer_ID = AND2.ID;
            aggregate2.SaveToDatabase();
            
            //set the order so that 2 comes before 1
            rootcontainer.AddChild(aggregate2, 1);
            rootcontainer.AddChild(aggregate1, 5);

             Console.WriteLine( builder.SQL);

             try
             {
                 if (valuesAreSame)
                 {
                     Assert.AreEqual(@"DECLARE @bob AS varchar(10);
SET @bob='Boom!';

(
	/*UnitTestAggregate2*/
	SELECT distinct
	[" + _scratchDatabaseName + @"]..[BulkData].[chi]
	FROM 
	[" + _scratchDatabaseName + @"]..[BulkData]
	WHERE
	(
	/*filter2_1*/
	@bob = 'bob'
	AND
	/*filter2_2*/
	@bob = 'bob'
	)

	EXCEPT

	/*UnitTestAggregate1*/
	SELECT distinct
	[" + _scratchDatabaseName + @"]..[BulkData].[chi]
	FROM 
	[" + _scratchDatabaseName + @"]..[BulkData]
	WHERE
	(
	/*filter1_1*/
	@bob = 'bob'
	AND
	/*filter1_2*/
	@bob = 'bob'
	)
)
", builder.SQL);
                 }
                 else
                 {
                     Assert.AreEqual(@"DECLARE @bob AS varchar(10);
SET @bob='Grenades Are Go';
DECLARE @bob_2 AS varchar(10);
SET @bob_2='Boom!';

(
	/*UnitTestAggregate2*/
	SELECT distinct
	["+TestDatabaseNames.Prefix+@"ScratchArea]..[BulkData].[chi]
	FROM 
	["+TestDatabaseNames.Prefix+@"ScratchArea]..[BulkData]
	WHERE
	(
	/*filter2_1*/
	@bob = 'bob'
	AND
	/*filter2_2*/
	@bob = 'bob'
	)

	EXCEPT

	/*UnitTestAggregate1*/
	SELECT distinct
	["+TestDatabaseNames.Prefix+@"ScratchArea]..[BulkData].[chi]
	FROM 
	["+TestDatabaseNames.Prefix+@"ScratchArea]..[BulkData]
	WHERE
	(
	/*filter1_1*/
	@bob_2 = 'bob'
	AND
	/*filter1_2*/
	@bob_2 = 'bob'
	)
)
", builder.SQL);
                 }
             }
             finally
             {
                 rootcontainer.RemoveChild(aggregate2);
                 rootcontainer.RemoveChild(aggregate1);

                 filter1_1.DeleteInDatabase();
                 filter1_2.DeleteInDatabase();
                 filter2_1.DeleteInDatabase();
                 filter2_2.DeleteInDatabase();
                 
                 AND1.DeleteInDatabase();
                 AND2.DeleteInDatabase();

             }
         }

    }
}
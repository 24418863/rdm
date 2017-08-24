﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Governance;
using NUnit.Framework;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using Tests.Common;

namespace CatalogueLibraryTests.Integration
{
    public class GovernanceTests:DatabaseTests
    {
        [Test]
        public void TestCreatingGovernance_StartsAtToday()
        {
            var gov = GetGov();

            Assert.NotNull(gov);
            Assert.AreEqual(gov.StartDate,DateTime.Now.Date);
        }
        [Test]
        public void TestCreatingGovernance_ChangeName()
        {
            var gov = GetGov();
            gov.Name = "Fish";
            GovernancePeriod freshCopy = CatalogueRepository.GetObjectByID<GovernancePeriod>(gov.ID);
            
            //local change not applied yet
            Assert.AreNotEqual(gov.Name,freshCopy.Name);
            
            //comitted change to database
            gov.SaveToDatabase();
            
            //notice that this fresh copy is still desynced
            Assert.AreNotEqual(gov.Name,freshCopy.Name);
            
            //sync it
            freshCopy = CatalogueRepository.GetObjectByID<GovernancePeriod>(gov.ID);
            Assert.AreEqual(gov.Name ,freshCopy.Name);

        }

        [Test]
        [ExpectedException(ExpectedMessage = "Cannot insert duplicate key row in object 'dbo.GovernancePeriod' with unique index 'idxGovernancePeriodNameMustBeUnique'. The duplicate key value is (HiDuplicate)", MatchType = MessageMatch.Contains)]
        public void TestCreatingGovernance_CannotHaveSameNames()
        {
            var gov1 = GetGov();
            var gov2 = GetGov();

            gov1.Name = "HiDuplicate";
            gov1.SaveToDatabase();

            gov2.Name = "HiDuplicate";
            gov2.SaveToDatabase();
        }

        [Test]
        [ExpectedException(ExpectedMessage = "GovernancePeriod TestExpiryBeforeStarting expires before it begins!")]
        public void Checkability_ExpiresBeforeStarts()
        {
            var gov = GetGov();
            gov.Name = "TestExpiryBeforeStarting";

            //valid to start with 
            gov.Check(new ThrowImmediatelyCheckNotifier());

            gov.EndDate = DateTime.MinValue;
            gov.Check(new ThrowImmediatelyCheckNotifier());//no longer valid - notice there is no SaveToDatabase because we can shouldnt be going back to db anyway
        }

        [Test]
        [ExpectedException(ExpectedMessage = "There is no end date for GovernancePeriod NeverExpires")]
        public void Checkability_NoExpiryDateWarning()
        {
            var gov = GetGov();
            gov.Name = "NeverExpires";

            //valid to start with 
            gov.Check(new ThrowImmediatelyCheckNotifier(){ThrowOnWarning = true});

        }

        [Test]
        public void GovernsCatalogue()
        {
            var gov = GetGov(); 
            Catalogue c = new Catalogue(CatalogueRepository, "GovernedCatalogue");
            try
            {
                Assert.AreEqual(gov.GovernedCatalogues.Count(), 0);

                //should be no governanced catalogues for this governancer yet
                gov.CreateGovernanceRelationshipTo(c);

                var allCatalogues = gov.GovernedCatalogues.ToArray();
                var governedCatalogue = allCatalogues[0];
                Assert.AreEqual(governedCatalogue, c); //we now govern C
            }
            finally 
            {
                gov.DeleteGovernanceRelationshipTo(c);
                Assert.AreEqual(gov.GovernedCatalogues.Count(), 0); //we govern c nevermore!

                c.DeleteInDatabase();
            }
        }

        [Test]
        [ExpectedException(ExpectedMessage = "Cannot insert duplicate key in object 'dbo.GovernancePeriod_Catalogue'",MatchType = MessageMatch.Contains)]
        public void GovernsSameCatalogueTwice()
        {
            Catalogue c = new Catalogue(CatalogueRepository, "GovernedCatalogue");
            
            var gov = GetGov();
            Assert.AreEqual(gov.GovernedCatalogues.Count(), 0);//should be no governanced catalogues for this governancer yet

            gov.CreateGovernanceRelationshipTo(c);
            gov.CreateGovernanceRelationshipTo(c);
            
        }


        [TearDown]
        public void ClearTempObjects()
        {
            foreach (GovernancePeriod gov in toCleanup.ToArray())
                try
                {
                    foreach (var governed in gov.GovernedCatalogues)
                    {
                        gov.DeleteGovernanceRelationshipTo(governed);
                        governed.DeleteInDatabase();
                    }
                    

                    gov.DeleteInDatabase();
                    toCleanup.Remove(gov);
                }
                catch (Exception)
                {
                    Console.WriteLine("Could not delete object " + gov + " nevermind, unit test probably deleted it itself or something");
                }

            
        }
        List<GovernancePeriod> toCleanup = new List<GovernancePeriod>();
        private GovernancePeriod GetGov()
        {
            GovernancePeriod gov = new GovernancePeriod(CatalogueRepository);
            toCleanup.Add(gov);

            return gov;
        }
    }
}
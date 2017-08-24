﻿using System;
using HIC.Common.Validation;
using HIC.Common.Validation.Constraints;
using HIC.Common.Validation.Constraints.Secondary.Predictor;
using NUnit.Framework;

namespace HIC.Common.Validation.Tests.Constraints.Secondary
{
    [TestFixture]
    class PredictionChiSexTest
    {
        private readonly DateTime _wrongType = DateTime.Now;
        
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Validate_IncompatibleChiType_ThrowsException()
        {
            var p = new Prediction(new ChiSexPredictor(),"gender");
            p.Validate(_wrongType, new[] { "M" }, new[] { "gender" });
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Validate_IncompatibleGenderType_ThrowsException()
        {
            var p = new Prediction(new ChiSexPredictor(), "gender");
            p.Validate(TestConstants._VALID_CHI, new object[] { _wrongType }, new string[] { "gender" });
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Validate_NullChiAndGender_IsIgnored()
        {
            var p = new Prediction(new ChiSexPredictor(), "gender");
            p.Validate(TestConstants._VALID_CHI, null, null);
        }

        [Test]
        [ExpectedException(typeof(MissingFieldException))]
        public void Validate_TargetFieldNotPresent_ThrowsException()
        {
            var p = new Prediction(new ChiSexPredictor(), "gender");
            var otherCols = new object[] {"M"};
            var otherColsNames = new string[] {"amagad"};
            p.Validate(TestConstants._VALID_CHI, otherCols, otherColsNames);
        }

        [Test]
        public void Validate_ConsistentChiAndSex_String_Succeeds()
        {
            var p = new Prediction(new ChiSexPredictor(), "gender");
            var otherCols = new object[] { "M" };
            var otherColsNames = new string[] { "gender" };
            p.Validate(TestConstants._VALID_CHI, otherCols, otherColsNames);
        }
        [Test]
        public void Validate_ConsistentChiAndSex_Char_Succeeds()
        {
            var p = new Prediction(new ChiSexPredictor(), "gender");
            var otherCols = new object[] { 'M' };
            var otherColsNames = new string[] { "gender" };
            p.Validate(TestConstants._VALID_CHI, otherCols, otherColsNames);
        }

        [Test]
        public void Validate_InconsistentChiAndSex_ThrowsException()
        {
            var p = new Prediction(new ChiSexPredictor(), "gender");
            var otherCols = new object[] { "F" };
            var otherColsNames = new string[] { "gender" };
            Assert.NotNull(p.Validate(TestConstants._VALID_CHI, otherCols, otherColsNames));
        }

        [Test]
        public void Validate_ChiAndUnspecifiedGender_Ignored()
        {
            var p = new Prediction(new ChiSexPredictor(), "gender");
            var otherCols = new object[] { "U" };
            var otherColsNames = new string[] { "gender" };
            p.Validate(TestConstants._VALID_CHI, otherCols, otherColsNames);
        }

        [Test]
        public void Validate_ChiAndNullGender_Ignored()
        {
            var p = new Prediction(new ChiSexPredictor(), "gender");
            var otherCols = new object[] { null };
            var otherColsNames = new string[] { "gender" };
            p.Validate(TestConstants._VALID_CHI, otherCols, otherColsNames);
        }
    }
}
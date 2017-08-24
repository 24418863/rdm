﻿using System;
using HIC.Common.Validation;
using HIC.Common.Validation.Constraints;
using HIC.Common.Validation.Constraints.Primary;
using HIC.Common.Validation.Constraints.Secondary;
using HIC.Common.Validation.Constraints.Secondary.Predictor;
using NUnit.Framework;

namespace HIC.Common.Validation.Tests
{
    [TestFixture]
    class PredictionValidationTest
    {

        #region Test arguments

        [TestCase("UNKNOWN")]
        [TestCase("Gender")]
        [ExpectedException(typeof(MissingFieldException))] 
        public void Validate_NullTargetField_GeneratesException(string targetField)
        {
            var prediction = new Prediction(new ChiSexPredictor(), targetField);
            var v = CreateInitialisedValidator(prediction);

            v.Validate(TestConstants.ValidChiAndInconsistentSex);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Validate_NullRule_GeneratesException()
        {
            var prediction = new Prediction(null, "gender");
            var v = CreateInitialisedValidator(prediction);

            v.Validate(TestConstants.ValidChiAndInconsistentSex);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Validate_Uninitialized_GeneratesException()
        {
            var prediction = new Prediction();
            var v = CreateInitialisedValidator(prediction);
            v.Validate(TestConstants.ValidChiAndInconsistentSex);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Validate_UninitializedTarget_GeneratesException()
        {
            var prediction = new Prediction();
            prediction.Rule = new ChiSexPredictor();
            var v = CreateInitialisedValidator(prediction);
            v.Validate(TestConstants.ValidChiAndInconsistentSex);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Validate_UninitializedRule_GeneratesException()
        {
            var prediction = new Prediction();
            prediction.TargetColumn = "chi";
            var v = CreateInitialisedValidator(prediction);
            v.Validate(TestConstants.ValidChiAndInconsistentSex);
        }
        #endregion

        #region Test CHI - with primary constraint & secondary constraint

        [Test]
        public void Validate_ChiHasConsistentSexIndicator_Valid()
        {
            var prediction = new Prediction(new ChiSexPredictor(), "gender");
            var v = CreateInitialisedValidator(prediction);

            Assert.IsNull(v.Validate(TestConstants.ValidChiAndConsistentSex));
        }
        
        [Test]
        public void Validate_ChiIsNull_Valid()
        {
            var prediction = new Prediction(new ChiSexPredictor(), "gender");
            var v = CreateInitialisedValidator(prediction);

            Assert.IsNull(v.Validate(TestConstants.NullChiAndValidSex));
        }

        [Test]
        public void Validate_SexIsNull_Valid()
        {
            var prediction = new Prediction(new ChiSexPredictor(), "gender");
            var v = CreateInitialisedValidator(prediction);

            Assert.IsNull(v.Validate(TestConstants.NullChiAndNullSex));
        }

        [Test]
        public void Validate_CHIHasInconsistentSexIndicator_Invalid()
        {
            var prediction = new Prediction(new ChiSexPredictor(), "gender");
            var v = CreateInitialisedValidator(prediction);

            Assert.NotNull(v.Validate(TestConstants.ValidChiAndInconsistentSex));
        }

        [Test]
        public void Validate_ChiIsInvalid_Invalid()
        {
            var prediction = new Prediction(new ChiSexPredictor(), "gender");
            var v = CreateInitialisedValidator(prediction);

            Assert.NotNull(v.Validate(TestConstants.InvalidChiAndValidSex));
        }

        #endregion

        #region Test CHI - with primary constraint & secondary constraint

        [Test]
        public void Validate_NoPrimaryConstraintChiHasConsistentSexIndicator_Valid()
        {
            var prediction = new Prediction(new ChiSexPredictor(), "gender");
            var v = CreateInitialisedValidatorWithNoPrimaryConstraint(prediction);

            Assert.IsNull(v.Validate(TestConstants.ValidChiAndConsistentSex));
        }

        [Test]
        public void Validate_NoPrimaryConstraintChiIsNull_Valid()
        {
            var prediction = new Prediction(new ChiSexPredictor(), "gender");
            var v = CreateInitialisedValidatorWithNoPrimaryConstraint(prediction);

            Assert.IsNull(v.Validate(TestConstants.NullChiAndNullSex));
        }

        [Test]
        public void Validate_NoPrimaryConstraintCHIHasInconsistentSexIndicator_Invalid()
        {
            var prediction = new Prediction(new ChiSexPredictor(), "gender");
            var v = CreateInitialisedValidatorWithNoPrimaryConstraint(prediction);

            Assert.NotNull(v.Validate(TestConstants.ValidChiAndInconsistentSex));
        }

        [Test]
        public void Validate_NoPrimaryConstraintChiIsInvalid_ValidBecauseWhoCaresIfChiIsInvalid_IfYouDoCareUseAChiPrimaryConstraintInstead()
        {
            var prediction = new Prediction(new ChiSexPredictor(), "gender");
            var v = CreateInitialisedValidatorWithNoPrimaryConstraint(prediction);

            Assert.Null(v.Validate(TestConstants.InvalidChiAndValidSex));
        }

        #endregion
        
        private static Validator CreateInitialisedValidator(SecondaryConstraint prediction)
        {
            var i = new ItemValidator();
            i.PrimaryConstraint = new Chi();
            i.SecondaryConstraints.Add(prediction);

            var v = new Validator();
            v.AddItemValidator(i, "chi", typeof(string));
            return v;
        }

        private static Validator CreateInitialisedValidatorWithNoPrimaryConstraint(SecondaryConstraint prediction)
        {
            var i = new ItemValidator();
            i.SecondaryConstraints.Add(prediction);
            
            var v = new Validator();
            v.AddItemValidator(i, "chi", typeof(string));
            return v;
        }
    }
}
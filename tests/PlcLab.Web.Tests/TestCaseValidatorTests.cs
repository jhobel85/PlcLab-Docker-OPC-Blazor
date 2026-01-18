using Allure.Xunit.Attributes;
using System;
using System.Collections.Generic;
using PlcLab.Domain;
using Xunit;

namespace PlcLab.Web.Tests
{
    [AllureSuite("Domain Validation")]
    public class TestCaseValidatorTests        
    {
        [Theory]
        [InlineData(null, false, "No required signals defined.")]
        [InlineData("empty", false, "No required signals defined.")]
        [InlineData("has", true, null)]
            [AllureFeature("Required Signals")]
        public void ValidateRequiredSignals_Scenarios(string? kind, bool expected, string? expectedError)
        {
            var signals = kind switch
            {
                null => (List<SignalSnapshot>?)null,
                "empty" => new List<SignalSnapshot>(),
                _ => new List<SignalSnapshot> { new() { SignalName = "S1" } }
            };

            var testCase = new TestCase { RequiredSignals = signals! };
            var result = TestCaseValidator.ValidateRequiredSignals(testCase, out var error);

            Assert.Equal(expected, result);
            if (!expected)
                Assert.Equal(expectedError, error);
            else
                Assert.Null(error);
        }

        [Theory]
        [InlineData(null, false, "Timeout not specified.")]
        [InlineData(0.5d, false, "Timeout must be between 1 second and 10 minutes.")]
        [InlineData(1d, true, null)]
        [InlineData(60d, true, null)]
        [InlineData(600d, true, null)]
        [InlineData(601d, false, "Timeout must be between 1 second and 10 minutes.")]
        [InlineData(661d, false, "Timeout must be between 1 second and 10 minutes.")]
            [AllureFeature("Timeout Validation")]
        public void ValidateTimeout_Scenarios(double? seconds, bool expected, string? expectedError)
        {
            var testCase = new TestCase();
            TimeSpan? timeout = seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : (TimeSpan?)null;
            var result = TestCaseValidator.ValidateTimeout(testCase, timeout, out var error);
            Assert.Equal(expected, result);
            if (!expected)
                Assert.Equal(expectedError, error);
            else
                Assert.Null(error);
        }

        [Theory]
        [InlineData("5", 1d, 10d, true, null)]
        [InlineData("0", 1d, 10d, false, "Value 0 is below minimum 1.")]
        [InlineData("15", 1d, 10d, false, "Value 15 is above maximum 10.")]
        [InlineData("abc", 1d, 10d, true, null)]
        [InlineData("7", null, null, true, null)]
        [InlineData("0.1", 1d, null, false, "Value 0.1 is below minimum 1.")]
            [AllureFeature("Limits Validation")]
        public void ValidateLimits_Scenarios(string value, double? min, double? max, bool expected, string? expectedError)
        {
            var snapshot = new SignalSnapshot { Value = value };
            var result = TestCaseValidator.ValidateLimits(snapshot, min, max, out var error);
            Assert.Equal(expected, result);
            if (!expected && expectedError != null)
                Assert.Equal(expectedError, error);
            if (expected)
                Assert.Null(error);
        }
    }
}

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
        [Fact]
            [AllureFeature("Required Signals")]
        public void ValidateRequiredSignals_ReturnsFalse_WhenNoSignals()
        {
            var testCase = new TestCase { RequiredSignals = new List<SignalSnapshot>() };
            var result = TestCaseValidator.ValidateRequiredSignals(testCase, out var error);
            Assert.False(result);
            Assert.Equal("No required signals defined.", error);
        }

        [Fact]
            [AllureFeature("Required Signals")]
        public void ValidateRequiredSignals_ReturnsTrue_WhenSignalsPresent()
        {
            var testCase = new TestCase { RequiredSignals = new List<SignalSnapshot> { new SignalSnapshot { SignalName = "S1" } } };
            var result = TestCaseValidator.ValidateRequiredSignals(testCase, out var error);
            Assert.True(result);
            Assert.Null(error);
        }

        [Theory]
        [InlineData(null, false, "Timeout not specified.")]
        [InlineData(0.5d, false, "Timeout must be between 1 second and 10 minutes.")]
        [InlineData(601d, false, "Timeout must be between 1 second and 10 minutes.")]
        [InlineData(60d, true, null)]
            [AllureFeature("Timeout Validation")]
        public void ValidateTimeout_Works(double? seconds, bool expected, string? expectedError)
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
            [AllureFeature("Limits Validation")]
        public void ValidateLimits_Works(string value, double min, double max, bool expected, string? expectedError)
        {
            var snapshot = new SignalSnapshot { Value = value };
            var result = TestCaseValidator.ValidateLimits(snapshot, min, max, out var error);
            Assert.Equal(expected, result);
            if (!expected && expectedError != null)
                Assert.Equal(expectedError, error);
        }
    }
}

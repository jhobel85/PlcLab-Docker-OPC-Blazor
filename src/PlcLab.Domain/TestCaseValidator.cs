namespace PlcLab.Domain;

public static class TestCaseValidator
{
    public static bool ValidateRequiredSignals(TestCase testCase, out string? error)
    {
        if (testCase.RequiredSignals == null || testCase.RequiredSignals.Count == 0)
        {
            error = "No required signals defined.";
            return false;
        }
        error = null;
        return true;
    }

    public static bool ValidateTimeout(TestCase testCase, TimeSpan? timeout, out string? error)
    {
        // Example: check if timeout is set and within allowed range
        if (timeout == null)
        {
            error = "Timeout not specified.";
            return false;
        }
        if (timeout.Value.TotalSeconds < 1 || timeout.Value.TotalMinutes > 10)
        {
            error = "Timeout must be between 1 second and 10 minutes.";
            return false;
        }
        error = null;
        return true;
    }

    public static bool ValidateLimits(SignalSnapshot snapshot, double? min, double? max, out string? error)
    {
        if (snapshot.Value is IConvertible)
        {
            try
            {
                var val = Convert.ToDouble(snapshot.Value);
                if (min.HasValue && val < min.Value)
                {
                    error = $"Value {val} is below minimum {min.Value}.";
                    return false;
                }
                if (max.HasValue && val > max.Value)
                {
                    error = $"Value {val} is above maximum {max.Value}.";
                    return false;
                }
            }
            catch
            {
                // Not a numeric value
            }
        }
        error = null;
        return true;
    }
}

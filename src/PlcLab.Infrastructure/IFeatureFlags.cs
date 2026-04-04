namespace PlcLab.Infrastructure;

/// <summary>
/// Provides a way to query named feature flags backed by the application configuration.
/// Flag values are read from the "FeatureFlags" configuration section.
/// </summary>
/// <example>
/// appsettings.json:
/// <code>
/// {
///   "FeatureFlags": {
///     "ExperimentalResultsChart": true,
///     "DarkMode": false
///   }
/// }
/// </code>
/// Usage:
/// <code>
/// if (_features.IsEnabled("ExperimentalResultsChart")) { ... }
/// </code>
/// </example>
public interface IFeatureFlags
{
    /// <summary>
    /// Returns <c>true</c> when the named feature flag is set to <c>true</c>
    /// in the "FeatureFlags" configuration section; <c>false</c> otherwise.
    /// </summary>
    bool IsEnabled(string flagName);
}

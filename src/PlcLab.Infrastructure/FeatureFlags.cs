using Microsoft.Extensions.Configuration;

namespace PlcLab.Infrastructure;

/// <summary>
/// Configuration-backed implementation of <see cref="IFeatureFlags"/>.
/// Reads flag values from the "FeatureFlags" section of <see cref="IConfiguration"/>.
/// </summary>
public sealed class FeatureFlags : IFeatureFlags
{
    private readonly IConfiguration _config;

    public FeatureFlags(IConfiguration config)
    {
        _config = config;
    }

    /// <inheritdoc />
    public bool IsEnabled(string flagName) =>
        _config.GetValue<bool>($"FeatureFlags:{flagName}");
}

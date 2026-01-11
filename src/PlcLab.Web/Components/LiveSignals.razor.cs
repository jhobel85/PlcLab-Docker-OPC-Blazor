using Microsoft.AspNetCore.Components;
using Opc.Ua;
using Opc.Ua.Client;
using PlcLab.Infrastructure.Services;

namespace PlcLab.Web.Components;

public partial class LiveSignals : ComponentBase
{
    [Parameter] public Session? Session { get; set; }

    [Inject] protected ILiveSignalSubscriptionService LiveSignalSubscriptionService { get; set; } = default!;

    private Session? _attachedSession;
    private Subscription? Subscription;
    private bool IsSubscribed;

    private readonly Dictionary<string, (object? Value, DateTime Timestamp)> SignalValues = new();
    private readonly List<(string label, string path)> DemoSignals = new()
    {
        ("Process/State", "ns=6;s=Scalar_Simulation_Boolean"),
        ("Guid", "ns=6;s=Scalar_Simulation_Guid"),
        ("Duration (Double)", "ns=6;s=Scalar_Simulation_Duration"),
        ("Variant", "ns=6;s=Scalar_Simulation_Variant")
    };

    protected override async Task OnParametersSetAsync()
    {
        if (Session != _attachedSession)
        {
            await UnsubscribeAsync();
            _attachedSession = Session;
        }
    }

    private async Task SubscribeToSignals()
    {
        if (Session == null || IsSubscribed)
        {
            return;
        }


        try
        {
            Subscription = await LiveSignalSubscriptionService.SubscribeAsync(
                Session,
                DemoSignals,
                (label, value, timestamp) =>
                {
                    SignalValues[label] = (value, timestamp);
                    _ = InvokeAsync(StateHasChanged);
                });

            IsSubscribed = true;
            StateHasChanged();
        }
        catch (Exception)
        {
            await UnsubscribeAsync();
        }
    }

    private async Task UnsubscribeAsync()
    {
        await LiveSignalSubscriptionService.UnsubscribeAsync(Subscription);
        Subscription = null;

        IsSubscribed = false;
        SignalValues.Clear();
        StateHasChanged();
    }
}

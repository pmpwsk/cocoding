using Microsoft.AspNetCore.Components.Server.Circuits;

namespace cocoding;

/// <summary>
/// Injectable service for obtaining the current circuit and events for it.
/// </summary>
public class CircuitService : CircuitHandler
{
    public string? Id = null;

    public event Action? Connected;

    public event Action? Disconnected;
    
    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (Id != null)
            Console.WriteLine("A circuit was overwritten!");
        Id = circuit.Id;
        await base.OnCircuitOpenedAsync(circuit, cancellationToken);
        Connected?.Invoke();
    }

    public override async Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        await base.OnConnectionUpAsync(circuit, cancellationToken);
        Connected?.Invoke();
    }

    public override async Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        await base.OnConnectionDownAsync(circuit, cancellationToken);
        Disconnected?.Invoke();
    }

    public override async Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        await base.OnCircuitClosedAsync(circuit, cancellationToken);
        Disconnected?.Invoke();
        Id = null;
    }
}
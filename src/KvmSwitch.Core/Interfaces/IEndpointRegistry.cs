using KvmSwitch.Core.Models;

namespace KvmSwitch.Core.Interfaces;

/// <summary>
/// Manages the registry of discovered endpoints and their configurations.
/// </summary>
public interface IEndpointRegistry
{
    event EventHandler<Endpoint>? EndpointAdded;
    event EventHandler<Endpoint>? EndpointRemoved;
    event EventHandler<Endpoint>? EndpointUpdated;
    
    Task<IEnumerable<Endpoint>> GetAllEndpointsAsync();
    Task<Endpoint?> GetEndpointByIdAsync(string id);
    Task<Endpoint?> GetEndpointByHotkeyAsync(ModifierKeys modifiers, int keyCode);
    Task AddOrUpdateEndpointAsync(Endpoint endpoint);
    Task RemoveEndpointAsync(string id);
    Task SaveAsync();
    Task LoadAsync();
}


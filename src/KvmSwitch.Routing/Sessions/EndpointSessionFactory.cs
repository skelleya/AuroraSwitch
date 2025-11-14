using KvmSwitch.Core.Interfaces;
using KvmSwitch.Core.Models;
using Microsoft.Extensions.Logging;

namespace KvmSwitch.Routing.Sessions;

public static class EndpointSessionFactory
{
    public static IEndpointSession CreateSession(Endpoint endpoint, ILoggerFactory? loggerFactory = null)
    {
        var logger = loggerFactory?.CreateLogger<IEndpointSession>();
        
        return endpoint.ConnectionType switch
        {
            ConnectionType.UsbC => new UsbCEndpointSession(endpoint, 
                loggerFactory?.CreateLogger<UsbCEndpointSession>()),
            ConnectionType.Network => new UsbIpEndpointSession(endpoint, 
                loggerFactory?.CreateLogger<UsbIpEndpointSession>()),
            ConnectionType.Hybrid => new UsbCEndpointSession(endpoint, 
                loggerFactory?.CreateLogger<UsbCEndpointSession>()), // Prefer USB-C for hybrid
            _ => new UsbIpEndpointSession(endpoint, 
                loggerFactory?.CreateLogger<UsbIpEndpointSession>()) // Default to USB/IP
        };
    }
}


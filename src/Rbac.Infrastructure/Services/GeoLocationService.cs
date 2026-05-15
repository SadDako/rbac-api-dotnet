using System.Net;
using System.Net.Http.Json;
using Rbac.Application.Interfaces;
using Rbac.Application.Security;

namespace Rbac.Infrastructure.Services;

public sealed class GeoLocationService : IGeoLocationService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GeoLocationService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<GeoLocationResult> LocateAsync(string ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)
            || !IPAddress.TryParse(ipAddress, out var parsed)
            || IPAddress.IsLoopback(parsed)
            || IsPrivateAddress(parsed))
        {
            return new GeoLocationResult();
        }

        try
        {
            var client = _httpClientFactory.CreateClient("GeoLocation");
            var response = await client.GetFromJsonAsync<IpWhoResponse>($"{ipAddress}", cancellationToken);
            if (response is null || !response.Success)
            {
                return new GeoLocationResult();
            }

            return new GeoLocationResult
            {
                Country = response.Country ?? string.Empty,
                City = response.City ?? string.Empty,
                Latitude = response.Latitude,
                Longitude = response.Longitude
            };
        }
        catch
        {
            return new GeoLocationResult();
        }
    }

    private static bool IsPrivateAddress(IPAddress address)
    {
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 10
               || bytes[0] == 127
               || bytes[0] == 192 && bytes[1] == 168
               || bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31;
    }

    private sealed class IpWhoResponse
    {
        public bool Success { get; init; }
        public string? Country { get; init; }
        public string? City { get; init; }
        public double? Latitude { get; init; }
        public double? Longitude { get; init; }
    }
}

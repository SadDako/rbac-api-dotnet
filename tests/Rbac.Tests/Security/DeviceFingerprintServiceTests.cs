using FluentAssertions;
using Rbac.Application.Interfaces;
using Rbac.Application.Security;
using Rbac.Domain.Entities;
using Rbac.Infrastructure.Services;
using Xunit;

namespace Rbac.Tests.Security;

public sealed class DeviceFingerprintServiceTests
{
    [Fact]
    public async Task TrackDeviceAsync_ShouldPersistNewDeviceAndRecordSecurityEvent()
    {
        var repository = new InMemoryDeviceFingerprintRepository();
        var events = new RecordingSecurityEventService();
        var service = new DeviceFingerprintService(repository, events);
        var userId = Guid.NewGuid();
        var context = new SecurityRequestContext
        {
            ClientFingerprint = "client-fp",
            UserAgent = "Mozilla/5.0",
            AcceptLanguage = "pt-BR",
            IPAddress = "198.51.100.10",
            Timezone = "America/Cuiaba",
            Platform = "Windows",
            DeviceName = "Workstation",
            Browser = "Edge",
            RelevantHeaders = new Dictionary<string, string> { ["Accept"] = "application/json" }
        };

        var descriptor = service.CreateDescriptor(context, new GeoLocationResult { Country = "BR", City = "Cuiaba" });
        var result = await service.TrackDeviceAsync(userId, descriptor, CancellationToken.None);

        result.IsNewDevice.Should().BeTrue();
        result.IsSuspicious.Should().BeTrue();
        result.Record.Fingerprint.Should().HaveLength(64);
        repository.Records.Should().ContainSingle();
        events.Events.Should().ContainSingle(e => e.Type == SecurityEventType.NewDevice);
    }

    [Fact]
    public void CreateDescriptor_ShouldGenerateStableSha256Fingerprint()
    {
        var service = new DeviceFingerprintService(new InMemoryDeviceFingerprintRepository(), new RecordingSecurityEventService());
        var context = new SecurityRequestContext
        {
            ClientFingerprint = "abc",
            UserAgent = "Mozilla",
            AcceptLanguage = "en-US",
            Timezone = "UTC",
            Platform = "Linux",
            DeviceName = "Laptop",
            Browser = "Firefox",
            RelevantHeaders = new Dictionary<string, string> { ["Accept"] = "application/json" }
        };

        var first = service.CreateDescriptor(context, new GeoLocationResult()).Fingerprint;
        var second = service.CreateDescriptor(context, new GeoLocationResult()).Fingerprint;

        second.Should().Be(first);
        first.Should().HaveLength(64);
    }

    private sealed class InMemoryDeviceFingerprintRepository : IDeviceFingerprintRepository
    {
        public List<DeviceFingerprintRecord> Records { get; } = new();

        public Task<DeviceFingerprintRecord?> GetByUserAndFingerprintAsync(Guid userId, string fingerprint, CancellationToken cancellationToken)
        {
            return Task.FromResult(Records.FirstOrDefault(r => r.UserId == userId && r.Fingerprint == fingerprint));
        }

        public Task AddAsync(DeviceFingerprintRecord record, CancellationToken cancellationToken)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<DeviceFingerprintRecord>> ListByUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<DeviceFingerprintRecord>>(Records.Where(r => r.UserId == userId).ToArray());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingSecurityEventService : ISecurityEventService
    {
        public List<SecurityEvent> Events { get; } = new();

        public Task<RecordSecurityEventResult> RecordEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken)
        {
            Events.Add(securityEvent);
            return Task.FromResult(new RecordSecurityEventResult { Success = true, Message = "ok" });
        }
    }
}

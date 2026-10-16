using FSH.Modules.Multitenancy.Contracts.Dtos;

namespace FSH.Modules.Multitenancy.Provisioning;

public interface ITenantProvisioningService
{
    Task<TenantProvisioning> StartAsync(string tenantId, CancellationToken cancellationToken = default);

    Task<TenantProvisioning?> GetLatestAsync(string tenantId, CancellationToken cancellationToken = default);

    Task<TenantProvisioningStatusDto> GetStatusAsync(string tenantId, CancellationToken cancellationToken = default);

    Task EnsureCanActivateAsync(string tenantId, CancellationToken cancellationToken = default);

    Task<string> RetryAsync(string tenantId, CancellationToken cancellationToken = default);

    Task<bool> MarkRunningAsync(string tenantId, string correlationId, TenantProvisioningStepName step, CancellationToken cancellationToken = default);

    Task MarkStepCompletedAsync(string tenantId, string correlationId, TenantProvisioningStepName step, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(string tenantId, string correlationId, TenantProvisioningStepName step, string error, CancellationToken cancellationToken = default);

    Task MarkCompletedAsync(string tenantId, string correlationId, CancellationToken cancellationToken = default);
}
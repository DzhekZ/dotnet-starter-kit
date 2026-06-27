using FSH.Modules.Profile.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Profile.Events;

public sealed class ProfileEventHandlers(ILogger<ProfileEventHandlers> logger) :
    INotificationHandler<ProfileCreatedDomainEvent>,
    INotificationHandler<ProfilePriceChangedDomainEvent>,
    INotificationHandler<ProfileStockAdjustedDomainEvent>
{
    public ValueTask Handle(ProfileCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Handling ProductCreatedDomainEvent for ProductId: {ProductId}", notification.ProductId);
        }
        return default;
    }

    public ValueTask Handle(ProfilePriceChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Handling ProductPriceChangedDomainEvent for ProductId: {ProductId}", notification.ProductId);
        }
        return default;
    }

    public ValueTask Handle(ProfileStockAdjustedDomainEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Handling ProductStockAdjustedDomainEvent for ProductId: {ProductId}", notification.ProductId);
        }
        return default;
    }
}

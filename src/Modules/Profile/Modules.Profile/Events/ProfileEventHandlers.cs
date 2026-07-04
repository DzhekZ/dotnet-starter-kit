using FSH.Modules.Profile.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Profile.Events;

public sealed class ProfileEventHandlers(ILogger<ProfileEventHandlers> logger) :
    INotificationHandler<ProfileCreatedDomainEvent>,
    INotificationHandler<ProfileSubordinatesChangedDomainEvent>,
    INotificationHandler<ProfileStockAdjustedDomainEvent>
{
    public ValueTask Handle(ProfileCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Handling ProfileCreatedDomainEvent for ProfileId: {ProfileId}", notification.ProfileId);
        }
        return default;
    }

    public ValueTask Handle(ProfileSubordinatesChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Handling ProfilePriceChangedDomainEvent for ProfileId: {ProfileId}", notification.ProfileId);
        }
        return default;
    }

    public ValueTask Handle(ProfileStockAdjustedDomainEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Handling ProfileStockAdjustedDomainEvent for ProfileId: {ProfileId}", notification.ProfileId);
        }
        return default;
    }
}

namespace FSH.Modules.Webhooks.Contracts.Dtos;

public sealed record WebhookSubscriptionDto
{
    public Guid Id { get; init; }
    public string Url { get; init; } = default!;
    public string[] Events { get; init; } = [];
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

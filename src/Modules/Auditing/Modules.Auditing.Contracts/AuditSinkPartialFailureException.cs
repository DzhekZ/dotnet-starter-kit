namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// Raised by an <see cref="IAuditSink"/> that persisted part of a batch and could not persist the rest
/// (e.g. one tenant's database is unreachable or not migrated yet while the others are healthy).
/// Everything outside <see cref="FailedEnvelopes"/> is already durable, so the caller must retry or
/// dead-letter only the failed subset — re-sending the whole batch would insert the persisted rows twice.
/// </summary>
public sealed class AuditSinkPartialFailureException : Exception
{
    private static readonly IReadOnlyList<AuditEnvelope> NoEnvelopes = [];

    /// <summary>The envelopes that were NOT persisted. Never empty when raised by a sink.</summary>
    public IReadOnlyList<AuditEnvelope> FailedEnvelopes { get; }

    public AuditSinkPartialFailureException()
        : this("Part of the audit batch could not be written.")
    {
    }

    public AuditSinkPartialFailureException(string message)
        : base(message)
    {
        FailedEnvelopes = NoEnvelopes;
    }

    public AuditSinkPartialFailureException(string message, Exception innerException)
        : base(message, innerException)
    {
        FailedEnvelopes = NoEnvelopes;
    }

    public AuditSinkPartialFailureException(
        string message,
        IReadOnlyList<AuditEnvelope> failedEnvelopes,
        Exception innerException)
        : base(message, innerException)
    {
        ArgumentNullException.ThrowIfNull(failedEnvelopes);
        FailedEnvelopes = failedEnvelopes;
    }
}

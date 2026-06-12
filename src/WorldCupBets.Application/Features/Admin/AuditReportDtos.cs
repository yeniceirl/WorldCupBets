namespace WorldCupBets.Application.Features.Admin;

public static class AuditReportLabels
{
    public const string DerivedCurrentState = "Derived current-state report";
    public const string DerivedCurrentStateDescription = "Values are derived from current stored state, not an immutable accounting ledger.";
}

public sealed record AuditReportMetadataDto(
    string Label,
    string Description,
    bool IsDerivedFromCurrentState);

public sealed record AuditBalanceSummaryDto(
    AuditReportMetadataDto Metadata,
    IReadOnlyList<AuditBalanceSummaryRowDto> Rows);

public sealed record AuditBalanceSummaryRowDto(
    int UserId,
    string DisplayName,
    string Email,
    decimal AvailableBalanceCc,
    decimal PendingTotalCc,
    decimal DerivedTotalBalanceCc,
    decimal WonTotalCc,
    decimal LostTotalCc,
    decimal RescueDebtCc,
    int RescueCount);

public sealed record AuditUserSubledgerDto(
    AuditReportMetadataDto Metadata,
    AuditBalanceSummaryRowDto User,
    IReadOnlyList<AuditLedgerItemDto> Items);

public sealed record AuditLedgerItemDto(
    string SourceType,
    int SourceId,
    string Label,
    DateTime PlacedAtUtc,
    decimal StakeAmountCc,
    string Status,
    decimal CreditAmountCc,
    decimal LossAmountCc,
    decimal PendingAmountCc,
    string? PendingReason,
    IReadOnlyList<AuditLedgerMetadataItemDto> Metadata);

public sealed record AuditLedgerMetadataItemDto(string Label, string Value);

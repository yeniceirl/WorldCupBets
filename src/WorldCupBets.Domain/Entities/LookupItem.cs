using WorldCupBets.Domain.Common;

namespace WorldCupBets.Domain.Entities;

public sealed class LookupItem : Entity
{
    public string Category { get; private set; } = string.Empty;

    public string Key { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public string? Detail { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; } = true;
}

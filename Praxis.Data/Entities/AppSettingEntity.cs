using SQLite;

namespace Praxis.Data.Entities;

public sealed class AppSettingEntity
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

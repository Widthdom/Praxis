namespace Praxis.Models;

public sealed class AppSettingEntity
{
    [SQLite.PrimaryKey]
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

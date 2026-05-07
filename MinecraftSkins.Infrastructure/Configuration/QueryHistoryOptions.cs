namespace MinecraftSkins.Infrastructure.Configuration;

public class QueryHistoryOptions
{
    public const string SectionName = "QueryHistory";
    public int RetentionDays { get; set; } = 30;
    public int MaxRows { get; set; } = 10000;
}

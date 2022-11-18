namespace KindleCal.Models;

public class CalEvent
{
    private const int TitleLength = 26;
    private const int DescriptionLength = 30;

    public bool IsAllDay => StartDate == null;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? StartTime => StartDate?.ToString("h:mm tt");
    public string? EndTime => EndDate?.ToString("h:mm tt");
    public string? Title { get; set; }
    public string TitleShort => ((Title?.Length ?? 0) > TitleLength ? Title?.Substring(0, TitleLength - 1) + "…" : Title) ?? string.Empty;
    public string? Description { get; set; }
    public string DescriptionShort => ((Description?.Length ?? 0) > DescriptionLength ? Description?.Substring(0, DescriptionLength - 1) + "…" : Description) ?? string.Empty;
}

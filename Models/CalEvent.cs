namespace KindleCal.Models;

public class CalEvent
{
    public bool IsAllDay => StartDate == null;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? StartTime => StartDate?.ToString("h:mm tt");
    public string? EndTime => EndDate?.ToString("h:mm tt");
    public string? Title { get; set; }
    public string TitleShort => ((Title?.Length ?? 0) > 23 ? Title?.Substring(0, 22) + "…" : Title) ?? string.Empty;
    public string? Description { get; set; }
    public string DescriptionShort => ((Description?.Length ?? 0) > 20 ? Description?.Substring(0, 19) + "…" : Description) ?? string.Empty;
}

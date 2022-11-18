using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using KindleCal.Models;

namespace KindleCal.Services;

public class CalendarService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public CalendarService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClient = httpClientFactory.CreateClient();
        _config = config;
    }

    public async Task<List<CalEvent>> GetEventsAsync()
    {
        var calEvents = new List<CalEvent>();
        var urls = _config.GetValue<string>("CalendarUrls")?.Split(",") ?? new string[] { };

        foreach (var url in urls)
        {
            var calendarText = await GetCalendarTextAsync(url);

            if (string.IsNullOrWhiteSpace(calendarText))
                continue;

            var calendar = Calendar.Load(calendarText);

            calEvents.AddRange(GetCalEvents(calendar));
        }

        return calEvents
            .OrderByDescending(x => x.IsAllDay)
            .ThenBy(x => x.StartDate)
            .ThenBy(x => x.EndDate)
            .ToList();
    }

    private async Task<string> GetCalendarTextAsync(string url)
    {
        var response = await _httpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadAsStringAsync();

        return string.Empty;
    }

    private List<CalEvent> GetCalEvents(Calendar calendar)
    {
        var calEvents = new List<CalEvent>();
        var now = DateOnly.FromDateTime(DateTime.Now);

        foreach (var evt in calendar.Events)
        {
            var regularEvent = GetRegularCalEvent(evt, now);

            if (regularEvent != null)
            {
                calEvents.Add(regularEvent);
                continue;
            }

            var occurrenceEvent = GetOccurrenceEvent(evt, now);

            if (occurrenceEvent != null)
            {
                calEvents.Add(occurrenceEvent);
                continue;
            }
        }

        return calEvents;
    }

    private CalEvent? GetRegularCalEvent(CalendarEvent evt, DateOnly now)
    {
        var startDate = DateOnly.FromDateTime(evt.Start.Date);

        if (startDate != now)
            return null;

        if (evt.IsAllDay)
        {
            return new CalEvent
            {
                Title = evt.Summary,
                Description = evt.Location,
            };
        }
        else if (evt.Start is CalDateTime startDateTime && evt.End is CalDateTime endDateTime)
        {
            return new CalEvent
            {
                Title = evt.Summary,
                Description = evt.Location,
                StartDate = startDateTime.Value,
                EndDate = endDateTime.Value
            };
        }

        return null;
    }

    private CalEvent? GetOccurrenceEvent(CalendarEvent evt, DateOnly now)
    {
        if (!evt.RecurrenceRules.Any())
            return null;

        var occurrences = evt.GetOccurrences(now.ToDateTime(TimeOnly.MinValue), now.ToDateTime(TimeOnly.MaxValue));

        if (!occurrences.Any())
            return null;

        var occurrence = occurrences.First();

        if (evt.IsAllDay)
        {
            return new CalEvent
            {
                Title = evt.Summary,
                Description = evt.Location,
            };
        }
        else if (occurrence.Period.StartTime is CalDateTime startDateTime
                && occurrence.Period.EndTime is CalDateTime endDateTime)
        {
            return new CalEvent
            {
                Title = evt.Summary,
                Description = evt.Location,
                StartDate = startDateTime.Value,
                EndDate = endDateTime.Value
            };
        }

        return null;
    }
}
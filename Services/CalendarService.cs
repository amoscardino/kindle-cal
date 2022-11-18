using Ical.Net;
using Ical.Net.DataTypes;
using KindleCal.Models;
using Microsoft.Extensions.Configuration;

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
        var urls = _config.GetSection("CalendarUrls").Get<string[]>() ?? new string[] { };

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
            var startDate = DateOnly.FromDateTime(evt.Start.Date);

            if (startDate != now)
                continue;

            if (evt.IsAllDay)
            {
                calEvents.Add(new CalEvent
                {
                    Title = evt.Summary,
                    Description = evt.Location,
                });
            }
            else if (evt.Start is CalDateTime startDateTime && evt.End is CalDateTime endDateTime)
            {
                calEvents.Add(new CalEvent
                {
                    Title = evt.Summary,
                    Description = evt.Location,
                    StartDate = startDateTime.Value,
                    EndDate = endDateTime.Value
                });
            }
        }

        return calEvents;
    }
}
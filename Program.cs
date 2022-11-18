using KindleCal.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddTransient<CalendarService>();
builder.Services.AddTransient<ImageService>();

var app = builder.Build();

app.UseStaticFiles();

app.MapGet("/", () => "Hello!");

app.MapGet("/image", async (string key, IConfiguration config, CalendarService calendarService, ImageService imageService) =>
{
    var accessKey = config.GetValue<string>("AccessKey");

    if (key != accessKey)
        return Results.NotFound();

    var events = await calendarService.GetEventsAsync();
    var imageBytes = await imageService.CreateImageAsync(events);

    return Results.Bytes(imageBytes, "image/png");
});

app.Run();

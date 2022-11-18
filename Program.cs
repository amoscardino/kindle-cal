using KindleCal.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddTransient<CalendarService>();
builder.Services.AddTransient<ImageService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapGet("/", () => "Hello!");

app.MapGet("/image", async (CalendarService calendarService, ImageService imageService) =>
{
    var events = await calendarService.GetEventsAsync();

    using var imageStream = new MemoryStream();
    await imageService.CreateImageAsync(events, imageStream);

    imageStream.Position = 0;

    return Results.Bytes(imageStream.ToArray(), "image/png");
});

app.Run();

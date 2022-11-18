using KindleCal.Models;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KindleCal.Services;

public class ImageService
{
    private const int Unit = 1;
    private const int Margin = 10 * Unit;
    private const int MarginHalf = (int)(Margin / 2d);
    private const int MarginDouble = Margin * 2;
    private const int Width = 600 * Unit;
    private const int Height = 800 * Unit;
    private const int HeaderHeight = 150 * Unit;
    private const int EventCount = 5;
    private const int EventHeight = (int)((Height - HeaderHeight) / EventCount);
    private const int EventTimeWidth = EventHeight + Margin;

    private readonly FontCollection _fontCollection;
    private readonly IBrush _brush;
    private readonly DrawingOptions _lineDrawingOptions;

    public ImageService(IWebHostEnvironment webHostEnvironment)
    {
        var fontsFolder = $"{webHostEnvironment.WebRootPath}/fonts";

        _fontCollection = new FontCollection();
        _fontCollection.Add($"{fontsFolder}/Ubuntu-Regular.ttf");
        _fontCollection.Add($"{fontsFolder}/Ubuntu-Italic.ttf");
        _fontCollection.Add($"{fontsFolder}/Ubuntu-Bold.ttf");

        _brush = Brushes.Solid(Color.Black);

        _lineDrawingOptions = new DrawingOptions
        {
            GraphicsOptions = new GraphicsOptions
            {
                Antialias = false
            }
        };
    }

    public async Task<byte[]> CreateImageAsync(List<CalEvent> events)
    {
        using var image = new Image<L8>(Width, Height, Color.White.ToPixel<L8>());

        image.Mutate(imageContext => DrawDateHeader(imageContext));
        image.Mutate(imageContext => DrawEvents(imageContext, events));

        using var memoryStream = new MemoryStream();
        await image.SaveAsPngAsync(memoryStream);
        memoryStream.Position = 0;

        return memoryStream.ToArray();
    }

    private void DrawDateHeader(IImageProcessingContext imageContext)
    {
        var now = DateTime.Now;

        var dayOfWeek = now.DayOfWeek.ToString();
        var dayOfWeekFont = _fontCollection.Get("Ubuntu").CreateFont(54 * Unit, FontStyle.Bold);
        var dayOfWeekOptions = new TextOptions(dayOfWeekFont)
        {
            Origin = new Point { X = Margin, Y = Margin }
        };
        imageContext.DrawText(dayOfWeekOptions, dayOfWeek, _brush);

        var month = now.ToString("MMMM");
        var monthFont = _fontCollection.Get("Ubuntu").CreateFont(54 * Unit);
        var monthOptions = new TextOptions(monthFont);
        var monthRect = TextMeasurer.Measure(month, monthOptions);
        monthOptions.Origin = new Point
        {
            X = Margin,
            Y = HeaderHeight - (int)monthRect.Height - Margin
        };
        imageContext.DrawText(monthOptions, month, _brush);

        var date = now.Day.ToString();
        var dateFont = _fontCollection.Get("Ubuntu").CreateFont(124 * Unit, FontStyle.Bold);
        var dateOptions = new TextOptions(dateFont);
        var dateRect = TextMeasurer.Measure(date, dateOptions);
        dateOptions.Origin = new Point
        {
            X = Width - (int)dateRect.Width - MarginDouble,
            Y = (int)(((float)HeaderHeight / 2) - (dateRect.Height / 2))
        };
        imageContext.DrawText(dateOptions, date, _brush);

        var lineStart = new Point
        {
            X = -1 * Unit,
            Y = HeaderHeight - (2 * Unit)
        };
        var lineEnd = new Point
        {
            X = Width + (1 * Unit),
            Y = HeaderHeight - (2 * Unit)
        };
        imageContext.DrawLines(_lineDrawingOptions, _brush, 4 * Unit, lineStart, lineEnd);
    }

    private void DrawEvents(IImageProcessingContext imageContext, List<CalEvent> events)
    {
        if (!events.Any())
        {
            var noEventsText = "Nothing for today!";
            var noEventsFont = _fontCollection.Get("Ubuntu").CreateFont(32 * Unit, FontStyle.Italic);
            var noEventsOptions = new TextOptions(noEventsFont);
            var noEventsRect = TextMeasurer.Measure(noEventsText, noEventsOptions);

            noEventsOptions.Origin = new Point
            {
                X = (int)(Width / 2d) - (int)(noEventsRect.Width / 2d),
                Y = HeaderHeight + (int)((Height - HeaderHeight) / 2d) - (int)(noEventsRect.Height / 2d)
            };
            imageContext.DrawText(noEventsOptions, noEventsText, _brush);
        }

        var origin = new Point { X = 0, Y = HeaderHeight };

        for (int i = 0; i < Math.Min(events.Count, EventCount); i++)
        {
            DrawEvent(imageContext, origin, events[i]);

            if (i != Math.Min(events.Count, EventCount) - 1)
                DrawEventDivider(imageContext, origin);

            origin.Y += EventHeight;
        }
    }

    private void DrawEvent(IImageProcessingContext imageContext, Point origin, CalEvent calEvent)
    {
        if (!calEvent.IsAllDay)
        {
            var startTimeFont = _fontCollection.Get("Ubuntu").CreateFont(26 * Unit, FontStyle.Bold);
            var startTimeOptions = new TextOptions(startTimeFont);
            var startTimeRect = TextMeasurer.Measure(calEvent.StartTime!, startTimeOptions);

            startTimeOptions.Origin = new Point
            {
                X = origin.X + EventTimeWidth - (int)startTimeRect.Width,
                Y = origin.Y + (int)((float)EventHeight / 2) - (int)startTimeRect.Height - (2 * Unit)
            };
            imageContext.DrawText(startTimeOptions, calEvent.StartTime!, _brush);

            var endTimeFont = _fontCollection.Get("Ubuntu").CreateFont(25 * Unit);
            var endTimeOptions = new TextOptions(endTimeFont);
            var endTimeRect = TextMeasurer.Measure(calEvent.EndTime!, endTimeOptions);

            endTimeOptions.Origin = new Point
            {
                X = origin.X + EventTimeWidth - (int)endTimeRect.Width,
                Y = origin.Y + (int)((float)EventHeight / 2) + (2 * Unit)
            };
            imageContext.DrawText(endTimeOptions, calEvent.EndTime!, _brush);
        }

        var lineStart = new Point
        {
            X = origin.X + EventTimeWidth + MarginDouble,
            Y = origin.Y + MarginDouble
        };
        var lineEnd = new Point
        {
            X = origin.X + EventTimeWidth + MarginDouble,
            Y = origin.Y + EventHeight - MarginDouble
        };
        imageContext.DrawLines(_lineDrawingOptions, _brush, 1 * Unit, lineStart, lineEnd);

        var titleFont = _fontCollection.Get("Ubuntu").CreateFont(32 * Unit);
        var titleOptions = new TextOptions(titleFont);
        var titleRect = TextMeasurer.Measure(calEvent.TitleShort, titleOptions);

        if (string.IsNullOrWhiteSpace(calEvent.Description))
        {
            titleOptions.Origin = new Point
            {
                X = origin.X + EventTimeWidth + MarginDouble + MarginDouble,
                Y = origin.Y + (int)((double)EventHeight / 2) - (int)(titleRect.Height / 2)
            };
            imageContext.DrawText(titleOptions, calEvent.TitleShort, _brush);
        }
        else
        {
            titleOptions.Origin = new Point
            {
                X = origin.X + EventTimeWidth + MarginDouble + MarginDouble,
                Y = origin.Y + (int)((double)EventHeight / 2) - (int)(titleRect.Height)
            };
            imageContext.DrawText(titleOptions, calEvent.TitleShort, _brush);

            var descriptionFont = _fontCollection.Get("Ubuntu").CreateFont(24 * Unit, FontStyle.Italic);
            var descriptionOptions = new TextOptions(descriptionFont);
            var descriptionRect = TextMeasurer.Measure(calEvent.DescriptionShort, descriptionOptions);

            descriptionOptions.Origin = new Point
            {
                X = origin.X + EventTimeWidth + MarginDouble + MarginDouble,
                Y = origin.Y + (int)((double)EventHeight / 2) + MarginHalf
            };
            imageContext.DrawText(descriptionOptions, calEvent.DescriptionShort, _brush);
        }
    }

    private void DrawEventDivider(IImageProcessingContext imageContext, Point origin)
    {
        var lineStart = new Point
        {
            X = Margin,
            Y = origin.Y + EventHeight - (1 * Unit)
        };
        var lineEnd = new Point
        {
            X = Width - Margin,
            Y = origin.Y + EventHeight - (1 * Unit)
        };
        imageContext.DrawLines(_lineDrawingOptions, _brush, 2 * Unit, lineStart, lineEnd);
    }
}
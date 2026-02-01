using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace CleanOrgaCleaner.Views;

public partial class ImageAnnotationPage : ContentPage
{
    public enum DrawTool { Freehand, Circle, Arrow }

    private SKBitmap? _originalBitmap;
    private readonly List<DrawingElement> _elements = new();
    private DrawingElement? _currentElement;
    private DrawTool _currentTool = DrawTool.Freehand;
    private readonly SKColor _drawColor = SKColors.Red;
    private const float StrokeWidth = 6f;

    private float _scale = 1f;
    private float _offsetX = 0f;
    private float _offsetY = 0f;

    public byte[]? AnnotatedImageBytes { get; private set; }
    public bool WasSaved { get; private set; }

    public ImageAnnotationPage(byte[] imageBytes)
    {
        System.Diagnostics.Debug.WriteLine("[ANNOTATION PAGE] Constructor start");
        InitializeComponent();
        System.Diagnostics.Debug.WriteLine("[ANNOTATION PAGE] InitializeComponent done");
        LoadImage(imageBytes);
        System.Diagnostics.Debug.WriteLine("[ANNOTATION PAGE] LoadImage done");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("[ANNOTATION PAGE] OnAppearing");
    }

    private void LoadImage(byte[] imageBytes)
    {
        System.Diagnostics.Debug.WriteLine($"[ANNOTATION PAGE] LoadImage: {imageBytes.Length} bytes");
        _originalBitmap = SKBitmap.Decode(imageBytes);
        System.Diagnostics.Debug.WriteLine($"[ANNOTATION PAGE] Bitmap decoded: {_originalBitmap?.Width}x{_originalBitmap?.Height}");
        BackgroundImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        CalculateTransform();
    }

    private void CalculateTransform()
    {
        if (_originalBitmap == null || CanvasContainer.Width <= 0 || CanvasContainer.Height <= 0) return;

        var containerWidth = (float)CanvasContainer.Width;
        var containerHeight = (float)CanvasContainer.Height;

        float scaleX = containerWidth / _originalBitmap.Width;
        float scaleY = containerHeight / _originalBitmap.Height;
        _scale = Math.Min(scaleX, scaleY);

        float scaledWidth = _originalBitmap.Width * _scale;
        float scaledHeight = _originalBitmap.Height * _scale;

        _offsetX = (containerWidth - scaledWidth) / 2f;
        _offsetY = (containerHeight - scaledHeight) / 2f;
    }

    private SKPoint ScreenToImage(SKPoint screenPoint)
    {
        return new SKPoint(
            (screenPoint.X - _offsetX) / _scale,
            (screenPoint.Y - _offsetY) / _scale
        );
    }

    private void OnToolSelected(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            _currentTool = btn.ClassId switch
            {
                "circle" => DrawTool.Circle,
                "arrow" => DrawTool.Arrow,
                _ => DrawTool.Freehand
            };

            // Update button visuals
            BtnFreehand.BackgroundColor = _currentTool == DrawTool.Freehand ? Color.FromArgb("#E91E63") : Color.FromArgb("#555");
            BtnCircle.BackgroundColor = _currentTool == DrawTool.Circle ? Color.FromArgb("#E91E63") : Color.FromArgb("#555");
            BtnArrow.BackgroundColor = _currentTool == DrawTool.Arrow ? Color.FromArgb("#E91E63") : Color.FromArgb("#555");
        }
    }

    private void OnUndoClicked(object sender, EventArgs e)
    {
        if (_elements.Count > 0)
        {
            _elements.RemoveAt(_elements.Count - 1);
            CanvasView.InvalidateSurface();
        }
    }

    private void OnTouch(object sender, SKTouchEventArgs e)
    {
        var imagePoint = ScreenToImage(e.Location);

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                _currentElement = _currentTool switch
                {
                    DrawTool.Circle => new CircleElement { Center = imagePoint, Color = _drawColor },
                    DrawTool.Arrow => new ArrowElement { Start = imagePoint, End = imagePoint, Color = _drawColor },
                    _ => new FreehandElement { Color = _drawColor }
                };

                if (_currentElement is FreehandElement freehand)
                    freehand.Points.Add(imagePoint);

                e.Handled = true;
                break;

            case SKTouchAction.Moved:
                if (_currentElement != null)
                {
                    switch (_currentElement)
                    {
                        case FreehandElement fh:
                            fh.Points.Add(imagePoint);
                            break;
                        case CircleElement circle:
                            circle.Radius = SKPoint.Distance(circle.Center, imagePoint);
                            break;
                        case ArrowElement arrow:
                            arrow.End = imagePoint;
                            break;
                    }
                    CanvasView.InvalidateSurface();
                    e.Handled = true;
                }
                break;

            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                if (_currentElement != null)
                {
                    // Only add if element has meaningful size
                    bool shouldAdd = _currentElement switch
                    {
                        FreehandElement fh => fh.Points.Count > 1,
                        CircleElement c => c.Radius > 5,
                        ArrowElement a => SKPoint.Distance(a.Start, a.End) > 10,
                        _ => false
                    };

                    if (shouldAdd)
                        _elements.Add(_currentElement);

                    _currentElement = null;
                    CanvasView.InvalidateSurface();
                    e.Handled = true;
                }
                break;
        }
    }

    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        // Apply transform to match image position
        canvas.Save();
        canvas.Translate(_offsetX, _offsetY);
        canvas.Scale(_scale);

        using var paint = new SKPaint
        {
            Color = _drawColor,
            StrokeWidth = StrokeWidth,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        // Draw all elements
        foreach (var element in _elements)
            DrawElement(canvas, element, paint);

        // Draw current element being created
        if (_currentElement != null)
            DrawElement(canvas, _currentElement, paint);

        canvas.Restore();
    }

    private void DrawElement(SKCanvas canvas, DrawingElement element, SKPaint paint)
    {
        paint.Color = element.Color;

        switch (element)
        {
            case FreehandElement freehand when freehand.Points.Count > 1:
                using (var path = new SKPath())
                {
                    path.MoveTo(freehand.Points[0]);
                    for (int i = 1; i < freehand.Points.Count; i++)
                        path.LineTo(freehand.Points[i]);
                    canvas.DrawPath(path, paint);
                }
                break;

            case CircleElement circle when circle.Radius > 0:
                canvas.DrawCircle(circle.Center, circle.Radius, paint);
                break;

            case ArrowElement arrow:
                DrawArrow(canvas, arrow.Start, arrow.End, paint);
                break;
        }
    }

    private void DrawArrow(SKCanvas canvas, SKPoint start, SKPoint end, SKPaint paint)
    {
        // Draw line
        canvas.DrawLine(start, end, paint);

        // Draw arrowhead
        float angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);
        float arrowSize = 25f;
        float arrowAngle = 0.5f; // ~30 degrees

        var p1 = new SKPoint(
            end.X - arrowSize * (float)Math.Cos(angle - arrowAngle),
            end.Y - arrowSize * (float)Math.Sin(angle - arrowAngle));
        var p2 = new SKPoint(
            end.X - arrowSize * (float)Math.Cos(angle + arrowAngle),
            end.Y - arrowSize * (float)Math.Sin(angle + arrowAngle));

        canvas.DrawLine(end, p1, paint);
        canvas.DrawLine(end, p2, paint);
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        WasSaved = false;
        await Navigation.PopModalAsync();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_originalBitmap == null)
        {
            await Navigation.PopModalAsync();
            return;
        }

        try
        {
            // Create output bitmap
            using var outputBitmap = _originalBitmap.Copy();
            using var canvas = new SKCanvas(outputBitmap);

            using var paint = new SKPaint
            {
                Color = _drawColor,
                StrokeWidth = StrokeWidth,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round
            };

            // Draw all elements onto the bitmap
            foreach (var element in _elements)
                DrawElement(canvas, element, paint);

            // Encode to JPEG
            using var image = SKImage.FromBitmap(outputBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
            AnnotatedImageBytes = data.ToArray();

            WasSaved = true;
            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save annotation error: {ex}");
            await DisplayAlert("Fehler", "Bild konnte nicht gespeichert werden", "OK");
        }
    }

    // Drawing element classes
    private abstract class DrawingElement
    {
        public SKColor Color { get; set; }
    }

    private class FreehandElement : DrawingElement
    {
        public List<SKPoint> Points { get; } = new();
    }

    private class CircleElement : DrawingElement
    {
        public SKPoint Center { get; set; }
        public float Radius { get; set; }
    }

    private class ArrowElement : DrawingElement
    {
        public SKPoint Start { get; set; }
        public SKPoint End { get; set; }
    }
}

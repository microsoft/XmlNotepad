using System.Drawing;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace XmlNotepad.Services;

internal sealed class MainWindowPlacement
{
    private readonly AppWindow _window;
    private readonly Settings _settings;
    private Rectangle _normalBounds;
    private bool _wasMaximized;

    public MainWindowPlacement(AppWindow window, Settings settings)
    {
        _window = window;
        _settings = settings;
        if (_settings["WindowBounds"] is Rectangle { Width: > 0, Height: > 0 } saved)
        {
            var display = DisplayArea.GetFromRect(new RectInt32(saved.X, saved.Y, saved.Width, saved.Height),
                DisplayAreaFallback.Nearest);
            if (display is not null)
            {
                var work = display.WorkArea;
                var origin = display.OuterBounds;
                var width = Math.Min(saved.Width, work.Width);
                var height = Math.Min(saved.Height, work.Height);
                var left = origin.X + work.X;
                var top = origin.Y + work.Y;
                _window.MoveAndResize(new RectInt32(
                    Math.Clamp(saved.X, left, left + work.Width - width),
                    Math.Clamp(saved.Y, top, top + work.Height - height), width, height));
            }
        }

        RecordCurrentPlacement();
        _window.Changed += Window_Changed;
    }

    public void RestorePresentation()
    {
        if (_settings.GetBoolean("WindowMaximized") && _window.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }
    }

    public void Save()
    {
        RecordCurrentPlacement();
        _window.Changed -= Window_Changed;
        if (!_normalBounds.IsEmpty)
        {
            _settings["WindowBounds"] = _normalBounds;
        }
        _settings["WindowMaximized"] = _wasMaximized;
    }

    private void Window_Changed(AppWindow sender, AppWindowChangedEventArgs args) => RecordCurrentPlacement();

    private void RecordCurrentPlacement()
    {
        if (_window.Presenter is not OverlappedPresenter presenter)
        {
            return;
        }

        if (presenter.State == OverlappedPresenterState.Restored)
        {
            var position = _window.Position;
            var size = _window.Size;
            _normalBounds = new Rectangle(position.X, position.Y, size.Width, size.Height);
            _wasMaximized = false;
        }
        else if (presenter.State == OverlappedPresenterState.Maximized)
        {
            _wasMaximized = true;
        }
    }
}

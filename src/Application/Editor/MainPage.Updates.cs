using System.Net.Http;
using System.Xml;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using XmlNotepad.Services;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private readonly CancellationTokenSource _updateLifetime = new();
    private DispatcherQueueTimer? _updateTimer;
    private Uri? _updateLink;
    private bool _checkingForUpdates;
    private bool _showUpdateResult;

    private void StartUpdateChecks()
    {
        _updateTimer = DispatcherQueue.CreateTimer();
        _updateTimer.Tick += AutomaticUpdateCheck;
        Unloaded += (_, _) =>
        {
            _updateTimer.Stop();
            _updateLifetime.Cancel();
        };
        UpdateCheckSchedule();
    }

    private void UpdateCheckSchedule()
    {
        var settings = App.Settings;
        var hideUpdateUi = settings.GetBoolean("DisableUpdateUI");
        CheckForUpdatesItem.Visibility = hideUpdateUi ? Visibility.Collapsed : Visibility.Visible;
        _updateTimer?.Stop();
        if (!hideUpdateUi && settings.GetBoolean("UpdateEnabled") && _updateTimer is { } timer)
        {
            var interval = settings.GetTimeSpan("UpdateFrequency", TimeSpan.FromDays(1));
            timer.Interval = TimeSpan.FromTicks(Math.Clamp(interval.Ticks,
                ReleaseUpdateService.MinimumInterval.Ticks, TimeSpan.FromHours(1).Ticks));
            timer.Start();
            _ = CheckForUpdatesAsync(manual: false);
        }
    }

    private async void AutomaticUpdateCheck(DispatcherQueueTimer sender, object args) =>
        await CheckForUpdatesAsync(manual: false);

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e) =>
        await CheckForUpdatesAsync(manual: true);

    private async Task CheckForUpdatesAsync(bool manual)
    {
        var settings = App.Settings;
        if (_updateLifetime.IsCancellationRequested || settings.GetBoolean("DisableUpdateUI") ||
            !manual && !settings.GetBoolean("UpdateEnabled"))
        {
            return;
        }
        var interval = settings.GetTimeSpan("UpdateFrequency", TimeSpan.FromDays(1));
        var now = DateTime.UtcNow;
        var previous = settings.GetDateTime("LastUpdateCheck", DateTime.MinValue).ToUniversalTime();
        if (!manual && (now >= previous && now - previous < interval))
        {
            return;
        }

        if (manual)
        {
            _showUpdateResult = true;
            ShowUpdateNotice("Checking for updates…");
        }
        if (_checkingForUpdates)
        {
            return;
        }

        _checkingForUpdates = true;
        var configuredLocation = settings.GetString("UpdateLocation");
        try
        {
            var feed = string.IsNullOrWhiteSpace(configuredLocation) ? Settings.DefaultUpdateLocation : configuredLocation;
            if (!ReleaseUpdateService.TryGetLocation(feed, out var location))
            {
                throw new InvalidOperationException("Choose a web address or XML file in Options.");
            }

            settings["LastUpdateCheck"] = now;
            App.SettingsService.Save();
            var manifest = await ReleaseUpdateService.CheckAsync(location, _updateLifetime.Token);
            if (_updateLifetime.IsCancellationRequested || settings.GetString("UpdateLocation") != configuredLocation)
            {
                return;
            }

            if (manifest.MinimumInterval is { } minimum && minimum > interval)
            {
                settings["UpdateFrequency"] = minimum;
            }
            if (location.Scheme == Uri.UriSchemeHttps && manifest.FeedLocation?.Scheme == Uri.UriSchemeHttps)
            {
                settings["UpdateLocation"] = manifest.FeedLocation.AbsoluteUri;
            }
            App.SettingsService.Save();

            if (manifest.LatestVersion > ReleaseUpdateService.InstalledVersion)
            {
                _updateLink = manifest.Installer ?? manifest.History ?? ReleaseUpdateService.Releases;
                ShowUpdateNotice($"XML Notepad {manifest.LatestVersion} is available",
                    $"Installed version {ReleaseUpdateService.InstalledVersion}", showLink: true);
            }
            else if (_showUpdateResult)
            {
                ShowUpdateNotice("XML Notepad is up to date", $"Installed version {ReleaseUpdateService.InstalledVersion}");
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or XmlException or
            UnauthorizedAccessException or OperationCanceledException or InvalidOperationException)
        {
            if (_showUpdateResult && !_updateLifetime.IsCancellationRequested)
            {
                ShowUpdateNotice("Could not check for updates", exception.Message, InfoBarSeverity.Error);
            }
        }
        finally
        {
            _checkingForUpdates = false;
            _showUpdateResult = false;
            if (settings.GetString("UpdateLocation") != configuredLocation)
            {
                _ = CheckForUpdatesAsync(manual: false);
            }
        }
    }

    private void ShowUpdateNotice(string title, string message = "", InfoBarSeverity severity = InfoBarSeverity.Informational,
        bool showLink = false)
    {
        UpdateNotice.Title = title;
        UpdateNotice.Message = message;
        UpdateNotice.Severity = severity;
        OpenUpdateButton.Visibility = showLink ? Visibility.Visible : Visibility.Collapsed;
        UpdateNotice.IsOpen = true;
    }

    private async void OpenUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (_updateLink is not { } link)
        {
            return;
        }
        try
        {
            if (await Launcher.LaunchUriAsync(link))
            {
                return;
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or COMException)
        {
            ShowUpdateNotice("Could not open the update page", exception.Message, InfoBarSeverity.Error);
            return;
        }
        ShowUpdateNotice("Could not open the update page", link.AbsoluteUri, InfoBarSeverity.Error);
    }
}

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using XmlNotepad.Services;
using VirtualKey = Windows.System.VirtualKey;

namespace XmlNotepad;

public sealed partial class FindReplaceBar : UserControl
{
    private readonly DispatcherQueueTimer _queryTimer;
    private SearchFilter? _incrementalFilter;

    public event EventHandler<FindReplaceRequestEventArgs>? FindRequested;
    public event EventHandler<FindReplaceRequestEventArgs>? ReplaceRequested;
    public event EventHandler<FindReplaceRequestEventArgs>? ReplaceAllRequested;
    public event EventHandler? QueryChanged;
    public event EventHandler? CloseRequested;

    public FindReplaceBar()
    {
        _queryTimer = DispatcherQueue.CreateTimer();
        _queryTimer.Interval = TimeSpan.FromMilliseconds(150);
        _queryTimer.IsRepeating = false;
        _queryTimer.Tick += (_, _) => RequestFind(backwards: ReplaceRow.Visibility == Visibility.Visible && _replaceBackwardsOption.IsChecked, preview: true);
        InitializeComponent();
        var settings = App.SettingsService.Settings;
        _matchCaseOption = CreateOption("Match case", settings.GetBoolean("SearchMatchCase"));
        _wholeWordOption = CreateOption("Whole word", settings.GetBoolean("SearchWholeWord"));
        _xpathOption = CreateOption("XPath", settings.GetBoolean("SearchXPath"));
        _replaceBackwardsOption = CreateOption("Replace backwards", settings.GetBoolean("SearchBackwards"));
        _regexOption = CreateOption("Regular expression", settings.GetBoolean("SearchRegex") && !_xpathOption.IsChecked);
        _selectedFilter = Math.Clamp(settings.GetInteger("SearchFilter"), 0, FilterNames.Length - 1);
        BuildOptionsFlyout();
        UpdateOptionPresentation();
        UpdateSearchButtons();
    }

    public string SearchText => FindTextBox.Text;

    public bool IsEditingText =>
        FindInput?.FocusState is not (null or FocusState.Unfocused) || ReplaceTextBox.FocusState != FocusState.Unfocused;

    private TextBox? FindInput => MainPage.FindDescendant<TextBox>(FindTextBox);

    internal bool FocusInput() => TryFocusFindInput();

    public void ShowFind(bool showReplace)
    {
        var modeChanged = _incrementalFilter is not null;
        _incrementalFilter = null;
        Visibility = Visibility.Visible;
        SetExpanded(showReplace);
        if (modeChanged)
        {
            SearchQueryChanged();
        }
        FocusFindText();
    }

    internal void ShowIncremental(SearchFilter filter)
    {
        _incrementalFilter = filter;
        Visibility = Visibility.Visible;
        SetExpanded(false);
        SearchQueryChanged();
        FocusFindText();
    }

    public void HideFind()
    {
        _queryTimer.Stop();
        _optionsFlyout.Hide();
        FindTextBox.IsSuggestionListOpen = false;
        Visibility = Visibility.Collapsed;
        ShowStatus(string.Empty);
    }

    public void ShowStatus(string message)
    {
        StatusText.Text = message;
        StatusText.Visibility = message.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        UpdateSearchDetails();
    }

    private void UpdateSearchDetails() =>
        SearchDetails.Visibility = StatusText.Visibility == Visibility.Visible || SearchModeText.Visibility == Visibility.Visible
            ? Visibility.Visible : Visibility.Collapsed;

    public void FindNext() => RequestFind(backwards: false);

    public void FindPrevious() => RequestFind(backwards: true);

    private void ExpandButton_Click(object sender, RoutedEventArgs e)
    {
        if (_incrementalFilter is not null)
        {
            ShowFind(showReplace: true);
            return;
        }
        SetExpanded(ReplaceRow.Visibility != Visibility.Visible);
    }

    private void SetExpanded(bool expanded)
    {
        if (!expanded && ReplaceTextBox.FocusState != FocusState.Unfocused)
        {
            FocusFindText();
        }
        ReplaceRow.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
        ExpandIcon.Glyph = expanded ? "\uE70E" : "\uE70D";
        var label = expanded ? "Hide replace" : "Show replace";
        ToolTipService.SetToolTip(ExpandButton, label);
        AutomationProperties.SetName(ExpandButton, label);
        UpdateOptionPresentation();
    }

    private void FocusFindText()
    {
        FindTextBox.ApplyTemplate();
        if (!TryFocusFindInput())
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (Visibility == Visibility.Visible)
                {
                    TryFocusFindInput();
                }
            });
        }
    }

    private bool TryFocusFindInput()
    {
        if (FindInput is not { } input || !input.Focus(FocusState.Programmatic))
        {
            return false;
        }
        input.SelectAll();
        return true;
    }

    private void PreviousButton_Click(object sender, RoutedEventArgs e) => FindPrevious();

    private void NextButton_Click(object sender, RoutedEventArgs e) => FindNext();

    private void OptionsButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshNamespaceFlyout();
        RefreshHistoryFlyouts();
        _optionsFlyout.ShowAt(OptionsButton);
    }

    private void SearchBar_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Escape:
                e.Handled = true;
                if (FindTextBox.IsSuggestionListOpen)
                {
                    FindTextBox.IsSuggestionListOpen = false;
                }
                else
                {
                    CloseRequested?.Invoke(this, EventArgs.Empty);
                }
                break;
            case VirtualKey.Enter when ReplaceTextBox.FocusState != FocusState.Unfocused:
                e.Handled = true;
                RequestReplace(replaceAll: false);
                break;
        }
    }

    private void FindTextBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs e)
    {
        if (IsLoaded)
        {
            if (e.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                UpdateSuggestions();
            }
            SearchQueryChanged();
        }
    }

    private void FindTextBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs e)
    {
        sender.IsSuggestionListOpen = false;
        FindNext();
    }

    private void SearchQueryChanged()
    {
        UpdateSearchButtons();
        ShowStatus(string.Empty);
        QueryChanged?.Invoke(this, EventArgs.Empty);
        _queryTimer.Stop();
        if (Visibility == Visibility.Visible && SearchText.Length != 0)
        {
            _queryTimer.Start();
        }
    }

    private void UpdateSearchButtons()
    {
        var hasQuery = SearchText.Length != 0;
        NextButton.IsEnabled = hasQuery;
        PreviousButton.IsEnabled = hasQuery;
        ReplaceButton.IsEnabled = hasQuery;
        ReplaceAllButton.IsEnabled = hasQuery;
    }

    private void RequestFind(bool backwards, bool preview = false)
    {
        _queryTimer.Stop();
        if (SearchText.Length == 0 || (preview && Visibility != Visibility.Visible))
        {
            return;
        }

        var request = CreateRequest(backwards, preview);
        if (!preview)
        {
            _history.AddFind(request.Expression);
        }
        FindRequested?.Invoke(this, request);
    }

    private void RequestReplace(bool replaceAll)
    {
        _queryTimer.Stop();
        if (SearchText.Length == 0)
        {
            return;
        }

        _history.AddTerms(SearchText, ReplaceTextBox.Text);
        var callback = replaceAll ? ReplaceAllRequested : ReplaceRequested;
        callback?.Invoke(this, CreateRequest(backwards: !replaceAll && _replaceBackwardsOption.IsChecked));
    }

    private void ReplaceButton_Click(object sender, RoutedEventArgs e) => RequestReplace(replaceAll: false);

    private void ReplaceAllButton_Click(object sender, RoutedEventArgs e) => RequestReplace(replaceAll: true);

    private void CloseButton_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

    private void SearchBar_Unloaded(object sender, RoutedEventArgs e) => _queryTimer.Stop();
}

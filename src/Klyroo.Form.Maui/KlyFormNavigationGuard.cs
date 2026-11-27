using Klyroo.Form.Dynamic;

namespace Klyroo.Form.Maui;

/// <summary>
/// Asks before the user leaves a page while a form on it has unsaved changes. In a Shell app it listens to <see cref="Shell.Navigating"/> (the back button of Android, the back gesture, the back arrow, a tab or a route change),
/// holds the navigation with a deferral while it asks, and cancels it when the user stays. Place it anywhere in the page, next to the form; it takes no room.
/// </summary>
/// <remarks>
/// <para><b>What it guards.</b> Navigations that leave the page: pop, pop to root, a change of the shell item, section or content, and a route that replaces the stack. Pushing another page on top is not guarded, since the form
/// is still there when the user comes back. A navigation the platform does not let a handler cancel (<see cref="ShellNavigatingEventArgs.CanCancel"/> is <c>false</c>) goes through.</para>
/// <para><b>What it cannot guard.</b> Outside a Shell (a <see cref="NavigationPage"/> or a <see cref="TabbedPage"/>) MAUI gives no event that can cancel a back press or a swipe, so the guard has nothing to listen to:
/// call <see cref="ConfirmLeaveAsync"/> from your own back handling (an override of <c>Page.OnBackButtonPressed</c>, your own back button) instead. Closing the app, sending it to the background and the
/// system killing it have no confirmation on a phone, where the web guard can ask the browser to confirm closing the tab: <c>ConfirmExternalNavigation</c> of the Blazor guard has no native counterpart, so keep the draft with
/// <c>ExportDraft</c> if the loss matters.</para>
/// </remarks>
public class KlyFormNavigationGuard : View
{
    /// <summary>Identifies <see cref="Form"/>.</summary>
    public static readonly BindableProperty FormProperty = BindableProperty.Create(nameof(Form), typeof(IFormRuntime), typeof(KlyFormNavigationGuard), null);
    /// <summary>Identifies <see cref="Message"/>.</summary>
    public static readonly BindableProperty MessageProperty = BindableProperty.Create(nameof(Message), typeof(string), typeof(KlyFormNavigationGuard), null);
    /// <summary>Identifies <see cref="Disabled"/>.</summary>
    public static readonly BindableProperty DisabledProperty = BindableProperty.Create(nameof(Disabled), typeof(bool), typeof(KlyFormNavigationGuard), false);

    private Shell? _shell;

    /// <summary>Creates a guard.</summary>
    public KlyFormNavigationGuard()
    {
        WidthRequest = 0;
        HeightRequest = 0;
        InputTransparent = true;
        AutomationProperties.SetIsInAccessibleTree(this, false);
        Loaded += (_, _) => Attach();
        Unloaded += (_, _) => Detach();
    }

    /// <summary>The controller to protect: a <c>FormController&lt;TModel&gt;</c> or a <c>DynamicFormController</c>. Navigation is held while <see cref="IFormRuntime.IsDirty"/> is <c>true</c>, so the guard stops once a submission succeeds and the snapshot is retaken.</summary>
    /// <exception cref="ArgumentException">The value is a runtime that Klyroo.Form did not create.</exception>
    public IFormRuntime? Form { get => (IFormRuntime?)GetValue(FormProperty); set => SetValue(FormProperty, value); }

    /// <summary>The question of the default confirmation. The default is the <see cref="FormTexts.UnsavedChanges"/> text.</summary>
    public string? Message { get => (string?)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }

    /// <summary>Turns the guard off, for example while the page saves and navigates on its own. The default is <c>false</c>.</summary>
    public bool Disabled { get => (bool)GetValue(DisabledProperty); set => SetValue(DisabledProperty, value); }

    /// <summary>
    /// Called when the user leaves the page while the form is dirty. Return <c>true</c> to let the navigation go ahead and <c>false</c> to stay, for example after asking in your own dialog. Without it the guard asks
    /// with the confirm dialog of the <see cref="KlyOverlayHost"/> the guard is in, or with the alert of the page: <see cref="Message"/> with the <see cref="FormTexts.LeavePage"/> and <see cref="FormTexts.StayOnPage"/> buttons.
    /// </summary>
    public Func<ShellNavigatingEventArgs, Task<bool>>? ConfirmLeave { get; set; }

    /// <summary>Whether the guard would hold a navigation now: it is on, and the form has unsaved changes.</summary>
    public bool IsGuarding => !Disabled && Form is { IsDirty: true };

    /// <summary>The shell the guard listens to, or <c>null</c> outside a Shell.</summary>
    internal Shell? ListeningTo => _shell;

    /// <summary>Asks the question, with the default confirmation. Returns <c>true</c> when the form is clean (or the guard is off) or the user leaves, <c>false</c> when the user stays. Call it from your own back handling outside a Shell.</summary>
    /// <returns>Whether leaving the page is allowed.</returns>
    public async Task<bool> ConfirmLeaveAsync() => !IsGuarding || await AskAsync();

    /// <summary>Starts listening to <see cref="Shell.Navigating"/>. It runs when the guard enters a page; a guard that is not in a Shell listens to nothing.</summary>
    internal void Attach()
    {
        var shell = Shell.Current;
        if (ReferenceEquals(shell, _shell)) return;
        Detach();
        _shell = shell;
        if (_shell is not null) _shell.Navigating += OnNavigating;
    }

    internal void Detach()
    {
        if (_shell is not null) _shell.Navigating -= OnNavigating;
        _shell = null;
    }

    /// <summary>Which navigations leave the page: pushing another page on top does not.</summary>
    internal static bool Leaves(ShellNavigationSource source) => source is not (ShellNavigationSource.Push or ShellNavigationSource.Insert);

    private async void OnNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        if (!IsGuarding || !e.CanCancel || !Leaves(e.Source)) return;
        // Only the page the guard is in: another page of the stack does not answer for this form.
        if (FormUi.FindAncestor<Page>(this) is { } page && sender is Shell shell && shell.CurrentPage is { } current && !ReferenceEquals(current, page)) return;

        var deferral = e.GetDeferral();
        try
        {
            var leave = ConfirmLeave is not null ? await ConfirmLeave(e) : await AskAsync();
            if (!leave) e.Cancel();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // A confirmation that fails must not trap the user on the page, nor lose the form: stay.
            System.Diagnostics.Trace.TraceError($"Klyroo.Form: the confirmation of the navigation guard failed: {ex}");
            e.Cancel();
        }
        finally
        {
            deferral.Complete();
        }
    }

    private async Task<bool> AskAsync()
    {
        var texts = FormTexts.Current;
        var message = Message ?? texts.UnsavedChanges;
        if (KlyOverlayHost.TryFind(this, out var host))
        {
            return await host.ConfirmAsync(message, null, texts.LeavePage, texts.StayOnPage, KlyColor.Error);
        }
        if (FormUi.FindAncestor<Page>(this) is { } page)
        {
            return await page.DisplayAlertAsync(null, message, texts.LeavePage, texts.StayOnPage);
        }
        return false;
    }
}

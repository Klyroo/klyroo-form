using Klyroo.Form.Dynamic;
using Klyroo.UI.Maui;
using Microsoft.Maui.Controls;

namespace Klyroo.Form.Maui.Tests;

public class FormGuardTests
{
    private static DynamicFormController Controller() => new(FormKit.Schema(FormKit.Field("name")));

    private static ShellNavigatingEventArgs Navigating(ShellNavigationSource source, bool canCancel = true) =>
        new(new ShellNavigationState("//form"), new ShellNavigationState("//other"), source, canCancel);

    // ---- when it guards

    [Fact]
    public void A_clean_form_or_a_disabled_guard_holds_nothing()
    {
        var form = Controller();
        var guard = new KlyFormNavigationGuard { Form = form };
        Assert.False(guard.IsGuarding);

        form.SetValue("name", "x");
        Assert.True(guard.IsGuarding);

        guard.Disabled = true;
        Assert.False(guard.IsGuarding);
        Assert.False(new KlyFormNavigationGuard().IsGuarding);
    }

    [Fact]
    public async Task The_guard_stops_once_a_submission_succeeded_and_the_form_is_clean_again()
    {
        var form = Controller();
        var guard = new KlyFormNavigationGuard { Form = form };
        form.SetValue("name", "x");
        Assert.True(guard.IsGuarding);

        await form.SubmitAsync();

        Assert.False(guard.IsGuarding);
    }

    [Theory]
    [InlineData(ShellNavigationSource.Pop, true)]
    [InlineData(ShellNavigationSource.PopToRoot, true)]
    [InlineData(ShellNavigationSource.ShellItemChanged, true)]
    [InlineData(ShellNavigationSource.ShellSectionChanged, true)]
    [InlineData(ShellNavigationSource.ShellContentChanged, true)]
    [InlineData(ShellNavigationSource.Remove, true)]
    [InlineData(ShellNavigationSource.Unknown, true)]
    [InlineData(ShellNavigationSource.Push, false)]
    [InlineData(ShellNavigationSource.Insert, false)]
    public void Only_the_navigations_that_leave_the_page_are_guarded(ShellNavigationSource source, bool leaves)
    {
        Assert.Equal(leaves, KlyFormNavigationGuard.Leaves(source));
    }

    // ---- what it does with a navigation

    [Fact]
    public async Task A_navigation_away_from_a_dirty_form_is_cancelled_when_the_user_stays_and_goes_through_when_they_leave()
    {
        var form = Controller();
        form.SetValue("name", "x");
        var asked = new List<ShellNavigatingEventArgs>();
        var answer = false;
        var guard = new KlyFormNavigationGuard
        {
            Form = form,
            ConfirmLeave = e =>
            {
                asked.Add(e);
                return Task.FromResult(answer);
            },
        };

        var stay = Navigating(ShellNavigationSource.Pop);
        await guard.HandleNavigatingAsync(null, stay);
        Assert.True(stay.Cancelled);

        answer = true;
        var leave = Navigating(ShellNavigationSource.Pop);
        await guard.HandleNavigatingAsync(null, leave);
        Assert.False(leave.Cancelled);
        Assert.Equal(2, asked.Count);
    }

    [Fact]
    public async Task A_clean_form_a_push_and_a_navigation_that_cannot_be_cancelled_are_not_asked_about()
    {
        var form = Controller();
        var asked = 0;
        var guard = new KlyFormNavigationGuard { Form = form, ConfirmLeave = _ => { asked++; return Task.FromResult(false); } };

        await guard.HandleNavigatingAsync(null, Navigating(ShellNavigationSource.Pop));
        form.SetValue("name", "x");
        var push = Navigating(ShellNavigationSource.Push);
        await guard.HandleNavigatingAsync(null, push);
        var fixedNavigation = Navigating(ShellNavigationSource.Pop, canCancel: false);
        await guard.HandleNavigatingAsync(null, fixedNavigation);

        Assert.Equal(0, asked);
        Assert.False(push.Cancelled);
        Assert.False(fixedNavigation.Cancelled);
    }

    [Fact]
    public async Task A_confirmation_that_throws_keeps_the_user_on_the_page()
    {
        var form = Controller();
        form.SetValue("name", "x");
        var guard = new KlyFormNavigationGuard { Form = form, ConfirmLeave = _ => throw new InvalidOperationException("dialog failed") };

        var args = Navigating(ShellNavigationSource.Pop);
        await guard.HandleNavigatingAsync(null, args);

        Assert.True(args.Cancelled);
    }

    [Fact]
    public async Task A_disabled_guard_lets_everything_through()
    {
        var form = Controller();
        form.SetValue("name", "x");
        var guard = new KlyFormNavigationGuard { Form = form, Disabled = true, ConfirmLeave = _ => Task.FromResult(false) };

        var args = Navigating(ShellNavigationSource.Pop);
        await guard.HandleNavigatingAsync(null, args);

        Assert.False(args.Cancelled);
    }

    // ---- the default confirmation

    private static async Task<bool> Answer(KlyOverlayHost host, Task<bool> question, string button)
    {
        var layer = Assert.Single(host.Layers);
        FormUi.Descendants(layer.Content).OfType<KlyButton>().First(b => b.Text == button).SendClicked();
        return await question;
    }

    [Fact]
    public async Task The_default_confirmation_asks_in_the_overlay_host_with_the_message_and_the_leave_and_stay_buttons()
    {
        var form = Controller();
        form.SetValue("name", "x");
        var guard = new KlyFormNavigationGuard { Form = form, Message = "Discard your edits?" };
        var host = new KlyOverlayHost { Content = new VerticalStackLayout { guard } };

        var question = guard.ConfirmLeaveAsync();

        var layer = Assert.Single(host.Layers);
        Assert.Contains(FormUi.Descendants(layer.Content).OfType<Label>(), l => l.Text == "Discard your edits?");
        Assert.Contains(FormUi.Descendants(layer.Content).OfType<KlyButton>(), b => b.Text == FormTexts.Current.LeavePage);
        Assert.Contains(FormUi.Descendants(layer.Content).OfType<KlyButton>(), b => b.Text == FormTexts.Current.StayOnPage);
        Assert.False(await Answer(host, question, FormTexts.Current.StayOnPage));
    }

    [Fact]
    public async Task Leaving_in_the_default_confirmation_lets_the_navigation_go_and_the_default_message_is_the_unsaved_changes_text()
    {
        var form = Controller();
        form.SetValue("name", "x");
        var guard = new KlyFormNavigationGuard { Form = form };
        var host = new KlyOverlayHost { Content = new VerticalStackLayout { guard } };
        var args = Navigating(ShellNavigationSource.Pop);

        var handling = guard.HandleNavigatingAsync(null, args);
        Assert.Contains(FormUi.Descendants(Assert.Single(host.Layers).Content).OfType<Label>(), l => l.Text == FormTexts.Current.UnsavedChanges);
        FormUi.Descendants(host.Layers[0].Content).OfType<KlyButton>().First(b => b.Text == FormTexts.Current.LeavePage).SendClicked();
        await handling;

        Assert.False(args.Cancelled);
    }

    [Fact]
    public async Task Staying_in_the_default_confirmation_cancels_the_navigation()
    {
        var form = Controller();
        form.SetValue("name", "x");
        var guard = new KlyFormNavigationGuard { Form = form };
        var host = new KlyOverlayHost { Content = new VerticalStackLayout { guard } };
        var args = Navigating(ShellNavigationSource.Pop);

        var handling = guard.HandleNavigatingAsync(null, args);
        FormUi.Descendants(host.Layers[0].Content).OfType<KlyButton>().First(b => b.Text == FormTexts.Current.StayOnPage).SendClicked();
        await handling;

        Assert.True(args.Cancelled);
    }

    [Fact]
    public async Task ConfirmLeaveAsync_is_true_without_asking_when_nothing_is_to_lose_and_false_where_there_is_nobody_to_ask()
    {
        var form = Controller();
        var guard = new KlyFormNavigationGuard { Form = form };
        Assert.True(await guard.ConfirmLeaveAsync());

        form.SetValue("name", "x");

        Assert.False(await guard.ConfirmLeaveAsync());
    }

    // ---- the shell it listens to

    [Fact]
    public void The_guard_listens_to_the_shell_it_is_in_and_stops_when_it_leaves()
    {
        var shell = new Shell();
        var guard = new KlyFormNavigationGuard { CurrentShell = () => shell };

        guard.Attach();
        Assert.Same(shell, guard.ListeningTo);

        guard.Attach();
        Assert.Same(shell, guard.ListeningTo);

        guard.Detach();
        Assert.Null(guard.ListeningTo);
    }

    [Fact]
    public void Outside_a_shell_the_guard_listens_to_nothing()
    {
        var guard = new KlyFormNavigationGuard { CurrentShell = () => null };

        guard.Attach();

        Assert.Null(guard.ListeningTo);
    }

    [Fact]
    public void The_guard_takes_no_room_and_is_not_read_by_a_screen_reader()
    {
        var guard = new KlyFormNavigationGuard();

        Assert.Equal(0, guard.WidthRequest);
        Assert.Equal(0, guard.HeightRequest);
        Assert.True(guard.InputTransparent);
        Assert.False(AutomationProperties.GetIsInAccessibleTree(guard));
    }

    [Fact]
    public void The_guard_has_no_form_and_no_custom_message_until_it_is_given_them()
    {
        var guard = new KlyFormNavigationGuard();
        Assert.Null(guard.Form);
        Assert.Null(guard.Message);
        Assert.False(guard.Disabled);

        var form = Controller();
        guard.Form = form;

        Assert.Same(form, guard.Form);
    }
}

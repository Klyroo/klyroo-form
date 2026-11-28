using Klyroo.Form.Dynamic;
using Microsoft.Extensions.DependencyInjection;

namespace Klyroo.Form.Maui;

/// <summary>
/// The host of the overlay forms opened with <see cref="FormService"/> (typed) and <see cref="DynamicFormService"/> (runtime): the native counterpart of the Blazor provider. Wrap the content of the page in it
/// (it is a <see cref="KlyOverlayHost"/>, so the other overlay controls of Klyroo.UI.Maui work inside it too) and call <see cref="FormService.CreateFormAsync{TModel}"/> or <see cref="DynamicFormService.CreateFormAsync"/>
/// from anywhere that has the service.
/// </summary>
/// <remarks>
/// <para>A <see cref="FormShellMode.Modal"/> or <see cref="FormShellMode.Inline"/> form opens in a <see cref="KlyModal"/>, a <see cref="FormShellMode.Drawer"/> form in a <see cref="KlySlideover"/> docked to the end of the screen,
/// and a <see cref="FormShellMode.Fullscreen"/> form in a layer that covers the whole host. The close button of the form, a tap outside it and the back button of Android close it without submitting; none of them works
/// while the form saves. One overlay serves the typed forms and another the runtime forms, so one of each kind can be open together.</para>
/// <para>The provider serves <see cref="Service"/> and <see cref="DynamicService"/>; without them, the ones registered with <see cref="FormServiceCollectionExtensions.AddKlyrooFormMaui"/>, else <see cref="FormService.Default"/>
/// and <see cref="FormService.DefaultDynamic"/>. It listens to them while it is in a page.</para>
/// </remarks>
/// <example>
/// <code>
/// var provider = new KlyFormProvider { Content = pageContent };
/// var outcome = await FormService.Default.CreateFormAsync(schema, model, FormShellMode.Drawer);
/// if (outcome.IsCompleted) await SaveAsync(outcome.Model!);
/// </code>
/// </example>
public class KlyFormProvider : KlyOverlayHost
{
    /// <summary>Identifies <see cref="Service"/>.</summary>
    public static readonly BindableProperty ServiceProperty = BindableProperty.Create(nameof(Service), typeof(FormService), typeof(KlyFormProvider), null, propertyChanged: (b, _, _) => ((KlyFormProvider)b).Reattach());
    /// <summary>Identifies <see cref="DynamicService"/>.</summary>
    public static readonly BindableProperty DynamicServiceProperty = BindableProperty.Create(nameof(DynamicService), typeof(DynamicFormService), typeof(KlyFormProvider), null, propertyChanged: (b, _, _) => ((KlyFormProvider)b).Reattach());

    private readonly Slot _typed;
    private readonly Slot _dynamic;
    private FormService? _attachedTyped;
    private DynamicFormService? _attachedDynamic;
    private bool _attached;
    private bool _reconciling;

    /// <summary>Creates a provider.</summary>
    public KlyFormProvider()
    {
        _typed = new Slot(this);
        _dynamic = new Slot(this);
        Loaded += (_, _) => Attach();
        Unloaded += (_, _) => Detach();
    }

    /// <summary>The service of the typed forms this provider shows. Null serves the registered one, else <see cref="FormService.Default"/>.</summary>
    public FormService? Service { get => (FormService?)GetValue(ServiceProperty); set => SetValue(ServiceProperty, value); }

    /// <summary>The service of the runtime forms this provider shows. Null serves the registered one, else <see cref="FormService.DefaultDynamic"/>.</summary>
    public DynamicFormService? DynamicService { get => (DynamicFormService?)GetValue(DynamicServiceProperty); set => SetValue(DynamicServiceProperty, value); }

    /// <summary>The service of the typed forms in use.</summary>
    public FormService EffectiveService => Service ?? Resolve<FormService>() ?? FormService.Default;

    /// <summary>The service of the runtime forms in use.</summary>
    public DynamicFormService EffectiveDynamicService => DynamicService ?? Resolve<DynamicFormService>() ?? FormService.DefaultDynamic;

    /// <summary>The overlay of the typed forms: the modal of a <see cref="FormShellMode.Modal"/> form. For the tests.</summary>
    internal KlyModal TypedModal => _typed.Modal;

    /// <summary>The slideover of the typed forms. For the tests.</summary>
    internal KlySlideover TypedSlideover => _typed.Slideover;

    internal KlyModal DynamicModal => _dynamic.Modal;

    internal KlySlideover DynamicSlideover => _dynamic.Slideover;

    internal KlyOverlayLayer? TypedFullscreen => _typed.FullscreenLayer;

    internal KlyOverlayLayer? DynamicFullscreen => _dynamic.FullscreenLayer;

    internal bool IsAttached => _attached;

    /// <summary>Starts showing the forms of the services. It runs when the provider enters a page.</summary>
    internal void Attach()
    {
        if (_attached) return;
        _attached = true;
        _attachedTyped = EffectiveService;
        _attachedDynamic = EffectiveDynamicService;
        _attachedTyped.Changed += OnServiceChanged;
        _attachedDynamic.Changed += OnServiceChanged;
        Reconcile();
    }

    /// <summary>Stops listening to the services. A form that is open stays open in its service, and shows again when the provider comes back.</summary>
    internal void Detach()
    {
        if (!_attached) return;
        _attached = false;
        if (_attachedTyped is not null) _attachedTyped.Changed -= OnServiceChanged;
        if (_attachedDynamic is not null) _attachedDynamic.Changed -= OnServiceChanged;
        _attachedTyped = null;
        _attachedDynamic = null;
        _reconciling = true;
        try
        {
            _typed.CloseAll();
            _dynamic.CloseAll();
        }
        finally
        {
            _reconciling = false;
        }
    }

    private T? Resolve<T>()
        where T : class
    {
        try
        {
            return Handler?.MauiContext?.Services.GetService<T>();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private void Reattach()
    {
        if (!_attached) return;
        Detach();
        Attach();
    }

    private void OnServiceChanged() => FormUi.Post(this, Reconcile);

    /// <summary>Shows the form each service has open, and closes the overlay of the one that has none.</summary>
    internal void Reconcile()
    {
        if (!_attached) return;
        var typed = _attachedTyped?.Active is { } overlay
            ? new Shown(overlay.Engine, overlay.Title, overlay.Mode, overlay.Body(), _attachedTyped.CloseForm)
            : null;
        var dynamic = _attachedDynamic?.ActiveForm is { } form
            ? new Shown(form.Engine, form.Schema.Title, _attachedDynamic.ActiveMode, DynamicBody(_attachedDynamic, form), _attachedDynamic.CloseForm)
            : null;
        _reconciling = true;
        try
        {
            _typed.Show(typed);
            _dynamic.Show(dynamic);
        }
        finally
        {
            _reconciling = false;
        }
    }

    private View DynamicBody(DynamicFormService service, DynamicFormController form)
    {
        if (_dynamic.BodyOf is { } body && ReferenceEquals(_dynamic.BodyForm, form)) return body;
        _dynamic.Body?.Dispose();
        var engine = new KlyDynamicFormEngine { Form = form, Shell = service.ActiveMode };
        engine.Submitted += (_, _) => service.CompleteSubmittedForm(form);
        engine.Cancelled += (_, _) => service.CloseForm();
        _dynamic.Body = engine;
        _dynamic.BodyForm = form;
        return engine;
    }

    // What an open overlay shows.
    private sealed record Shown(FormEngine Engine, string? Title, FormShellMode Mode, View Body, Action Close);

    private enum Kind
    {
        None,
        Modal,
        Slideover,
        Fullscreen,
    }

    // One overlay for one kind of form: a modal, a slideover and a fullscreen layer, of which one at a time is open.
    private sealed class Slot
    {
        private readonly KlyFormProvider _owner;
        private Kind _open;
        private FormEngine? _followed;
        private string? _lastTitle;
        private Action? _close;
        private View? _body;

        public Slot(KlyFormProvider owner)
        {
            _owner = owner;
            Modal = new KlyModal { Size = KlySize.Lg, Close = false, InputTransparent = true };
            Slideover = new KlySlideover { Close = false, InputTransparent = true };
            Modal.Closed += (_, _) => Dismissed(Kind.Modal);
            Slideover.Closed += (_, _) => Dismissed(Kind.Slideover);
            owner.Children.Add(Modal);
            owner.Children.Add(Slideover);
        }

        public KlyModal Modal { get; }

        public KlySlideover Slideover { get; }

        public KlyOverlayLayer? FullscreenLayer { get; private set; }

        public KlyDynamicFormEngine? Body { get; set; }

        public DynamicFormController? BodyForm { get; set; }

        public View? BodyOf => Body;

        public void Show(Shown? shown)
        {
            if (shown is null)
            {
                CloseAll();
                Follow(null);
                return;
            }
            Follow(shown.Engine);
            _lastTitle = shown.Title;
            _close = shown.Close;
            var kind = shown.Mode switch
            {
                FormShellMode.Drawer => Kind.Slideover,
                FormShellMode.Fullscreen => Kind.Fullscreen,
                _ => Kind.Modal,
            };
            if (kind != _open) CloseAll();
            var label = shown.Title ?? FormTexts.Current.FormTitle;
            var dismissible = !shown.Engine.IsSubmitting;
            _body = shown.Body;
            switch (kind)
            {
                case Kind.Modal:
                    Modal.AccessibleName = label;
                    Modal.Dismissible = dismissible;
                    if (!ReferenceEquals(Modal.Body, shown.Body)) Modal.Body = shown.Body;
                    Modal.IsOpen = true;
                    break;
                case Kind.Slideover:
                    Slideover.AccessibleName = label;
                    Slideover.Dismissible = dismissible;
                    if (!ReferenceEquals(Slideover.Body, shown.Body)) Slideover.Body = shown.Body;
                    Slideover.IsOpen = true;
                    break;
                case Kind.Fullscreen:
                    ShowFullscreen(shown.Body, dismissible);
                    break;
            }
            _open = kind;
        }

        public void CloseAll()
        {
            if (Modal.IsOpen) Modal.IsOpen = false;
            if (Slideover.IsOpen) Slideover.IsOpen = false;
            if (FullscreenLayer is { IsShown: true } layer) layer.Host.Hide(layer);
            FullscreenLayer = null;
            _open = Kind.None;
        }

        private void ShowFullscreen(View body, bool dismissible)
        {
            if (FullscreenLayer is { IsShown: true } layer)
            {
                layer.Dismissible = dismissible;
                if (!ReferenceEquals(layer.Content.GetType(), typeof(FormFullscreen)) || ((FormFullscreen)layer.Content).Body != body) CloseAll();
                else return;
            }
            var surface = new FormFullscreen(body);
            FullscreenLayer = _owner.Show(surface, new KlyOverlayLayerOptions { Modal = true, Dim = false, Dismissible = dismissible, Transition = KlyOverlayTransition.SlideFromBottom });
            FullscreenLayer.Dismissed += (_, _) => Dismissed(Kind.Fullscreen);
        }

        // A tap outside the form, the back button, or the close of the overlay by the user: the form ends as if it had been closed.
        private void Dismissed(Kind kind)
        {
            if (kind == Kind.Fullscreen) FullscreenLayer = null;
            if (_owner._reconciling) return;
            if (_open == kind) _open = Kind.None;
            _close?.Invoke();
        }

        // The overlay cannot be dismissed while the form saves.
        private void Follow(FormEngine? engine)
        {
            if (ReferenceEquals(_followed, engine)) return;
            if (_followed is not null) _followed.Changed -= OnEngineChanged;
            _followed = engine;
            if (engine is not null) engine.Changed += OnEngineChanged;
        }

        private void OnEngineChanged() => FormUi.Post(_owner, () =>
        {
            if (_followed is not { } engine) return;
            var dismissible = !engine.IsSubmitting;
            if (Modal.IsOpen) Modal.Dismissible = dismissible;
            if (Slideover.IsOpen) Slideover.Dismissible = dismissible;
            if (FullscreenLayer is { IsShown: true } layer) layer.Dismissible = dismissible;
        });
    }
}

/// <summary>The surface of a fullscreen form: the page background over the whole host, and the form in a column no wider than a tablet's, which scrolls.</summary>
internal sealed class FormFullscreen : Border
{
    public FormFullscreen(View body)
    {
        Body = body;
        StrokeThickness = 0;
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Fill;
        this.SetThemeColor(BackgroundColorProperty, KlyTheme.Background);
        body.HorizontalOptions = LayoutOptions.Fill;
        var column = new Grid { MaximumWidthRequest = 768, HorizontalOptions = LayoutOptions.Center, Padding = new Thickness(16) };
        FormUi.Detach(body);
        column.Add(body);
        Content = new ScrollView { Content = column };
    }

    public View Body { get; }
}

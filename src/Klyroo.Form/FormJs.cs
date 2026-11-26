using Microsoft.JSInterop;

namespace Klyroo.Form;

// The few things the components cannot do with Blazor events: moving focus to an element by id. Every call is best effort and
// never throws: without JavaScript (prerendering, a disconnected circuit, a test host) the form still works, focus just stays put.
internal sealed class FormJs(IJSRuntime js) : IAsyncDisposable
{
    private const string ModulePath = "./_content/Klyroo.Form/klyroo-form.js";
    private Task<IJSObjectReference?>? _module;

    public async Task FocusAsync(string id)
    {
        try
        {
            var module = await (_module ??= LoadAsync());
            if (module is not null)
            {
                await module.InvokeVoidAsync("focusById", id);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Focus is a convenience: a failing call must not break the form.
        }
    }

    private async Task<IJSObjectReference?> LoadAsync()
    {
        try
        {
            return await js.InvokeAsync<IJSObjectReference>("import", ModulePath);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;
        try
        {
            if (await _module is { } module) await module.DisposeAsync();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // The circuit may already be gone.
        }
    }
}

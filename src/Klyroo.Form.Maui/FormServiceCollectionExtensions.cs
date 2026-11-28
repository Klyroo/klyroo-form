using Klyroo.Form.Dynamic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Klyroo.Form.Maui;

/// <summary>Dependency injection registration for the native views of Klyroo.Form.</summary>
public static class FormServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="FormService"/> and <see cref="DynamicFormService"/> as singletons, since a MAUI app has one window to show a form in (a scoped service is one per page in a Blazor app).
    /// A <see cref="KlyFormProvider"/> serves the services of the container of its app; without this registration it serves <see cref="FormService.Default"/> and <see cref="FormService.DefaultDynamic"/>.
    /// </summary>
    /// <param name="services">The service collection, <c>builder.Services</c> in <c>MauiProgram</c>.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddKlyrooFormMaui(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<FormService>();
        services.TryAddSingleton<DynamicFormService>();
        return services;
    }
}

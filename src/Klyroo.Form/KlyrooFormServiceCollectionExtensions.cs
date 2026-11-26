using Klyroo.Form.Dynamic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Klyroo.Form;

/// <summary>Dependency injection registration for Klyroo.Form.</summary>
public static class KlyrooFormServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="DynamicFormService"/> as a scoped service and calls <c>AddKlyrooUi()</c>, because the components build on Klyroo.UI.
    /// In a Blazor Web App with interactive WebAssembly or Auto, call it in the server project and in the client project.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">
    /// Changes the options, such as the built-in texts of a locale with <see cref="KlyrooFormOptions.Texts"/>. The options are stored with
    /// the Klyroo.UI options, so a second call changes the same instance.
    /// </param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddKlyrooForm(this IServiceCollection services, Action<KlyrooFormOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddKlyrooUi(options => configure?.Invoke(options.Extension<KlyrooFormOptions>()));
        services.TryAddScoped<DynamicFormService>();
        return services;
    }
}

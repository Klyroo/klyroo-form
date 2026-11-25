using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Klyroo.Form;

/// <summary>Dependency injection registration for Klyroo.Form.</summary>
public static class KlyrooFormServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="FormService"/> as a scoped service and calls <c>AddKlyrooUi()</c>, because the components build on Klyroo.UI.
    /// In a Blazor Web App with interactive WebAssembly or Auto, call it in the server project and in the client project.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddKlyrooForm(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddKlyrooUi();
        services.TryAddScoped<FormService>();
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Klyroo.Form;

public static class KlyrooFormServiceCollectionExtensions
{
    public static IServiceCollection AddKlyrooForm(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddKlyrooUi();
        services.TryAddScoped<FormService>();
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lux.Form;

public static class LuxFormServiceCollectionExtensions
{
    public static IServiceCollection AddLuxForm(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddLuxUi();
        services.TryAddScoped<FormService>();
        return services;
    }
}

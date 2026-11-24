using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Blux.UI.Tools;

public static class LuxFormServiceCollectionExtensions
{
    public static IServiceCollection AddLuxForm(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddBluxUi();
        services.TryAddScoped<FormService>();
        return services;
    }
}

using ExchangeRateManager.Common.Configuration;
using ExchangeRateManager.Common.Constants;
using ExchangeRateManager.Common.Exceptions;
using ExchangeRateManager.Common.Interfaces.Base;
using ExchangeRateManager.Common.Interfaces.ServiceLifetime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Reflection;
using IConnectionFactory = RabbitMQ.Client.IConnectionFactory;

namespace ExchangeRateManager.Common.Extensions;

/// <summary>
/// Helper extensions for loading and registring services.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// A set of calls to load all components dependencies
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddServiceCollectionExtensions(this IServiceCollection services)
    {
        services.AddHttpClientFactories<IHttpClient>();
        services.AddTransient<KeyedServiceFactory>();   //NOTE: Must be transient as singleton services cannot access scoped contexts (on docker, locally works fine)
        services.AddAutoMapper(LoadAssemblies());

        services.LoadServicesFromAssemblies<IClient>();
        services.LoadServicesFromAssemblies<IRepository>();
        services.LoadServicesFromAssemblies<IService>();

        return services;
    }

    /// <summary>
    /// Discovers and registers all services components into service collection
    /// based on their ancestor types through reflection.
    /// </summary>
    /// <typeparam name="TService">The base service type to lookup.</typeparam>
    /// <param name="serviceCollection">The service Collection.</param>
    /// <param name="serviceLifetime">The service lifetime (Scoped, Singleton or Transient).</param>
    /// <returns>The <paramref name="serviceCollection"/> for daisy chaining.</returns>
    /// <exception cref="ArgumentException">The asserted type is not an interface.</exception>
    public static IServiceCollection LoadServicesFromAssemblies<TService>(this IServiceCollection serviceCollection) where TService : class
    {
        typeof(TService).AssertInterface();
        IEnumerable<Assembly> availableAssemblies = LoadAssemblies();

        var serviceTypes = availableAssemblies
           .SelectMany(x => x.GetTypes()
               .Where(t => t.IsInterface && t.GetInterfaces().Any(i => i == typeof(TService))));

        var typeMaps = serviceTypes.Select(Service =>
        {
            var Implementations = availableAssemblies
                .SelectMany(x => x.GetTypes()
                    .Where(t => t.IsClass)
                    .Where(t => t.GetInterfaces().Any(x => x == Service)));

            return new { Service, Implementations };
        });

        foreach (var typeMap in typeMaps)
        {
            if (typeMap.Implementations.Count() == 1)
            {
                var singleType = typeMap.Implementations.Single();
                serviceCollection.Add(new ServiceDescriptor(
                    typeMap.Service, singleType,
                    singleType.GetServiceLifetime()));
            }
            else
            {
                foreach (var implementationType in typeMap.Implementations)
                {
                    serviceCollection.Add(new ServiceDescriptor(
                        typeMap.Service, implementationType.FullName, implementationType,
                        implementationType.GetServiceLifetime()));

                }

                serviceCollection.AddTransient(typeMap.Service, sp => sp
                    .GetRequiredService<KeyedServiceFactory>().Create(typeMap.Service));
            }
        }


        return serviceCollection;
    }

    /// <summary>
    /// Adds factories for all registered Http Clients, based on the AppSettings definitions.
    /// </summary>
    public static IServiceCollection AddHttpClientFactories<THttpClient>(this IServiceCollection serviceCollection)
        where THttpClient : class
    {
        typeof(THttpClient).AssertInterface();
        IEnumerable<Assembly> availableAssemblies = LoadAssemblies();

        var serviceTypes = availableAssemblies
           .SelectMany(x => x.GetTypes()
               .Where(t => t.IsClass && t.GetInterfaces().Any(i => i == typeof(THttpClient))));

        foreach (var serviceType in serviceTypes)
        {
            serviceCollection.AddHttpClient(serviceType.FullName!, (IServiceProvider sp, HttpClient httpClient) =>
            {
                var settings = sp.GetRequiredService<IOptionsMonitor<Settings>>().CurrentValue;
                if (settings.HttpClients!.TryGetValue(serviceType.FullName!, out var clientSettings))
                {
                    var uri = clientSettings.BaseAddress ??
                        throw new MissingSettingException($"{nameof(settings.HttpClients)}.\"{serviceType.FullName}\".{nameof(clientSettings.BaseAddress)}'");

                    if (!string.IsNullOrWhiteSpace(clientSettings.QueryParams))
                    {
                        uri += "?" + clientSettings.QueryParams;
                    }

                    httpClient.BaseAddress = new Uri(uri);

                    if (!string.IsNullOrWhiteSpace(clientSettings.Authorization))
                    {
                        httpClient.DefaultRequestHeaders.Add("Authorization", clientSettings.Authorization);
                    }
                }
                else
                {
                    throw new MissingSettingException($"\"{nameof(settings.HttpClients)}\".\"{serviceType.FullName}\"");
                }
            });
        }

        return serviceCollection;
    }

    /// <summary>
    /// Adds the RabbitMQ Connection factory
    /// </summary>
    public static IServiceCollection AddRabbitMqConnectionFactory(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionFactory>(x => new ConnectionFactory
        {
            Uri = new Uri(configuration.GetConnectionString(ConnectionStrings.MessageQueue)!)
        });

        return services;
    }

     /// <summary>
     /// Add a distributed cache (optional)
     /// </summary>
    public static IServiceCollection AddCache(this IServiceCollection services, IConfiguration configuration, Settings settings)
    {
        if (settings.Cache == CacheSettings.Redis)
        {
            // Add Redis/Valkey cache service
            services.AddStackExchangeRedisCache(options => options.Configuration =
                configuration.GetConnectionString(ConnectionStrings.CacheConnection)!);

        }
        else if(settings.Cache == CacheSettings.Memory)
        {
            services.AddDistributedMemoryCache();
        }

        return services;
    }

    /// <summary>
    /// Asserts if the type is an interface.
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <exception cref="ArgumentException">The asserted type is not an interface.</exception>
    public static void AssertInterface(this Type type)
    {
        if (!type.IsInterface)
        {
            throw new ArgumentException($"{type.FullName} must be an interface");
        }
    }

    /// <summary>
    /// Gets an http client from the respective factory, based on the requesting client component.
    /// </summary>
    /// <typeparam name="T">The type defined on the appsettings usually the respecive class or
    /// interface that consumes the http client. Check Appsettings for more details.</typeparam>
    public static HttpClient CreateClient<T>(this IHttpClientFactory factory)
        => factory.CreateClient(typeof(T).FullName!);

    /// <summary>
    /// Resolves the service implementation lifecycle, by looking for the inherited
    /// <see cref="IScoped"/>, <see cref="ITransient"/>, or <see cref="ISingleton"/> interfaces.
    /// </summary>
    /// <param name="implementationType">The implementation type to be checked.</param>
    /// <returns>The resolved <see cref="ServiceLifetime"/>.</returns>
    /// <exception cref="TypeLoadException">If no service lifetime has been inherited.</exception>
    private static ServiceLifetime GetServiceLifetime(this Type implementationType)
    {
        if (implementationType.IsAssignableTo(typeof(IScoped)))
        {
            return ServiceLifetime.Scoped;
        }

        if (implementationType.IsAssignableTo(typeof(ITransient)))
        {
            return ServiceLifetime.Transient;
        }

        if (implementationType.IsAssignableTo(typeof(ISingleton)))
        {
            return ServiceLifetime.Singleton;
        }

        throw new TypeLoadException(
            $"'{implementationType.FullName}' requires a service Lifetime interface. Inherit either " +
            $"'{typeof(IScoped).FullName}', '{typeof(ITransient).FullName}' or '{typeof(ISingleton).FullName}'.");
    }

    /// <summary>
    /// Enforces loading all project's assemblies from DependencyContext as they may
    /// not be yet available through AppDomain.CurrentDomain.GetAssemblies().
    /// </summary>
    /// <remarks>See: <see href="https://github.com/dotnet/runtime/issues/9184#issuecomment-339202986"/></remarks>
    /// <returns>The list of loaded assemblies.</returns>
    public static IEnumerable<Assembly> LoadAssemblies()
    {
        return DependencyContext.Default!.GetDefaultAssemblyNames()
           .Where(x => x.Name?.StartsWith(nameof(ExchangeRateManager)) ?? false)
           .Select(Assembly.Load);

    }

}

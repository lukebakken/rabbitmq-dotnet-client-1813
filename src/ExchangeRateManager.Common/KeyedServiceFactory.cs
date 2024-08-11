using ExchangeRateManager.Common.Configuration;
using ExchangeRateManager.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ExchangeRateManager.Common
{
    /// <summary>
    /// Service factory for keyed services. Loads the servicers based on the "KeyedServices" setting on appsettings.json.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="settings">The appsettings configuration.</param>
    public class KeyedServiceFactory(IServiceProvider serviceProvider, IOptionsMonitor<Settings> settings)
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IOptionsMonitor<Settings> _settings = settings;

        /// <summary>
        /// Gets a keyed service from the service provider, based on 'KeyedServices' configuration in AppSettings.json.
        /// </summary>
        /// <typeparam name="T">The service type to be resolved.</typeparam>
        /// <returns>The resolved dependency.</returns>
        /// <exception cref="TypeLoadException">The type is not mapped on AppSettings.json</exception>
        /// <exception cref="ArgumentException">The asserted type is not an interface.</exception>
        /// <exception cref="InvalidOperationException">There is no service of type <typeparamref name="T"/>.</exception>
        public T Create<T>() where T : class
        {
            return (Create(typeof(T)) as T)!;
        }

        /// <summary>
        /// Gets a keyed service from the service provider, based on 'KeyedServices' configuration in AppSettings.json.
        /// </summary>
        /// <typeparam name="serviceType">The service type to be resolved.</typeparam>
        /// <returns>The resolved dependency.</returns>
        /// <exception cref="TypeLoadException">The type is not mapped on AppSettings.json</exception>
        /// <exception cref="ArgumentException">The asserted type is not an interface.</exception>
        /// <exception cref="InvalidOperationException">There is no service of type <typeparamref name="T"/>.</exception>
        public object Create(Type serviceType)
        {
            serviceType.AssertInterface();

            // NOTE1: Getting settings from IOptionsMonitor (Singleton) or
            // IOptionsSnaphot (Transient or Scoped) allow to make feature flag updates
            // on appsettings in runtime without the need of redeploying the app.
            // NOTE2: Alternatively we can enable or switch classes
            // using feature flags from a database or external service, at runtime.

            string? typeFullName = default;
            _settings.CurrentValue.KeyedServices?.TryGetValue(serviceType.FullName!, out typeFullName);

            if (string.IsNullOrWhiteSpace(typeFullName))
            {
                throw new TypeLoadException($"Must have a defined type map for '{serviceType.FullName}'.");
            }

            object instance = _serviceProvider.GetRequiredKeyedService(serviceType, typeFullName);

            return instance;
        }
    }
}

using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Testing
{
    internal static class ConfigHelper
    {
        internal static IConfigurationRoot GetConfig() => new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .Build();
    }
}

using System;

namespace Domains.Registry
{
    /// <summary>Minimal service-locator contract for cross-domain registration + resolve.</summary>
    public interface IServiceRegistry
    {
        void Register<T>(T impl);
        T Resolve<T>();
    }
}

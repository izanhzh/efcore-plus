using EfCorePlus.Test.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EfCorePlus.Test
{
    public abstract class TestBase : IClassFixture<ServiceProviderFixture>
    {
        private readonly ServiceProviderFixture _serviceProviderFixture;

        protected TestBase(ServiceProviderFixture serviceProviderFixture)
        {
            _serviceProviderFixture = serviceProviderFixture;
        }

        public IServiceProvider ServiceProvider => _serviceProviderFixture.ServiceProvider;

        public TestDbContextFactory CreateTestDbContextFactory() => ServiceProvider.GetRequiredService<TestDbContextFactory>();
    }
}

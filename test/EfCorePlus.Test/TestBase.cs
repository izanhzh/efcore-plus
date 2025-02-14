using EfCorePlus.Test.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace EfCorePlus.Test
{
    public abstract class TestBase : IClassFixture<ServiceProviderFixture>
    {
        private readonly ServiceProviderFixture _serviceProviderFixture;

        private readonly TestDbContextFactory _factory;

        protected TestBase(ServiceProviderFixture serviceProviderFixture)
        {
            _serviceProviderFixture = serviceProviderFixture;
            _factory = _serviceProviderFixture.ServiceProvider.GetRequiredService<TestDbContextFactory>();
        }

        public IServiceProvider ServiceProvider => _serviceProviderFixture.ServiceProvider;

        public TestDbContext CreateTestDbContext() => _factory.CreateContext();
    }
}

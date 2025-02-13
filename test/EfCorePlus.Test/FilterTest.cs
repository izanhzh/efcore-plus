using EfCorePlus.Filters.Standard;
using EfCorePlus.Test.Entities;
using System;

namespace EfCorePlus.Test
{
    public class FilterTest : TestBase
    {
        public FilterTest(ServiceProviderFixture serviceProviderFixture) : base(serviceProviderFixture)
        {
        }

        [Fact]
        public void SoftDeleteFilterTest()
        {
            using var factory = CreateTestDbContextFactory();
            using var db = factory.CreateContext();

            var user = new User
            {
                Name = "Test",
                TenantId = 1,
                IsDeleted = true
            };
            db.Users.Add(user);
            db.SaveChanges();

            var users = db.Users.ToList();

            Assert.Equal(users?.Count, 0);
        }

        [Fact]
        public void TanentFilterTest()
        {
            using var factory = CreateTestDbContextFactory();
            using var db = factory.CreateContext();

            var user1 = new User
            {
                Name = "Test1",
                TenantId = 1,
                IsDeleted = false
            };
            var user2 = new User
            {
                Name = "Test2",
                TenantId = 2,
                IsDeleted = false
            };
            db.Users.Add(user1);
            db.Users.Add(user2);
            db.SaveChanges();

            var users = db.Users.ToList();
            Assert.Equal(users?.Count, 0);

            db.CurrentTenantId = 1;
            users = db.Users.ToList();
            Assert.Equal(users?.Count, 1);
            Assert.Equal(users?[0].TenantId, 1);

            db.CurrentTenantId = 2;
            users = db.Users.ToList();
            Assert.Equal(users?.Count, 1);
            Assert.Equal(users?[0].TenantId, 2);

            using (var filterManager = db.CreateFilterManager())
            {
                filterManager.DisableFilter<TenantFilter>();

                users = db.Users.ToList();
                Assert.Equal(users?.Count, 2);
                Assert.Equal(users?[0].TenantId, 1);
                Assert.Equal(users?[1].TenantId, 2);
            }

            try
            {
                users = db.Users.ToList();
                Assert.Equal(users?.Count, 1);
                Assert.Equal(users?[0].TenantId, 2);
                using (var filterManager2 = db.CreateFilterManager())
                {
                    filterManager2.DisableFilter<TenantFilter>();

                    users = db.Users.ToList();
                    Assert.Equal(users?.Count, 2);
                }
            }
            catch (Exception ex)
            {
                Assert.True(ex is InvalidOperationException);
            }
        }
    }
}
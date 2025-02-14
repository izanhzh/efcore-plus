using EfCorePlus.Filters.Standard;
using EfCorePlus.Test.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace EfCorePlus.Test
{
    /// <summary>
    /// 特别注意：
    /// 目前发现Sqlite内存数据库的导航属性查询机制有点特别，当一个实体查询或并加载了导航属性后，再次查询时不会走数据库查询，直接从内存中取数据，还是维持了原来的数据，会导致全局过滤器和预期不符
    /// 可能相同的原因：https://github.com/dotnet/efcore/issues/19649
    /// 因此在测试导航属性全局过滤时，需要注意这个问题，尽量让实体只查询过一次
    /// </summary>
    public class FilterTest : TestBase
    {
        public FilterTest(ServiceProviderFixture serviceProviderFixture) : base(serviceProviderFixture)
        {
            SeedTestData();
        }

        private void SeedTestData()
        {
            using var db = CreateTestDbContext();

            // softDeleteTestData
            {
                var softDeleteTestData1 = new SoftDeleteTestData
                {
                    Data = "Data1",
                    IsDeleted = true
                };
                var softDeleteTestData2 = new SoftDeleteTestData
                {
                    Data = "Data2",
                    IsDeleted = false
                };
                db.SoftDeleteTestDatas.AddRange(softDeleteTestData1, softDeleteTestData2);
                db.SaveChanges();
            }

            // tanentTestData
            {
                var tanentTestData1 = new TanentTestData
                {
                    Data = "Data1",
                    TenantId = 1
                };
                var tanentTestData2 = new TanentTestData
                {
                    Data = "Data2",
                    TenantId = 2
                };
                db.TanentTestDatas.AddRange(tanentTestData1, tanentTestData2);
                db.SaveChanges();
            }

            // activeTestData
            {
                var activeTestData1 = new ActiveTestData
                {
                    Data = "Data1",
                    IsActive = true
                };
                var activeTestData2 = new ActiveTestData
                {
                    Data = "Data2",
                    IsActive = false
                };
                db.ActiveTestDatas.AddRange(activeTestData1, activeTestData2);
                db.SaveChanges();
            }

            // blog
            {
                var blog1 = new Blog
                {
                    Name = "Blog1",
                    TenantId = 1,
                    IsDeleted = false,
                    Posts = new List<Post>
                {
                    new Post
                    {
                        Title = "Blog1Post1",
                        IsDeleted = false
                    },
                    new Post
                    {
                        Title = "Blog1Post2",
                        IsDeleted = true
                    }
                }
                };
                var blog2 = new Blog
                {
                    Name = "Blog2",
                    TenantId = 1,
                    IsDeleted = true,
                    Posts = new List<Post>
                {
                    new Post
                    {
                        Title = "Blog2Post1",
                        IsDeleted = false
                    },
                    new Post
                    {
                        Title = "Blog2Post2",
                        IsDeleted = true
                    }
                }
                };
                var blog3 = new Blog
                {
                    Name = "Blog3",
                    TenantId = 2,
                    IsDeleted = false,
                    Posts = new List<Post>
                {
                    new Post
                    {
                        Title = "Blog3Post1",
                        IsDeleted = false
                    },
                    new Post
                    {
                        Title = "Blog3Post2",
                        IsDeleted = true
                    }
                }
                };
                var blog4 = new Blog
                {
                    Name = "Blog4",
                    TenantId = 2,
                    IsDeleted = true,
                    Posts = new List<Post>
                {
                    new Post
                    {
                        Title = "Blog4Post1",
                        IsDeleted = false
                    },
                    new Post
                    {
                        Title = "Blog4Post2",
                        IsDeleted = true
                    }
                }
                };
                db.Blogs.AddRange(blog1, blog2, blog3, blog4);
                db.SaveChanges();
            }

            // user
            {
                var user1 = new User
                {
                    Name = "User1",
                    TenantId = 1,
                    IsDeleted = false
                };
                var user2 = new User
                {
                    Name = "User2",
                    TenantId = 1,
                    IsDeleted = true
                };
                var user3 = new User
                {
                    Name = "User3",
                    TenantId = 2,
                    IsDeleted = false
                };
                var user4 = new User
                {
                    Name = "User4",
                    TenantId = 2,
                    IsDeleted = true
                };
                db.Users.AddRange(user1, user2, user3, user4);
                db.SaveChanges();
            }
        }

        [Fact]
        public void SoftDeleteFilterTest()
        {
            using var db = CreateTestDbContext();
            var datas = db.SoftDeleteTestDatas.ToList();
            Assert.Single(datas);
            Assert.False(datas?[0].IsDeleted);
        }

        [Fact]
        public void TanentFilterTest()
        {
            using var db = CreateTestDbContext();

            var datas = db.TanentTestDatas.ToList();
            Assert.Empty(datas);

            db.CurrentTenantId = 1;
            datas = db.TanentTestDatas.ToList();
            Assert.Single(datas);
            Assert.Equal(1, datas?[0].TenantId);

            db.CurrentTenantId = 2;
            datas = db.TanentTestDatas.ToList();
            Assert.Single(datas);
            Assert.Equal(2, datas?[0].TenantId);
        }

        [Fact]
        public void ActiveFilterTest()
        {
            using var db = CreateTestDbContext();
            var datas = db.ActiveTestDatas.ToList();
            Assert.Single(datas);
            Assert.True(datas?[0].IsActive);
        }

        [Fact]
        public void NavigationPropertyTest()
        {
            using var db = CreateTestDbContext();
            db.CurrentTenantId = 1;
            var blogs = db.Blogs.ToList();
            Assert.Single(blogs);
            Assert.Equal("Blog1", blogs?[0].Name);
            Assert.Equal(1, blogs?[0].Posts?.Count);
            Assert.Equal("Blog1Post1", blogs?[0].Posts?[0].Title);

            db.CurrentTenantId = 2;
            blogs = db.Blogs.ToList();
            Assert.Single(blogs);
            Assert.Equal("Blog3", blogs?[0].Name);
            Assert.Equal(1, blogs?[0].Posts?.Count);
            Assert.Equal("Blog3Post1", blogs?[0].Posts?[0].Title);
        }

        [Fact]
        public void FilterManager_AllEntity_Test()
        {
            using var db = CreateTestDbContext();
            db.CurrentTenantId = 1;

            var blogs = db.Blogs.ToList();
            Assert.Single(blogs);
            Assert.False(blogs?[0].IsDeleted);
            Assert.Equal(1, blogs?[0].TenantId);

            var users = db.Users.ToList();
            Assert.Single(users);
            Assert.False(users?[0].IsDeleted);
            Assert.Equal(1, users?[0].TenantId);

            using (var fm = db.CreateFilterManager())
            {
                {
                    fm.DisableFilter<SoftDeleteFilter>();

                    blogs = db.Blogs.ToList();
                    Assert.Equal(2, blogs.Count);
                    Assert.Contains(blogs, x => x.IsDeleted);
                    Assert.Contains(blogs, x => !x.IsDeleted);
                    Assert.All(blogs, x => Assert.Equal(1, x.TenantId));

                    users = db.Users.ToList();
                    Assert.Equal(2, users.Count);
                    Assert.Contains(users, x => x.IsDeleted);
                    Assert.Contains(users, x => !x.IsDeleted);
                    Assert.All(users, x => Assert.Equal(1, x.TenantId));
                }

                {
                    fm.DisableFilter<TenantFilter>();

                    blogs = db.Blogs.ToList();
                    Assert.Equal(4, blogs.Count);
                    Assert.Contains(blogs, x => x.IsDeleted);
                    Assert.Contains(blogs, x => !x.IsDeleted);
                    Assert.Contains(blogs, x => x.TenantId == 1);
                    Assert.Contains(blogs, x => x.TenantId == 2);

                    users = db.Users.ToList();
                    Assert.Equal(4, users.Count);
                    Assert.Contains(users, x => x.IsDeleted);
                    Assert.Contains(users, x => !x.IsDeleted);
                    Assert.Contains(users, x => x.TenantId == 1);
                    Assert.Contains(users, x => x.TenantId == 2);
                }

                {
                    fm.EnableFilter<SoftDeleteFilter>();

                    blogs = db.Blogs.ToList();
                    Assert.Equal(2, blogs.Count);
                    Assert.All(blogs, x => Assert.False(x.IsDeleted));
                    Assert.Contains(blogs, x => x.TenantId == 1);
                    Assert.Contains(blogs, x => x.TenantId == 2);

                    users = db.Users.ToList();
                    Assert.Equal(2, users.Count);
                    Assert.All(users, x => Assert.False(x.IsDeleted));
                    Assert.Contains(users, x => x.TenantId == 1);
                    Assert.Contains(users, x => x.TenantId == 2);
                }

                {
                    fm.EnableFilter<TenantFilter>();

                    blogs = db.Blogs.ToList();
                    Assert.Single(blogs);
                    Assert.False(blogs?[0].IsDeleted);
                    Assert.Equal(1, blogs?[0].TenantId);

                    users = db.Users.ToList();
                    Assert.Single(users);
                    Assert.False(users?[0].IsDeleted);
                    Assert.Equal(1, users?[0].TenantId);
                }

                fm.DisableFilter<SoftDeleteFilter>();
                fm.DisableFilter<TenantFilter>();
            }

            blogs = db.Blogs.ToList();
            Assert.Single(blogs);
            Assert.False(blogs?[0].IsDeleted);
            Assert.Equal(1, blogs?[0].TenantId);

            users = db.Users.ToList();
            Assert.Single(users);
            Assert.False(users?[0].IsDeleted);
            Assert.Equal(1, users?[0].TenantId);

            using (var fm = db.CreateFilterManager())
            {
                {
                    fm.DisableFilter(typeof(SoftDeleteFilter));
                    fm.DisableFilter(typeof(TenantFilter));

                    blogs = db.Blogs.ToList();
                    Assert.Contains(blogs, x => x.IsDeleted);
                    Assert.Contains(blogs, x => !x.IsDeleted);
                    Assert.Contains(blogs, x => x.TenantId == 1);
                    Assert.Contains(blogs, x => x.TenantId == 2);

                    users = db.Users.ToList();
                    Assert.Contains(users, x => x.IsDeleted);
                    Assert.Contains(users, x => !x.IsDeleted);
                    Assert.Contains(users, x => x.TenantId == 1);
                    Assert.Contains(users, x => x.TenantId == 2);
                }

                {
                    fm.EnableFilter(typeof(SoftDeleteFilter));
                    fm.EnableFilter(typeof(TenantFilter));

                    blogs = db.Blogs.ToList();
                    Assert.False(blogs?[0].IsDeleted);
                    Assert.Equal(1, blogs?[0].TenantId);

                    users = db.Users.ToList();
                    Assert.False(users?[0].IsDeleted);
                    Assert.Equal(1, users?[0].TenantId);
                }
            }
        }

        [Fact]
        public void FilterManager_SpecificEntity_Test()
        {
            using var db = CreateTestDbContext();
            db.CurrentTenantId = 1;

            var blogs = db.Blogs.ToList();
            Assert.Single(blogs);
            Assert.False(blogs?[0].IsDeleted);
            Assert.Equal(1, blogs?[0].TenantId);

            var users = db.Users.ToList();
            Assert.Single(users);
            Assert.False(users?[0].IsDeleted);
            Assert.Equal(1, users?[0].TenantId);

            using (var fm = db.CreateFilterManager())
            {
                {
                    fm.DisableFilter<SoftDeleteFilter>(typeof(User));

                    blogs = db.Blogs.ToList();
                    Assert.Single(blogs);
                    Assert.False(blogs?[0].IsDeleted);
                    Assert.Equal(1, blogs?[0].TenantId);

                    users = db.Users.ToList();
                    Assert.Equal(2, users.Count);
                    Assert.Contains(users, x => x.IsDeleted);
                    Assert.Contains(users, x => !x.IsDeleted);
                    Assert.All(users, x => Assert.Equal(1, x.TenantId));
                }

                {
                    fm.DisableFilter<TenantFilter>(typeof(Blog));

                    blogs = db.Blogs.ToList();
                    Assert.Equal(2, blogs.Count);
                    Assert.Contains(blogs, x => x.TenantId == 1);
                    Assert.Contains(blogs, x => x.TenantId == 2);
                    Assert.All(blogs, x => Assert.False(x.IsDeleted));

                    users = db.Users.ToList();
                    Assert.Equal(2, users.Count);
                    Assert.Contains(users, x => x.IsDeleted);
                    Assert.Contains(users, x => !x.IsDeleted);
                    Assert.All(users, x => Assert.Equal(1, x.TenantId));
                }

                {
                    fm.EnableFilter<SoftDeleteFilter>(typeof(User));

                    blogs = db.Blogs.ToList();
                    Assert.Equal(2, blogs.Count);
                    Assert.Contains(blogs, x => x.TenantId == 1);
                    Assert.Contains(blogs, x => x.TenantId == 2);
                    Assert.All(blogs, x => Assert.False(x.IsDeleted));

                    users = db.Users.ToList();
                    Assert.Single(users);
                    Assert.False(blogs?[0].IsDeleted);
                    Assert.Equal(1, blogs?[0].TenantId);
                }

                {
                    fm.EnableFilter<TenantFilter>(typeof(Blog));

                    blogs = db.Blogs.ToList();
                    Assert.Single(blogs);
                    Assert.False(blogs?[0].IsDeleted);
                    Assert.Equal(1, blogs?[0].TenantId);

                    users = db.Users.ToList();
                    Assert.Single(users);
                    Assert.False(blogs?[0].IsDeleted);
                    Assert.Equal(1, blogs?[0].TenantId);
                }
            }

            blogs = db.Blogs.ToList();
            Assert.Single(blogs);
            Assert.False(blogs?[0].IsDeleted);
            Assert.Equal(1, blogs?[0].TenantId);

            users = db.Users.ToList();
            Assert.Single(users);
            Assert.False(blogs?[0].IsDeleted);
            Assert.Equal(1, blogs?[0].TenantId);

            using (var fm = db.CreateFilterManager())
            {
                {
                    fm.DisableFilter(typeof(SoftDeleteFilter), typeof(User).FullName!);
                    fm.DisableFilter(typeof(TenantFilter), typeof(Blog).FullName!);

                    blogs = db.Blogs.ToList();
                    Assert.Equal(2, blogs.Count);
                    Assert.All(blogs, x => Assert.False(x.IsDeleted));
                    Assert.Contains(blogs, x => x.TenantId == 1);
                    Assert.Contains(blogs, x => x.TenantId == 2);

                    users = db.Users.ToList();
                    Assert.Equal(2, users.Count);
                    Assert.Contains(users, x => x.IsDeleted);
                    Assert.Contains(users, x => !x.IsDeleted);
                    Assert.All(users, x => Assert.Equal(1, x.TenantId));
                }

                {
                    fm.EnableFilter(typeof(SoftDeleteFilter), typeof(User).FullName!);
                    fm.EnableFilter(typeof(TenantFilter), typeof(Blog).FullName!);

                    blogs = db.Blogs.ToList();
                    Assert.Single(blogs);
                    Assert.False(blogs?[0].IsDeleted);
                    Assert.Equal(1, blogs?[0].TenantId);

                    users = db.Users.ToList();
                    Assert.Single(users);
                    Assert.False(blogs?[0].IsDeleted);
                    Assert.Equal(1, blogs?[0].TenantId);
                }
            }
        }

        [Fact]
        public void FilterManager_InvalidOperationException_Test()
        {
            using var db = CreateTestDbContext();

            using (var fm = db.CreateFilterManager())
            {
                Assert.Throws<InvalidOperationException>(() => db.CreateFilterManager());

                fm.DisableFilter<SoftDeleteFilter>();
                Assert.Throws<InvalidOperationException>(() => fm.DisableFilter<SoftDeleteFilter>(typeof(User)));
                Assert.Throws<InvalidOperationException>(() => fm.EnableFilter<SoftDeleteFilter>(typeof(User)));
                fm.EnableFilter<SoftDeleteFilter>();
                fm.DisableFilter<SoftDeleteFilter>(typeof(User));
                Assert.Throws<InvalidOperationException>(() => fm.DisableFilter<SoftDeleteFilter>());

                fm.DisableFilter<TenantFilter>(typeof(Blog));
                Assert.Throws<InvalidOperationException>(() => fm.DisableFilter<TenantFilter>());
                Assert.Throws<InvalidOperationException>(() => fm.EnableFilter<TenantFilter>());
                fm.EnableFilter<TenantFilter>(typeof(Blog));
                fm.DisableFilter<TenantFilter>();
                Assert.Throws<InvalidOperationException>(() => fm.DisableFilter<TenantFilter>(typeof(Blog)));
            }
        }
    }
}
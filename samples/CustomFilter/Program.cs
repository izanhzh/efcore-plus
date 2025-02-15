// See https://aka.ms/new-console-template for more information

// 实例化DbContext
using CustomFilter.EntityFrameworkCore;
using CustomFilter.EntityFrameworkCore.Entities;
using CustomFilter.EntityFrameworkCore.Filters;
using EfCorePlus;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

Thread.CurrentThread.CurrentCulture = new CultureInfo("zh-CN");

using var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();

var dbContextOptionsBuilder = new DbContextOptionsBuilder<MyDbContext>()
    .UseSqlite(connection)
    .AddEfCorePlus(options =>
    {
        options.RegisterFilter<LanguageFilter>();
    });
var options = dbContextOptionsBuilder.Options;
var context = new MyDbContext(options);
context.Database.EnsureCreated();

var testData1 = new TestData
{
    Name = "Test1",
    Language = "en"
};
var testData2 = new TestData
{
    Name = "Test2",
    Language = "zh-CN"
};
context.TestDatas.AddRange(testData1, testData2);
context.SaveChanges();

var testDatas = context.TestDatas.ToList();

Thread.CurrentThread.CurrentCulture = new CultureInfo("en");

testDatas = context.TestDatas.ToList();

Console.WriteLine("Hello, World!");

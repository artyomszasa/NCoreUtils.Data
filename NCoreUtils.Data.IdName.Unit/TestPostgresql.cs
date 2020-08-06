using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace NCoreUtils.Data
{
    public class TestPostgresql : TestBase
    {
        public TestPostgresql()
            : base((conf, builder) => builder.UseNpgsql(
                "Host=192.168.1.254; Port=5432; Username=unit; Password=096398f13; Database=unit",
                b => b.SetPostgresVersion(9, 6)
            ))
        { }

        [Fact]
        public override Task InsertOne() => base.InsertOne();

        [Fact]
        public override Task InsertTwo() => base.InsertTwo();

        [Fact]
        public override Task InsertOneWithForeign() => base.InsertOneWithForeign();

        [Fact]
        public override Task InsertTwoWithForeign() => base.InsertTwoWithForeign();

        [Fact]
        public override void InsertOneSync() => base.InsertOneSync();
    }
}
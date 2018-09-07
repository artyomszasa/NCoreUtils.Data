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
            : base((conf, builder) => builder.UseNpgsql(conf.GetConnectionString("Psql")))
        { }

        [Fact]
        public override Task InsertOne() => base.InsertOne();

        [Fact]
        public override Task InsertTwo() => base.InsertTwo();
    }
}
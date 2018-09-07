using Microsoft.EntityFrameworkCore;

namespace NCoreUtils.Data
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.HasGetIdNameSuffixFunction();

            builder.Entity<Item>(b =>
            {
                b.HasKey(e => e.Id);
                b.HasIdName(e => e.IdName, e => e.Name);
                b.Property(e => e.Name).HasMaxLength(320).IsUnicode(true).IsRequired(true);
            });

            base.OnModelCreating(builder);
        }
    }
}
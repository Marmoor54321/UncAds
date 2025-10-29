using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UncAds.Models;

namespace UncAds.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Ad> Ads { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<AdCategory> AdCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // konfiguracja klucza złożonego dla tabeli łącznikowej
            builder.Entity<AdCategory>()
                .HasKey(ac => new { ac.AdId, ac.CategoryId });

            builder.Entity<AdCategory>()
                .HasOne(ac => ac.Ad)
                .WithMany(a => a.AdCategories)
                .HasForeignKey(ac => ac.AdId);

            builder.Entity<AdCategory>()
                .HasOne(ac => ac.Category)
                .WithMany(c => c.AdCategories)
                .HasForeignKey(ac => ac.CategoryId);

            // konfiguracja drzewa kategorii (opcjonalnie)
            builder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict); // zapobiega kaskadowemu usuwaniu drzewa
        }
    }
}

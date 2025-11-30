using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UncAds.Models;

namespace UncAds.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Ad> Ads { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<AdCategory> AdCategories { get; set; }
        public DbSet<CategoryAttribute> CategoryAttributes { get; set; }
        public DbSet<AdAttributeValue> AdAttributeValues { get; set; }
        public DbSet<AdMedia> AdMedia { get; set; }
        public DbSet<AdAttachment> AdAttachments { get; set; }
        public DbSet<AdminSettings> AdminSettings { get; set; }
        public DbSet<AttributeDictionary> AttributeDictionaries { get; set; }
        public DbSet<ForbiddenWord> ForbiddenWords { get; set; }
        public DbSet<AdReport> AdReports { get; set; }
        public DbSet<UserCategorySubscription> UserCategorySubscriptions { get; set; }



        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Ad>()
                    .HasOne(a => a.User)       // Ogłoszenie ma jednego Usera
                    .WithMany()                // User ma wiele ogłoszeń (jeśli masz listę Ads w ApplicationUser wpisz .WithMany(u => u.Ads))
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
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

            builder.Entity<CategoryAttribute>()
                .HasOne(ca => ca.Category)
                .WithMany(c => c.CategoryAttributes)
                .HasForeignKey(ca => ca.CategoryId);

            builder.Entity<AdAttributeValue>()
                .HasOne(aav => aav.Ad)
                .WithMany(a => a.AttributeValues)
                .HasForeignKey(aav => aav.AdId);

            builder.Entity<AdAttributeValue>()
                .HasOne(aav => aav.CategoryAttribute)
                .WithMany()
                .HasForeignKey(aav => aav.CategoryAttributeId);

            builder.Entity<UserCategorySubscription>()
            .HasKey(t => new { t.UserId, t.CategoryId });

            builder.Entity<UserCategorySubscription>()
                .HasOne(pt => pt.User)
                .WithMany(p => p.CategorySubscriptions)
                .HasForeignKey(pt => pt.UserId);

            builder.Entity<UserCategorySubscription>()
                .HasOne(pt => pt.Category)
                .WithMany() // Kategoria nie musi wiedzieć o subskrybentach bezpośrednio
                .HasForeignKey(pt => pt.CategoryId);
        }
    }
}

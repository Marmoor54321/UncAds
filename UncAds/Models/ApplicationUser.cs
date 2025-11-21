using Microsoft.AspNetCore.Identity;

namespace UncAds.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }

        public int AdsPerPage { get; set; } = 10;

        public DateTime? LastNewsletterSent { get; set; } // Kiedy ostatnio dostał maila
        public ICollection<UserCategorySubscription>? CategorySubscriptions { get; set; }
    }
}

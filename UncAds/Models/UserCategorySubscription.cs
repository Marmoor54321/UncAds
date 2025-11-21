namespace UncAds.Models
{
    public class UserCategorySubscription
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
}
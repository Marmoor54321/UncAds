namespace UncAds.Models
{
    public class AdCategory
    {
        public int AdId { get; set; }
        public Ad Ad { get; set; } = null!;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
    }
}

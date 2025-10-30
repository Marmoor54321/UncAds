using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UncAds.Models
{
    public class AdMedia
    {
        public int Id { get; set; }

        [Required]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        public string MediaType { get; set; } = string.Empty;

        [ForeignKey("Ad")]
        public int AdId { get; set; }
        public Ad Ad { get; set; }
    }
}

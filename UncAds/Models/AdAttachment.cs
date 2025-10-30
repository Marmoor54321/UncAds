using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UncAds.Models
{
    public class AdAttachment
    {
        public int Id { get; set; }

        [Required]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        [ForeignKey("Ad")]
        public int AdId { get; set; }
        public Ad Ad { get; set; }
    }
}

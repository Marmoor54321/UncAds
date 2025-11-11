using System.ComponentModel.DataAnnotations;

namespace UncAds.Models
{
    public class ForbiddenWord
    {
        public int Id { get; set; }

        [Required]
        public string Word { get; set; } = string.Empty;
    }
}

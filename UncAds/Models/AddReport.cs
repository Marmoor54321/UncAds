using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UncAds.Models
{
    public class AdReport
    {
        public int Id { get; set; }

        [Required]
        public int AdId { get; set; }
        public Ad Ad { get; set; }

        [Required]
        public string ReporterId { get; set; }
        public ApplicationUser Reporter { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; }

        public DateTime ReportDate { get; set; } = DateTime.Now;

        public bool Resolved { get; set; } = false;
        public string? ResolutionNote { get; set; }
    }
}

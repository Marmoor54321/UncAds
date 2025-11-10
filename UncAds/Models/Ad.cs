using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace UncAds.Models
{
    public class Ad
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Title")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "Date")]
        public DateTime Date { get; set; } = DateTime.Now;

        [ForeignKey("User")]
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public ICollection<AdCategory>? AdCategories { get; set; }
        public ICollection<AdAttributeValue>? AttributeValues { get; set; }

        public ICollection<AdMedia>? Media { get; set; }
        public ICollection<AdAttachment>? Attachments { get; set; }

    }
}

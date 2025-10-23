using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        //[Display(Name = "Author")]
        //public string AuthorId { get; set; }

        //[ForeignKey("AuthorId")]
        //public virtual User Author { get; set; }
    }
}

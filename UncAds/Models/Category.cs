using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UncAds.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        // do budowania drzewa kategorii (nullable dla rootów)
        public int? ParentCategoryId { get; set; }
        public Category? ParentCategory { get; set; }
        public ICollection<Category>? Children { get; set; }

        // relacja wiele-do-wielu z Ad (przez tabelę łącznikową)
        public ICollection<AdCategory>? AdCategories { get; set; }
    }
}

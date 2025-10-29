using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UncAds.Models
{
    public class Category
    {
        //funkcja pomocnicza do wyświetlania pełnej ścieżki kategorii
        [NotMapped]
        public string FullPath
        {
            get
            {
                var names = new List<string>();
                var current = this;
                while (current != null)
                {
                    names.Insert(0, current.Name);
                    current = current.ParentCategory;
                }
                return string.Join(" > ", names);
            }
        }
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

        public ICollection<CategoryAttribute>? CategoryAttributes { get; set; }

    }
}

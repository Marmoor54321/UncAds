using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UncAds.Models
{
    public class Category
    {

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


        public int? ParentCategoryId { get; set; }
        public Category? ParentCategory { get; set; }
        public ICollection<Category>? Children { get; set; }


        public ICollection<AdCategory>? AdCategories { get; set; }

        public ICollection<CategoryAttribute>? CategoryAttributes { get; set; }

    }
}

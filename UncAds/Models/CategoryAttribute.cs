using System.ComponentModel.DataAnnotations;

namespace UncAds.Models
{
    public class CategoryAttribute
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public AttributeType Type { get; set; } // np. tekst, liczba, data, lista

        // np. lista dopuszczalnych wartości, JSON lub CSV
        public string? Options { get; set; }

        // relacja z kategorią
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }

    public enum AttributeType
    {
        Text,
        Number,
        Boolean,
        Date,
        Select
    }

 
        public class AdAttributeValue
        {
            public int Id { get; set; }

            public int AdId { get; set; }
            public Ad Ad { get; set; } = null!;

            public int CategoryAttributeId { get; set; }
            public CategoryAttribute CategoryAttribute { get; set; } = null!;

            [Required]
            public string Value { get; set; } = string.Empty;
        }
    

}

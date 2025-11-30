using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UncAds.Models
{
    public class CategoryAttribute
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public AttributeType Type { get; set; } 


        public string? Options { get; set; }


        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int? DictionaryId { get; set; }
        public AttributeDictionary? Dictionary { get; set; }


        public bool AllowMultiple { get; set; } = false;
    }

    public enum AttributeType
    {
        Text,
        Number,
        Boolean,
        Date,
        Select,
        Dictionary
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
    public class AttributeDictionary
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public ICollection<AttributeDictionaryValue> Values { get; set; } = new List<AttributeDictionaryValue>();
    }

    public class AttributeDictionaryValue
    {
        public int Id { get; set; }

        [Required]
        public string Value { get; set; } = string.Empty;

        [ForeignKey(nameof(Dictionary))]
        public int DictionaryId { get; set; }

        public AttributeDictionary? Dictionary { get; set; }
    }

}

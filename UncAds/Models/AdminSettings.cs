using System.ComponentModel.DataAnnotations;

namespace UncAds.Models
{
    public class AdminSettings
    {
        public int Id { get; set; }

        [Display(Name = "Maksymalna liczba załączników na ogłoszenie")]
        public int MaxAttachments { get; set; } = 5;

        [Display(Name = "Maksymalny rozmiar pliku (MB)")]
        public int MaxFileSizeMB { get; set; } = 10;

        [Display(Name = "Maksymalna liczba plików multimedialnych na ogłoszenie")]
        public int MaxMediaFiles { get; set; } = 5;
    }

}

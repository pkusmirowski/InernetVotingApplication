using System.ComponentModel.DataAnnotations;

namespace InternetVotingApplication.ViewModels
{
    public class ElectionFormViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Podaj nazwę wyborów")]
        [StringLength(200)]
        [Display(Name = "Nazwa wyborów")]
        public string Opis { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj datę rozpoczęcia")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data rozpoczęcia")]
        public DateTime? DataRozpoczecia { get; set; }

        [Required(ErrorMessage = "Podaj datę zakończenia")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data zakończenia")]
        public DateTime? DataZakonczenia { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DataRozpoczecia.HasValue && DataZakonczenia.HasValue && DataZakonczenia <= DataRozpoczecia)
            {
                yield return new ValidationResult("Data zakończenia musi być późniejsza niż data rozpoczęcia.", [nameof(DataZakonczenia)]);
            }
        }
    }
}

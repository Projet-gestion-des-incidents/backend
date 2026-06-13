using System.ComponentModel.DataAnnotations;

namespace projet0.Application.Commun.DTOs
{
    public class AdminUpdateTechnicienDto
    {
        
        [MinLength(3, ErrorMessage = "Le nom d'utilisateur doit contenir au moins 3 caractères")]
        [MaxLength(30, ErrorMessage = "Le nom d'utilisateur ne peut pas dépasser 30 caractères")]
        public string? UserName { get; set; }

        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string? Email { get; set; }

        
        [MinLength(3, ErrorMessage = "Le nom doit contenir au moins 3 caractères")]
        [MaxLength(30, ErrorMessage = "Le nom ne peut pas dépasser 30 caractères")]
        public string? Nom { get; set; }

        
        [MinLength(3, ErrorMessage = "Le prénom doit contenir au moins 3 caractères")]
        [MaxLength(30, ErrorMessage = "Le prénom ne peut pas dépasser 30 caractères")]
        public string? Prenom { get; set; }

        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [RegularExpression(@"^[0-9]{8}$", ErrorMessage = "Le numéro de téléphone doit contenir exactement 8 chiffres")]
        public string? PhoneNumber { get; set; }

        
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }        

        public string? Image { get; set; }

        // Validation personnalisée pour l'âge (18 ans minimum)
        public static ValidationResult? ValidateAge(DateTime? birthDate, ValidationContext context)
        {
            if (!birthDate.HasValue)
                return new ValidationResult("La date de naissance est requise");

            var today = DateTime.Today;
            var age = today.Year - birthDate.Value.Year;
            if (birthDate.Value.Date > today.AddYears(-age)) age--;

            if (age < 18)
                return new ValidationResult("L'utilisateur doit avoir au moins 18 ans");

            if (age > 120)
                return new ValidationResult("La date de naissance n'est pas valide");

            // Empêcher les dates dans le futur
            if (birthDate.Value.Date > today)
                return new ValidationResult("La date de naissance ne peut pas être dans le futur");

            return ValidationResult.Success;
        }
    }
}

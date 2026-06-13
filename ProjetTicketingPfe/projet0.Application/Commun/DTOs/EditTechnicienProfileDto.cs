using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace projet0.Application.Commun.DTOs
{
    public class EditTechnicienProfileDto
    {
        [MinLength(3, ErrorMessage = "Le nom doit contenir au moins 3 caractères")]
        [MaxLength(30, ErrorMessage = "Le nom ne peut pas dépasser 30 caractères")]
        public string? Nom { get; set; }  

        [MinLength(3, ErrorMessage = "Le prénom doit contenir au moins 3 caractères")]
        [MaxLength(30, ErrorMessage = "Le prénom ne peut pas dépasser 30 caractères")]
        public string? Prenom { get; set; }  

        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string? Email { get; set; }  

        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [RegularExpression(@"^[0-9]{8}$", ErrorMessage = "Le numéro de téléphone doit contenir exactement 8 chiffres")]
        public string? PhoneNumber { get; set; }  

        [DataType(DataType.Date)]
        [CustomValidation(typeof(EditTechnicienProfileDto), nameof(ValidateAge))]
        public DateTime? BirthDate { get; set; }  

        public string? Image { get; set; }  

        // Champs pour changement de mot de passe (optionnels)
        public string? CurrentPassword { get; set; }

        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        [CustomValidation(typeof(EditTechnicienProfileDto), nameof(ValidatePassword))]
        public string? NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public string? ConfirmPassword { get; set; }

        // Validation personnalisée pour l'âge 
        public static ValidationResult? ValidateAge(DateTime? birthDate, ValidationContext context)
        {
            if (!birthDate.HasValue)
                return ValidationResult.Success;  

            var today = DateTime.Today;
            var age = today.Year - birthDate.Value.Year;
            if (birthDate.Value.Date > today.AddYears(-age)) age--;

            if (age < 18)
                return new ValidationResult("Vous devez avoir au moins 18 ans");

            if (age > 120)
                return new ValidationResult("Date de naissance invalide");

            if (birthDate.Value.Date > today)
                return new ValidationResult("La date de naissance ne peut pas être dans le futur");

            return ValidationResult.Success;
        }

        // Validation personnalisée pour le mot de passe
        public static ValidationResult? ValidatePassword(string? password, ValidationContext context)
        {
            if (string.IsNullOrEmpty(password))
                return ValidationResult.Success;  

            var errors = new List<string>();

            if (password.Length < 6)
                errors.Add("Le mot de passe doit contenir au moins 6 caractères");

            if (!Regex.IsMatch(password, @"\d"))
                errors.Add("Le mot de passe doit contenir au moins un chiffre");

            if (!Regex.IsMatch(password, @"[a-z]"))
                errors.Add("Le mot de passe doit contenir au moins une lettre minuscule");

            if (!Regex.IsMatch(password, @"[A-Z]"))
                errors.Add("Le mot de passe doit contenir au moins une lettre majuscule");

            if (errors.Any())
                return new ValidationResult(string.Join(" | ", errors));

            return ValidationResult.Success;
        }
    }
}

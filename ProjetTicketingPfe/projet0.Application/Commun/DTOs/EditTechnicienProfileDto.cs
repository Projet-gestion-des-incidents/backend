using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace projet0.Application.Commun.DTOs
{
    public class EditTechnicienProfileDto
    {
        [Required(ErrorMessage = "Le nom est requis")]
        [MinLength(2, ErrorMessage = "Le nom doit contenir au moins 2 caractères")]
        [MaxLength(50, ErrorMessage = "Le nom ne peut pas dépasser 50 caractères")]
        public string Nom { get; set; }

        [Required(ErrorMessage = "Le prénom est requis")]
        [MinLength(2, ErrorMessage = "Le prénom doit contenir au moins 2 caractères")]
        [MaxLength(50, ErrorMessage = "Le prénom ne peut pas dépasser 50 caractères")]
        public string Prenom { get; set; }

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [RegularExpression(@"^[0-9]{8}$", ErrorMessage = "Le numéro de téléphone doit contenir exactement 8 chiffres")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "La date de naissance est requise")]
        [DataType(DataType.Date)]
        [CustomValidation(typeof(EditTechnicienProfileDto), nameof(ValidateAge))]
        public DateTime? BirthDate { get; set; }

        public string? Image { get; set; }

        // Champs pour changement de mot de passe (optionnels)
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }

        // Validation personnalisée pour l'âge
        public static ValidationResult? ValidateAge(DateTime? birthDate, ValidationContext context)
        {
            if (!birthDate.HasValue)
                return new ValidationResult("La date de naissance est requise");

            var today = DateTime.Today;
            var age = today.Year - birthDate.Value.Year;
            if (birthDate.Value.Date > today.AddYears(-age)) age--;

            if (age < 18)
                return new ValidationResult("Vous devez avoir au moins 18 ans");

            if (age > 120)
                return new ValidationResult("Date de naissance invalide");

            return ValidationResult.Success;
        }
    }
}

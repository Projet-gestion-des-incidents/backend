using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace projet0.Application.Commun.DTOs
{
    public class RegisterDTO
    {
        [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
        [MinLength(3, ErrorMessage = "Le nom d'utilisateur doit contenir au moins 3 caractères")]
        [MaxLength(30, ErrorMessage = "Le nom d'utilisateur ne peut pas dépasser 30 caractères")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        [CustomValidation(typeof(RegisterDTO), nameof(ValidatePassword))]
        public string Password { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        [MinLength(3, ErrorMessage = "Le nom doit contenir au moins 3 caractères")]
        [MaxLength(30, ErrorMessage = "Le nom ne peut pas dépasser 30 caractères")]
        public string Nom { get; set; }

        [Required(ErrorMessage = "Le prénom est requis")]
        [MinLength(3, ErrorMessage = "Le prénom doit contenir au moins 3 caractères")]
        [MaxLength(30, ErrorMessage = "Le prénom ne peut pas dépasser 30 caractères")]
        public string Prenom { get; set; }

        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [RegularExpression(@"^[0-9]{8}$", ErrorMessage = "Le numéro de téléphone doit contenir exactement 8 chiffres")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "La date de naissance est requise")]
        [DataType(DataType.Date)]
        [CustomValidation(typeof(RegisterDTO), nameof(ValidateAge))]
        public DateTime? BirthDate { get; set; }

        // Validation personnalisée pour le mot de passe
        public static ValidationResult? ValidatePassword(string? password, ValidationContext context)
        {
            if (string.IsNullOrEmpty(password))
                return new ValidationResult("Le mot de passe est requis");

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

        // Validation personnalisée pour l'âge (18 ans minimum)
        public static ValidationResult? ValidateAge(DateTime? birthDate, ValidationContext context)
        {
            if (!birthDate.HasValue)
                return new ValidationResult("La date de naissance est requise");

            var today = DateTime.Today;
            var age = today.Year - birthDate.Value.Year;
            if (birthDate.Value.Date > today.AddYears(-age)) age--;

            if (age < 18)
                return new ValidationResult("Vous devez avoir au moins 18 ans pour vous inscrire");

            if (age > 120)
                return new ValidationResult("La date de naissance n'est pas valide");

            return ValidationResult.Success;
        }
    }
}


using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace projet0.Application.Commun.DTOs
{
    public class EditCommercantProfileDto
    {
        [MinLength(2, ErrorMessage = "Le nom du magasin doit contenir au moins 2 caractères")]
        [MaxLength(20, ErrorMessage = "Le nom du magasin ne peut pas dépasser 20 caractères")]
        public string? NomMagasin { get; set; }  

        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string? Email { get; set; }  

        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [RegularExpression(@"^[0-9]{8}$", ErrorMessage = "Le numéro de téléphone doit contenir exactement 8 chiffres")]
        public string? PhoneNumber { get; set; }  

        [MaxLength(200, ErrorMessage = "L'adresse ne peut pas dépasser 200 caractères")]
        public string? Adresse { get; set; }  

        public string? Image { get; set; }  

        // Champs pour changement de mot de passe (optionnels)
        public string? CurrentPassword { get; set; }

        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        [CustomValidation(typeof(EditCommercantProfileDto), nameof(ValidatePassword))]
        public string? NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public string? ConfirmPassword { get; set; }

        // Validation personnalisée pour le mot de passe (optionnelle)
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

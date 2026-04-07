using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace projet0.Application.Commun.DTOs
{
    // Dans Application/Commun/DTOs/CreateCommercantDto.cs
    public class CreateCommercantDto
    {
        [Required(ErrorMessage = "Le nom du magasin est requis")]
        [MinLength(3, ErrorMessage = "Le nom du magasin doit contenir au moins 3 caractères")]
        [MaxLength(50, ErrorMessage = "Le nom du magasin ne peut pas dépasser 50 caractères")]
        public string NomMagasin { get; set; }  // Sera utilisé comme UserName

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [RegularExpression(@"^[0-9]{8}$", ErrorMessage = "Le numéro de téléphone doit contenir exactement 8 chiffres")]
        public string? PhoneNumber { get; set; }

        [MaxLength(200, ErrorMessage = "L'adresse ne peut pas dépasser 200 caractères")]
        public string? Adresse { get; set; }
    }
}

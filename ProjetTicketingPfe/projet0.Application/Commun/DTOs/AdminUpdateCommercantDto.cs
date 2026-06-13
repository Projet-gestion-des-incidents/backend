using System.ComponentModel.DataAnnotations;

namespace projet0.Application.Commun.DTOs
{
    public class AdminUpdateCommercantDto
    {
        
        [MinLength(2, ErrorMessage = "Le nom du magasin doit contenir au moins 2 caractères")]
        [MaxLength(20, ErrorMessage = "Le nom du magasin ne peut pas dépasser 20 caractères")]
        public string? NomMagasin { get; set; }  // Correspond à UserName

        
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string? Email { get; set; }

        
        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [RegularExpression(@"^[0-9]{8}$", ErrorMessage = "Le numéro de téléphone doit contenir exactement 8 chiffres")]
        public string? PhoneNumber { get; set; }


        [MaxLength(200, ErrorMessage = "L'adresse ne peut pas dépasser 200 caractères")]
        public string? Adresse { get; set; }

        public string? Image { get; set; }

    }
}

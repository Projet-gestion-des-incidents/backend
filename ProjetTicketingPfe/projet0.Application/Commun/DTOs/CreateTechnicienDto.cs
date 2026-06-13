using System.ComponentModel.DataAnnotations;

namespace projet0.Application.Commun.DTOs
{
    public class CreateTechnicienDto
    {
        [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
        [MinLength(3, ErrorMessage = "Le nom d'utilisateur doit contenir au moins 3 caractères")]
        [MaxLength(30, ErrorMessage = "Le nom d'utilisateur ne peut pas dépasser 30 caractères")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        [MinLength(3, ErrorMessage = "Le nom doit contenir au moins 3 caractères")]
        [MaxLength(30, ErrorMessage = "Le nom ne peut pas dépasser 30 caractères")]
        public string Nom { get; set; }

        [Required(ErrorMessage = "Le prénom est requis")]
        [MinLength(3, ErrorMessage = "Le prénom doit contenir au moins 3 caractères")]
        [MaxLength(30, ErrorMessage = "Le prénom ne peut pas dépasser 30 caractères")]
        public string Prenom { get; set; }

    }
}

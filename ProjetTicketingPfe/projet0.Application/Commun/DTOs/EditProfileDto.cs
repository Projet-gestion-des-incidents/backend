using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs
{
    public class EditProfileDto
    {
        // Informations de base
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Nom { get; set; }
        public string? Prenom { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? BirthDate { get; set; }

        // Changement de mot de passe
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }

        // Image de profil (Base64)
        public string? Image { get; set; }
    }
}

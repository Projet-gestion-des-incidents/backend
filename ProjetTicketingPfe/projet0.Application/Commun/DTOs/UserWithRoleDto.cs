using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs
{
    public class UserWithRoleDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }   // optionnel selon besoin
        public string Email { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string PhoneNumber { get; set; }
        public string Image { get; set; } // si tu veux gérer les avatars
        public string Role { get; set; }
    }
}

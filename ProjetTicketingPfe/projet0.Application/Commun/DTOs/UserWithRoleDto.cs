using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs
{
    public class UserWithRoleDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } 
        public string Email { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string PhoneNumber { get; set; }
        public string Image { get; set; } 
        public string Role { get; set; }
        public Guid? RoleId { get; set; }
        public UserStatut Statut { get; set; } = UserStatut.Actif;
        public DateTime? BirthDate { get; set; }

    }
}

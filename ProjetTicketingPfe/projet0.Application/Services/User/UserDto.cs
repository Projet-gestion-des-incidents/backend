using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text;

namespace projet0.Application.Services.User
{
    public class UserDto
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }

        public string? PhoneNumber { get; set; }

        public Guid RoleId { get; set; }

        public string? Image { get; set; }

        public DateTime? BirthDate { get; set; }
        public UserStatut Statut { get; set; }
        public string StatutString => Statut.ToString(); // Pour affichage simple

    }

}

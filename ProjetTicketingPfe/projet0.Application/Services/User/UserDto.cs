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
        public string Password { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }

        public string? PhoneNumber { get; set; }

        public Guid RoleId { get; set; }

        public string? Image { get; set; }

        public DateTime? BirthDate { get; set; }

    }

}

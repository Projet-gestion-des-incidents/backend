using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs
{
    public class RegisterDTO
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Nom { get; set; }     
        public string Prenom { get; set; }  
        public int Age { get; set; }      
        public bool EmailConfirmed { get; set; } = false;
        public string RoleId { get; set; }

    }
}


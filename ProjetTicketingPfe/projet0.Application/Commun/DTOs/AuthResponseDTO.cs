using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs
{
    public class AuthResponseDTO
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }  
        public DateTime ExpiresAt { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; } 
        public bool EmailConfirmed { get; set; }
        public string Role { get; set; }

    }

}

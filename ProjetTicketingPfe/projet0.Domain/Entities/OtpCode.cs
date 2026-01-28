using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace projet0.Domain.Entities
{
    public class OtpCode
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string Code { get; set; }   // 6 chiffres par ex
        public DateTime CreatedAt { get; set; }
        public DateTime ExpireAt { get; set; }
        public OtpStatus Status { get; set; }
        public OtpPurpose Purpose { get; set; }
    }
}




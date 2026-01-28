using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Domain.Enums
{
    public enum OtpPurpose
    {
        EmailConfirmation = 0,
        PhoneConfirmation = 1,
        ResetPassword = 2,
        TwoFactor = 3
    }
}

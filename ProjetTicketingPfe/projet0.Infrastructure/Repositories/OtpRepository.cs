using Microsoft.EntityFrameworkCore;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using projet0.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Infrastructure.Repositories
{
    public class OtpRepository : IOtpRepository
    {
        private readonly ApplicationDbContext _context;

        public OtpRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(OtpCode otp)
        {
            await _context.OtpCodes.AddAsync(otp);
            await _context.SaveChangesAsync();
        }

        public async Task<OtpCode> GetValidOtpAsync(Guid userId, string code, OtpPurpose purpose)
        {
            return await _context.OtpCodes
                .FirstOrDefaultAsync(o =>
                    o.UserId == userId &&
                    o.Code == code &&
                    o.Purpose == purpose &&
                    o.Status == OtpStatus.Generated &&
                    o.ExpireAt > DateTime.UtcNow);
        }

        public async Task UpdateAsync(OtpCode otp)
        {
            _context.OtpCodes.Update(otp);
            await _context.SaveChangesAsync();
        }
    }
}

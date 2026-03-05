
using projet0.Application.Commun.DTOs;
using projet0.Application.Commun.Ressources;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace projet0.Application.Services.TPE
{
    public interface ITPEService
    {
        Task<ApiResponse<TPEDto>> CreateAsync(CreateTPEDto dto);
        Task<ApiResponse<TPEDto>> UpdateAsync(Guid id, UpdateTPEDto dto);
        Task<ApiResponse<string>> DeleteAsync(Guid id);
        Task<ApiResponse<TPEDto>> GetByIdAsync(Guid id);
        Task<ApiResponse<IEnumerable<TPEDto>>> GetByCommercantIdAsync(Guid commercantId);
        Task<ApiResponse<IEnumerable<TPEDto>>> GetAllAsync();
        
    }
}
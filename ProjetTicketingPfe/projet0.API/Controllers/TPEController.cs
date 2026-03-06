using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projet0.Application.Commun.DTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Services.TPE;
using System;
using System.Threading.Tasks;

namespace projet0.API.Controllers
{
    [ApiController]
    [Route("api/tpe")]
    [Authorize]
    public class TPEController : ControllerBase
    {
        private readonly ITPEService _tpeService;

        public TPEController(ITPEService tpeService)
        {
            _tpeService = tpeService;
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(CreateTPEDto dto)
        {
            var result = await _tpeService.CreateAsync(dto);
            return result.ResultCode == 0 ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(Guid id, UpdateTPEDto dto)
        {
            var result = await _tpeService.UpdateAsync(id, dto);
            return result.ResultCode == 0 ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _tpeService.DeleteAsync(id);
            return result.ResultCode == 0 ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _tpeService.GetByIdAsync(id);
            return result.ResultCode == 0 ? Ok(result) : NotFound(result);
        }

        [HttpGet("commercant/{commercantId}")]
        [Authorize(Policy = "UserRead")]
        public async Task<IActionResult> GetByCommercantId(Guid commercantId)
        {
            var result = await _tpeService.GetByCommercantIdAsync(commercantId);
            return result.ResultCode == 0 ? Ok(result) : BadRequest(result);
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _tpeService.GetAllAsync();
            return Ok(result);
        }

    }
}
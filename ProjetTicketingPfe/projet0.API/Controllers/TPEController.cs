using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;  
using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Services.TPE;
using System;
using System.Threading.Tasks;
using System.Security.Claims;


namespace projet0.API.Controllers
{
    [ApiController]
    [Route("api/tpe")]
    [Authorize]
    public class TPEController : ControllerBase
    {
        private readonly ITPEService _tpeService;
        private readonly ILogger<TPEController> _logger;

        public TPEController(ITPEService tpeService, ILogger<TPEController> logger)
        {
            _tpeService = tpeService;
            _logger = logger;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                return userId;

            throw new UnauthorizedAccessException("Utilisateur non authentifié");
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(CreateTPEDto dto)
        {
            var userId = GetCurrentUserId(); // Récupérer l'ID de l'admin connecté
            var result = await _tpeService.CreateAsync(dto, userId);
            return result.ResultCode == 0 ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(Guid id, UpdateTPEDto dto)
        {
            var userId = GetCurrentUserId(); // Récupérer l'ID de l'admin connecté
            var result = await _tpeService.UpdateAsync(id, dto, userId);
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

        /// <summary>
        /// Récupère la liste paginée des TPEs avec filtres
        /// </summary>
        /// <param name="request">Paramètres de pagination et filtres</param>
        /// <returns>Liste paginée des TPEs</returns>
        [HttpGet("withFilters")]
        [Authorize(Policy = "UserRead")] // Ajustez la politique selon vos besoins
        public async Task<ActionResult<ApiResponse<PagedResult<TPEDto>>>> GetTPEsPaged(
            [FromQuery] TPEPagedRequest request)
        {
            try
            {
                _logger.LogInformation("Récupération paginée des TPEs - Page: {Page}, PageSize: {PageSize}",
                    request.Page, request.PageSize);

                var result = await _tpeService.GetTPEsPagedAsync(request);

                if (!result.IsSuccess)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des TPEs");
                return StatusCode(500, ApiResponse<PagedResult<TPEDto>>.Failure(
                    "Erreur interne du serveur"));
            }
        }

        /// <summary>
        /// Récupère les statistiques du dashboard TPE (taux de panne par modèle et par adresse)
        /// </summary>
        [HttpGet("dashboard")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<TPEDashboardDTO>>> GetTPEDashboard()
        {
            try
            {
                _logger.LogInformation("Récupération du dashboard TPE");

                var result = await _tpeService.GetTPEDashboardAsync();

                if (!result.IsSuccess)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dashboard TPE");
                return StatusCode(500, ApiResponse<TPEDashboardDTO>.Failure("Erreur interne du serveur"));
            }
        }
    }
}
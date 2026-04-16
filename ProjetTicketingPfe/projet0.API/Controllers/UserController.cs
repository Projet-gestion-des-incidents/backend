using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using projet0.API.Filters;
using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Services.User;
using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace projet0.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;

        }

        



       

        [HttpPut("{id}/activate")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Activate(Guid id)
        {
            var result = await _userService.ActivateAsync(id);
            return Ok(result);
        }

        [HttpDelete("desactivate/{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Desactivate(Guid id)
        {
            var result = await _userService.DesactivateAsync(id);
            return Ok(result);
        }

        // API/Controllers/UserController.cs

        // API/Controllers/UserController.cs

        /// <summary>
        /// Modifier le profil de l'administrateur (par l'admin lui-même)
        /// </summary>
        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> EditAdminProfile([FromBody] EditAdminProfileDto dto)
        {
            // ✅ 1. Valider le modèle (DataAnnotations)
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<ApplicationUser>.Failure(
                    message: "Données invalides",
                    errors: errors,
                    resultCode: 99));
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Utilisateur non identifié");

            var roles = await _userService.GetUserRolesAsync(userId);
            if (!roles.Contains("Admin"))
            {
                return BadRequest(ApiResponse<ApplicationUser>.Failure(
                    message: "Cette API est réservée aux administrateurs",
                    errors: null,
                    resultCode: 99));
            }

            var response = await _userService.EditAdminProfileAsync(userId, dto);

            if (response.ResultCode == 42 || response.ResultCode == 43)
                return Ok(response);

            if (response.ResultCode != 0)
                return BadRequest(response);

            return Ok(response.Data);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (userIdClaim == null)
                return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);

            // 1. Récupérer l'utilisateur
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            // 2. Récupérer les rôles
            var roles = await _userService.GetUserRolesAsync(userId);
            var role = roles.FirstOrDefault() ?? "USER";

            // 3. Créer un objet anonyme avec l'utilisateur + le rôle
            var result = new
            {
                // Propriétés de l'utilisateur
                user.Nom,
                user.Prenom,
                user.Image,
                user.BirthDate,
                user.Statut,
                user.Adresse,  // ✅ AJOUTER CETTE LIGNE

                user.TicketsCrees,
                user.TicketsAssignes,
                user.Commentaires,
                user.PiecesJointes,
                user.Notifications,
                user.HistoriquesModifies,
                user.IncidentLiaisons,
                user.TPEs,
                user.Id,
                user.UserName,
                user.NormalizedUserName,
                user.Email,
                user.NormalizedEmail,
                user.EmailConfirmed,
                user.PasswordHash,
                user.SecurityStamp,
                user.ConcurrencyStamp,
                user.PhoneNumber,
                user.PhoneNumberConfirmed,
                user.TwoFactorEnabled,
                user.LockoutEnd,
                user.LockoutEnabled,
                user.AccessFailedCount,
                Role = role
            };

            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _userService.DeleteAsync(id);
            return Ok(result);
        }
        
        [HttpGet("search")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> SearchUsers([FromQuery] UserSearchRequest request)
        {
            // La validation est faite automatiquement par le modèle
            var result = await _userService.SearchUsersAsync(request);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }
        // projet0.API/Controllers/UserController.cs


        // Dans UserController.cs - Remplacer l'ancienne méthode GetTechniciens

        /// <summary>
        /// Récupère la liste paginée des techniciens avec recherche et filtres
        /// </summary>
        [HttpGet("techniciens")]
        [Authorize(Policy = "UserRead")]
        public async Task<ActionResult<ApiResponse<PagedResult<TechnicienDto>>>> GetTechniciensPaged(
            [FromQuery] TechnicienSearchRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(ApiResponse<PagedResult<TechnicienDto>>.Failure(
                        "Utilisateur non authentifié"));
                }

                var userRoles = await _userService.GetUserRolesAsync(userId);
                var isAdmin = userRoles.Contains("Admin");
                var isTechnicien = userRoles.Contains("Technicien");

                _logger.LogInformation("Récupération paginée des techniciens par {UserId} (Admin: {IsAdmin}) - Page: {Page}, SearchTerm: {SearchTerm}",
                    userId, isAdmin, request.Page, request.SearchTerm);

                var result = await _userService.GetTechniciensPagedAsync(request);

                if (!result.IsSuccess)
                    return BadRequest(result);

                // Si c'est un technicien qui consulte, on masque son propre profil
                if (!isAdmin && isTechnicien && result.Data.Items.Any())
                {
                    var filteredItems = result.Data.Items.Where(t => t.Id != userId).ToList();
                    var filteredResult = new PagedResult<TechnicienDto>
                    {
                        Items = filteredItems,
                        TotalCount = filteredItems.Count,
                        Page = request.Page,
                        PageSize = request.PageSize
                    };

                    _logger.LogInformation("Technicien connecté: exclusion de lui-même. {Count} techniciens restants",
                        filteredItems.Count);

                    return Ok(ApiResponse<PagedResult<TechnicienDto>>.Success(
                        data: filteredResult,
                        message: $"{filteredItems.Count} technicien(s) trouvé(s)",
                        resultCode: 0));
                }

                // Ajouter les en-têtes de pagination
                Response.Headers.Append("X-Pagination-TotalCount", result.Data.TotalCount.ToString());
                Response.Headers.Append("X-Pagination-Page", result.Data.Page.ToString());
                Response.Headers.Append("X-Pagination-PageSize", result.Data.PageSize.ToString());
                Response.Headers.Append("X-Pagination-TotalPages",
                    Math.Ceiling((double)result.Data.TotalCount / result.Data.PageSize).ToString());

                return Ok(new
                {
                    Data = result.Data.Items,
                    Pagination = new
                    {
                        result.Data.Page,
                        result.Data.PageSize,
                        result.Data.TotalCount,
                        TotalPages = (int)Math.Ceiling((double)result.Data.TotalCount / result.Data.PageSize),
                        result.Data.HasPreviousPage,
                        result.Data.HasNextPage
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des techniciens");
                return StatusCode(500, ApiResponse<PagedResult<TechnicienDto>>.Failure(
                    "Erreur interne du serveur"));
            }
        }
        // Dans UserController.cs

        /// <summary>
        /// Créer un technicien (par admin)
        /// </summary>
        [HttpPost("technicien")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateTechnicien([FromBody] CreateTechnicienDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ApplicationUser>.Failure(
                    message: "Données invalides",
                    errors: errors,
                    resultCode: 99));
            }

            var result = await _userService.CreateTechnicienAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Créer un commerçant (magasin) par admin
        /// </summary>
        [HttpPost("commercant")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateCommercant([FromBody] CreateCommercantDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ApplicationUser>.Failure(
                    message: "Données invalides",
                    errors: errors,
                    resultCode: 99));
            }

            var result = await _userService.CreateCommercantAsync(dto);
            return Ok(result);
        }

        // Dans UserController.cs

        /// <summary>
        /// Récupère la liste paginée des commerçants avec recherche et filtres
        /// </summary>
        [HttpGet("commercants")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<PagedResult<CommercantDto>>>> GetCommercantsPaged(
            [FromQuery] CommercantSearchRequest request)
        {
            try
            {
                _logger.LogInformation("Récupération paginée des commerçants - Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}",
                    request.Page, request.PageSize, request.SearchTerm);

                var result = await _userService.GetCommercantsPagedAsync(request);

                if (!result.IsSuccess)
                    return BadRequest(result);

                // Ajouter les en-têtes de pagination
                Response.Headers.Append("X-Pagination-TotalCount", result.Data.TotalCount.ToString());
                Response.Headers.Append("X-Pagination-Page", result.Data.Page.ToString());
                Response.Headers.Append("X-Pagination-PageSize", result.Data.PageSize.ToString());
                Response.Headers.Append("X-Pagination-TotalPages",
                    Math.Ceiling((double)result.Data.TotalCount / result.Data.PageSize).ToString());

                return Ok(new
                {
                    Data = result.Data.Items,
                    Pagination = new
                    {
                        result.Data.Page,
                        result.Data.PageSize,
                        result.Data.TotalCount,
                        TotalPages = (int)Math.Ceiling((double)result.Data.TotalCount / result.Data.PageSize),
                        result.Data.HasPreviousPage,
                        result.Data.HasNextPage
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des commerçants");
                return StatusCode(500, ApiResponse<PagedResult<CommercantDto>>.Failure(
                    "Erreur interne du serveur"));
            }
        }

        // Dans UserController.cs

        //// <summary>
        /// Modifier le profil d'un technicien (par le technicien lui-même)
        /// </summary>
        [Authorize]
        [HttpPut("me/technicien")]
        public async Task<IActionResult> EditTechnicienProfile([FromBody] EditTechnicienProfileDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            // Vérifier le rôle
            var roles = await _userService.GetUserRolesAsync(userId);
            if (!roles.Contains("Technicien"))
                return BadRequest(ApiResponse<ApplicationUser>.Failure(
                    message: "Cette API est réservée aux techniciens",
                    errors: null,
                    resultCode: 99));

            var result = await _userService.EditTechnicienProfileAsync(userId, dto);

            // ✅ Gérer les codes de retour spécifiques
            if (result.ResultCode == 42) // Email change - OTP envoyé
                return Ok(result);

            if (result.ResultCode == 43) // Password change - OTP envoyé
                return Ok(result);

            return result.ResultCode == 0 ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Modifier le profil d'un commerçant (par le commerçant lui-même)
        /// </summary>
        [Authorize]
        [HttpPut("me/commercant")]
        public async Task<IActionResult> EditCommercantProfile([FromBody] EditCommercantProfileDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            // Vérifier le rôle
            var roles = await _userService.GetUserRolesAsync(userId);
            if (!roles.Contains("Commercant"))
                return BadRequest(ApiResponse<ApplicationUser>.Failure(
                    message: "Cette API est réservée aux commerçants",
                    errors: null,
                    resultCode: 99));

            var result = await _userService.EditCommercantProfileAsync(userId, dto);

            if (result.ResultCode == 42) // Email change - OTP envoyé
                return Ok(result);

            if (result.ResultCode == 43) // Password change - OTP envoyé
                return Ok(result);

            return result.ResultCode == 0 ? Ok(result) : BadRequest(result);
        }

        // Dans UserController.cs

        /// <summary>
        /// Admin - Modifier un technicien
        /// </summary>
        [HttpPut("technicien/{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> AdminUpdateTechnicien(Guid id, [FromBody] AdminUpdateTechnicienDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ApplicationUser>.Failure(
                    message: "Données invalides",
                    errors: errors,
                    resultCode: 99));
            }

            var result = await _userService.AdminUpdateTechnicienAsync(id, dto);
            return result.ResultCode == 0 ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Admin - Modifier un commerçant (magasin)
        /// </summary>
        [HttpPut("commercant/{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> AdminUpdateCommercant(Guid id, [FromBody] AdminUpdateCommercantDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ApplicationUser>.Failure(
                    message: "Données invalides",
                    errors: errors,
                    resultCode: 99));
            }

            var result = await _userService.AdminUpdateCommercantAsync(id, dto);
            return result.ResultCode == 0 ? Ok(result) : BadRequest(result);
        }

        // API/Controllers/UserController.cs

        /// <summary>
        /// Récupère un technicien par son ID
        /// </summary>
        [HttpGet("technicien/{id}")]
        [Authorize(Policy = "UserRead")]
        public async Task<IActionResult> GetTechnicienById(Guid id)
        {
            try
            {
                // Vérifier les droits d'accès
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
                {
                    return Unauthorized(ApiResponse<TechnicienDto>.Failure(
                        message: "Utilisateur non authentifié",
                        errors: null,
                        resultCode: 401));
                }

                var userRoles = await _userService.GetUserRolesAsync(currentUserId);
                var isAdmin = userRoles.Contains("Admin");
                var isTechnicien = userRoles.Contains("Technicien");

                // Un technicien ne peut voir que son propre profil
                if (!isAdmin && isTechnicien && currentUserId != id)
                {
                    return Forbid();
                }

                var result = await _userService.GetTechnicienByIdAsync(id);
                return result.ResultCode == 0 ? Ok(result) : NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du technicien {Id}", id);
                return StatusCode(500, ApiResponse<TechnicienDto>.Failure(
                    message: "Erreur interne du serveur",
                    errors: null,
                    resultCode: 500));
            }
        }

        /// <summary>
        /// Récupère un commerçant par son ID
        /// </summary>
        [HttpGet("commercant/{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetCommercantById(Guid id)
        {
            try
            {
                var result = await _userService.GetCommercantByIdAsync(id);
                return result.ResultCode == 0 ? Ok(result) : NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du commerçant {Id}", id);
                return StatusCode(500, ApiResponse<CommercantDto>.Failure(
                    message: "Erreur interne du serveur",
                    errors: null,
                    resultCode: 500));
            }
        }

    }

}

    

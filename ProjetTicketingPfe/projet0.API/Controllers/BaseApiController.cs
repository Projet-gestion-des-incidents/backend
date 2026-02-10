using Microsoft.AspNetCore.Mvc;
using projet0.Application.Common.Models.Pagination;

namespace projet0.API.Controllers.Base
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        // Méthode helper pour les réponses paginées
        protected IActionResult PagedResult<T>(PagedResult<T> result)
        {
            // Ajouter les headers de pagination (optionnel)
            Response.Headers.Append("X-Pagination-TotalCount", result.TotalCount.ToString());
            Response.Headers.Append("X-Pagination-Page", result.Page.ToString());
            Response.Headers.Append("X-Pagination-PageSize", result.PageSize.ToString());
            Response.Headers.Append("X-Pagination-TotalPages", result.TotalPages.ToString());

            return Ok(new
            {
                Data = result.Items,
                Pagination = new
                {
                    result.Page,
                    result.PageSize,
                    result.TotalCount,
                    result.TotalPages,
                    result.HasPreviousPage,
                    result.HasNextPage
                }
            });
        }

        // Pour les réponses API standard
        protected IActionResult ApiResult<T>(T data, string? message = null)
        {
            return Ok(new
            {
                Success = true,
                Data = data,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
        }

        // Pour les erreurs
        protected IActionResult ApiError(string message, int statusCode = 400)
        {
            return StatusCode(statusCode, new
            {
                Success = false,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
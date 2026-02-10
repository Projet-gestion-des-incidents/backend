using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using projet0.Application.Common.Models.Pagination;

namespace projet0.API.Filters
{
    public class ValidatePaginationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Cherche tous les paramètres qui héritent de PagedRequest
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument is PagedRequest request)
                {
                    // Validation
                    if (request.Page < 1)
                    {
                        context.Result = new BadRequestObjectResult("Page doit être supérieure ou égale à 1");
                        return;
                    }

                    if (request.PageSize < 1 || request.PageSize > 100)
                    {
                        context.Result = new BadRequestObjectResult("PageSize doit être entre 1 et 100");
                        return;
                    }

                    // Normalisation
                    if (string.IsNullOrWhiteSpace(request.SortBy))
                    {
                        request.SortBy = "Id"; // Valeur par défaut
                    }

                    // Limiter la taille du SearchTerm
                    if (!string.IsNullOrEmpty(request.SearchTerm) && request.SearchTerm.Length > 100)
                    {
                        request.SearchTerm = request.SearchTerm.Substring(0, 100);
                    }
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Rien à faire après l'exécution
        }
    }
}
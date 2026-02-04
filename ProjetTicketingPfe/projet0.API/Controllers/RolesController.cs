using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using projet0.Application.Commun.DTOs;
using projet0.Application.Commun.Ressources;

namespace projet0.API.Controllers
{
    [ApiController]
    [Route("api/roles")]
    public class RolesController : ControllerBase
    {
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public RolesController(RoleManager<IdentityRole<Guid>> roleManager)
        {
            _roleManager = roleManager;
        }


        [HttpGet("register")]
        [AllowAnonymous]
        public IActionResult GetRolesForRegister()
        {
            var roles = _roleManager.Roles
     .Where(r => r.Name != "Admin")
     .Select(r => new RoleDto
     {
         Id = r.Id,
         Name = r.Name
     })
     .ToList();


            return Ok(ApiResponse<List<RoleDto>>.Success(
                data: roles,
                message: "Roles disponibles",
                resultCode: 0
            ));
        }
    }

}

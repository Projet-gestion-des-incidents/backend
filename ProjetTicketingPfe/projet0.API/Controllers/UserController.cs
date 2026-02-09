using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using projet0.Application.Commun.DTOs;
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
    [Authorize] //  Auth obligatoire par défaut
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        //  User / Manager / Admin
        [HttpGet]
        [Authorize(Policy = "UserRead")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        //  User / Manager / Admin
        [HttpGet("{id}")]
        [Authorize(Policy = "UserRead")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }
        // GET api/users/roles
        [HttpGet("roles")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAllWithRoles()
        {
            var users = await _userService.GetAllUsersWithRolesAsync();
            return Ok(users);
        }

        //  Admin seulement
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(UserDto dto)
        {
            var result = await _userService.CreateAsync(dto);
            return Ok(result);
        }

        //  Admin seulement
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(Guid id, UserDto dto)
        {
            var result = await _userService.UpdateAsync(id, dto);
            return Ok(result);
        }
        [HttpPut("{id}/activate")]
        [Authorize(Policy = "AdminOnly")]

        public async Task<IActionResult> Activate(Guid id)
        {
            var result = await _userService.ActivateAsync(id);
            return Ok(result);
        }

        //  Admin seulement
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _userService.DeleteAsync(id);
            return Ok(result);
        }
        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> EditProfile([FromBody] EditProfileDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Utilisateur non identifié");

            var response = await _userService.EditProfileAsync(userId, dto);

            if (response.ResultCode != 0) // <-- ici au lieu de IsSuccess
                return BadRequest(response);

            return Ok(response.Data);
        }
        [HttpGet("me")]
        [Authorize] // tout utilisateur connecté
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (userIdClaim == null)
                return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            return Ok(user);
        }
    }

}

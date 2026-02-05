using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projet0.Application.Services.User;
using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
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

        //  Admin seulement
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _userService.DeleteAsync(id);
            return Ok(result);
        }
    }
}

using EnglishCenter.API.DTOs;
using EnglishCenter.API.Models;
using EnglishCenter.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenter.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ISoftDeleteService _softDeleteService;

        public UsersController(IUserService userService, ISoftDeleteService softDeleteService)
        {
            _userService = userService;
            _softDeleteService = softDeleteService;
        }

        // GET: api/Users
        // Chỉ Admin
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<UserDto>>> GetUsers(
    string? search,
    string? role,
    string? sortBy,
    bool sortDesc = false,
    int page = 1,
    int pageSize = 20)
        {
            var users = await _userService.GetAllAsync(
                search,
                role,
                sortBy,
                sortDesc,
                page,
                pageSize);

            return Ok(users);
        }

        // GET: api/Users/1
        // Chỉ Admin
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user = await _userService.GetByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        // POST: api/Users
        // Chỉ Admin
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser(
            UserCreateDto dto)
        {
            var user = await _userService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetUser),
                new { id = user.Id },
                user);
        }

        // PUT: api/Users/1
        // Chỉ Admin
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(
            int id,
            UserUpdateDto dto)
        {
            var result =
                await _userService.UpdateAsync(id, dto);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        // DELETE: api/Users/1
        // Chỉ Admin
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result =
                await _userService.DeleteAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
        [HttpPut("{id}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restore(int id)
        {
            var result =
                await _softDeleteService.RestoreAsync<Course>(id);

            if (!result)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy khóa học đã bị xóa."
                });
            }

            return Ok(new
            {
                message = "Khôi phục khóa học thành công."
            });
        }
    }
}
using EnglishCenter.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishCenter.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public sealed class TrashController(ITrashRepository trashRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await trashRepository.GetAllAsync());

    [HttpPut("{entity}/{id:int}/restore")]
    public async Task<IActionResult> Restore(string entity, int id)
    {
        var restored = await trashRepository.RestoreAsync(entity, id);
        return restored ? Ok(new { message = "Khôi phục thành công." }) : NotFound(new { message = "Không tìm thấy bản ghi đã xóa." });
    }
}

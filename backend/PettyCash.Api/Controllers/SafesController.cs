using Microsoft.AspNetCore.Mvc;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.Safes;

namespace PettyCash.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SafesController(
    GetSafesUseCase getSafesUseCase,
    CreateSafeUseCase createSafeUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SafeDto>>> GetAll()
    {
        var safes = await getSafesUseCase.ExecuteAsync();
        return Ok(safes);
    }

    [HttpPost]
    public async Task<ActionResult<SafeDto>> Create(CreateSafeRequestDto dto)
    {
        var safe = await createSafeUseCase.ExecuteAsync(dto);
        return CreatedAtAction(nameof(GetAll), new { id = safe.Id }, safe);
    }
}

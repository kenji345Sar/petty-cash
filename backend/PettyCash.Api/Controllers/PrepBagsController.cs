using Microsoft.AspNetCore.Mvc;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.PrepBags;

namespace PettyCash.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrepBagsController(
    CreatePrepBagUseCase createPrepBagUseCase,
    GetPrepBagsUseCase getPrepBagsUseCase,
    HandOverPrepBagUseCase handOverPrepBagUseCase,
    CancelPrepBagUseCase cancelPrepBagUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PrepBagDto>>> GetAll([FromQuery] int safeId)
    {
        var bags = await getPrepBagsUseCase.ExecuteAsync(safeId);
        return Ok(bags);
    }

    [HttpPost]
    public async Task<ActionResult<PrepBagDto>> Create(CreatePrepBagRequestDto dto)
    {
        var bag = await createPrepBagUseCase.ExecuteAsync(dto);
        return CreatedAtAction(nameof(GetAll), new { id = bag.Id }, bag);
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<PrepBagDto>> Cancel(int id)
    {
        var bag = await cancelPrepBagUseCase.ExecuteAsync(id);
        return Ok(bag);
    }

    [HttpPost("{id}/handover")]
    public async Task<ActionResult<PrepBagDto>> HandOver(int id)
    {
        var bag = await handOverPrepBagUseCase.ExecuteAsync(id);
        return Ok(bag);
    }
}

using Microsoft.AspNetCore.Mvc;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.PrepBags;

namespace PettyCash.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrepBagsController(
    CreatePrepBagUseCase createPrepBagUseCase,
    GetPrepBagsUseCase getPrepBagsUseCase,
    HandOverPrepBagUseCase handOverPrepBagUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PrepBagDto>>> GetAll()
    {
        var bags = await getPrepBagsUseCase.ExecuteAsync();
        return Ok(bags);
    }

    [HttpPost]
    public async Task<ActionResult<PrepBagDto>> Create(CreatePrepBagRequestDto dto)
    {
        try
        {
            var bag = await createPrepBagUseCase.ExecuteAsync(dto);
            return CreatedAtAction(nameof(GetAll), new { id = bag.Id }, bag);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/handover")]
    public async Task<ActionResult<PrepBagDto>> HandOver(int id)
    {
        try
        {
            var bag = await handOverPrepBagUseCase.ExecuteAsync(id);
            return Ok(bag);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

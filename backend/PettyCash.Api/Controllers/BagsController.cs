using Microsoft.AspNetCore.Mvc;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.Bags;

namespace PettyCash.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BagsController(
    DepositBagUseCase depositBagUseCase,
    MoveBagToRegisterUseCase moveBagToRegisterUseCase,
    GetBagsUseCase getBagsUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ChangeBagDto>>> GetAll([FromQuery] int safeId)
    {
        var bags = await getBagsUseCase.ExecuteAsync(safeId);
        return Ok(bags);
    }

    [HttpPost("deposit")]
    public async Task<ActionResult<ChangeBagDto>> Deposit(DepositRequestDto dto)
    {
        var bag = await depositBagUseCase.ExecuteAsync(dto);
        return CreatedAtAction(nameof(GetAll), new { id = bag.Id }, bag);
    }

    [HttpPost("{id}/move")]
    public async Task<ActionResult<VendorTransactionDto>> MoveToRegister(int id)
    {
        try
        {
            var transaction = await moveBagToRegisterUseCase.ExecuteAsync(id);
            return Ok(transaction);
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

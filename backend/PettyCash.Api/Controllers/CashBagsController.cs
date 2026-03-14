using Microsoft.AspNetCore.Mvc;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.CashBags;

namespace PettyCash.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CashBagsController(
    DepositCashBagUseCase depositCashBagUseCase,
    GetCashBagsUseCase getCashBagsUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CashBagDto>>> GetAll([FromQuery] int safeId)
    {
        var bags = await getCashBagsUseCase.ExecuteAsync(safeId);
        return Ok(bags);
    }

    [HttpPost("deposit")]
    public async Task<ActionResult<CashBagDto>> Deposit(DepositRequestDto dto)
    {
        var bag = await depositCashBagUseCase.ExecuteAsync(dto);
        return CreatedAtAction(nameof(GetAll), new { id = bag.Id }, bag);
    }
}

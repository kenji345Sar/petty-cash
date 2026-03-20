using Microsoft.AspNetCore.Mvc;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.Transactions;

namespace PettyCash.Api.Controllers;

[ApiController]
[Route("api/petty-cash-transactions")]
public class PettyCashTransactionsController(
    GetPettyCashTransactionsUseCase getPettyCashTransactionsUseCase,
    CreatePettyCashTransactionUseCase createPettyCashTransactionUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PettyCashTransactionDto>>> GetAll([FromQuery] int safeId)
    {
        var transactions = await getPettyCashTransactionsUseCase.ExecuteAsync(safeId);
        return Ok(transactions);
    }

    [HttpPost]
    public async Task<ActionResult<PettyCashTransactionDto>> Create(CreatePettyCashTransactionRequestDto dto)
    {
        try
        {
            var transaction = await createPettyCashTransactionUseCase.ExecuteAsync(dto);
            return CreatedAtAction(nameof(GetAll), new { id = transaction.Id }, transaction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

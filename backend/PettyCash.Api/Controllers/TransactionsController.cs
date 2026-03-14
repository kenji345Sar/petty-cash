using Microsoft.AspNetCore.Mvc;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.Transactions;

namespace PettyCash.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController(
    GetTransactionsUseCase getTransactionsUseCase,
    CreateTransactionUseCase createTransactionUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TransactionDto>>> GetAll([FromQuery] int safeId)
    {
        var transactions = await getTransactionsUseCase.ExecuteAsync(safeId);
        return Ok(transactions);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> Create(CreateTransactionRequestDto dto)
    {
        try
        {
            var transaction = await createTransactionUseCase.ExecuteAsync(dto);
            return CreatedAtAction(nameof(GetAll), new { id = transaction.Id }, transaction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

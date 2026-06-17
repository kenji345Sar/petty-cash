using Microsoft.AspNetCore.Mvc;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.Transactions;

namespace PettyCash.Api.Controllers;

[ApiController]
[Route("api/petty-cash-transactions")]
public class PettyCashTransactionsController(
    GetPettyCashTransactionsUseCase getPettyCashTransactionsUseCase,
    CreatePettyCashTransactionUseCase createPettyCashTransactionUseCase,
    ReversePettyCashTransactionUseCase reversePettyCashTransactionUseCase) : ControllerBase
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
        var transaction = await createPettyCashTransactionUseCase.ExecuteAsync(dto);
        return CreatedAtAction(nameof(GetAll), new { id = transaction.Id }, transaction);
    }

    [HttpPost("{id}/reverse")]
    public async Task<ActionResult<PettyCashTransactionDto>> Reverse(int id, ReverseTransactionRequestDto dto)
    {
        var transaction = await reversePettyCashTransactionUseCase.ExecuteAsync(id, dto);
        return Ok(transaction);
    }
}

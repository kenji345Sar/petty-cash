using Microsoft.AspNetCore.Mvc;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.Transactions;

namespace PettyCash.Api.Controllers;

[ApiController]
[Route("api/vendor-transactions")]
public class VendorTransactionsController(
    GetVendorTransactionsUseCase getVendorTransactionsUseCase,
    ReverseVendorTransactionUseCase reverseVendorTransactionUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VendorTransactionDto>>> GetAll([FromQuery] int safeId)
    {
        var transactions = await getVendorTransactionsUseCase.ExecuteAsync(safeId);
        return Ok(transactions);
    }

    [HttpPost("{id}/reverse")]
    public async Task<ActionResult<VendorTransactionDto>> Reverse(int id, ReverseTransactionRequestDto dto)
    {
        var transaction = await reverseVendorTransactionUseCase.ExecuteAsync(id, dto);
        return Ok(transaction);
    }
}

using AlacrityCore.Models.ReqRes.Transaction;
using AlacrityCore.Services.Front;
using AlacrityServer.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AlacrityServer.Controllers;

[ApiController]
[Route("transaction")]
public class TransactionController : Controller
{
    private readonly ITransactionFrontService _transactionFrontService;
    public TransactionController(ITransactionFrontService transactionService)
        => (_transactionFrontService) = (transactionService);

    [HttpPost("deposit")]
    public async Task<DepositResponse> Deposit(DepositRequest request)
    {
        // Avoid actually validating the card information for the purposes of this project.
        var (succeeded, errorMessage) = 
            await _transactionFrontService.DepositFunds(this.GetClientId(), request.DepositAmount);

        return new()
        {
            Succeeded = succeeded,
            ErrorMessage = errorMessage
        };
    }

    [HttpPost("withdrawal")]
    public async Task<WithdrawalResponse> Withdrawal(WithdrawalRequest request)
    {
        // Avoid actually validating the card information for the purposes of this project.
        var (succeeded, errorMessage) =
            await _transactionFrontService.WithdrawFunds(this.GetClientId(), request.WithdrawalAmount);

        return new()
        {
            Succeeded = succeeded,
            ErrorMessage = errorMessage
        };
    }
}

using AlacrityCore.Enums;
using AlacrityCore.Infrastructure;
using AlacrityCore.Queries;

namespace AlacrityCore.Services.Front;

public interface ITransactionFrontService
{
    Task<(bool succeeded, string errorMessage)> DepositFunds(int clientId, decimal depositAmount);
    Task<(bool succeeded, string errorMessage)> WithdrawFunds(int clientId, decimal withdrawalAmount);
}

internal class TransactionFrontService : ITransactionFrontService
{
    private readonly IPositionsQuery _positionQuery;
    private readonly ILedgerQuery _ledgerQuery;
    private readonly IWebMessageQuery _webMessageQuery;
    private readonly ITransactionLock _transactionLock;
    public TransactionFrontService(IPositionsQuery positionsQuery, ILedgerQuery ledgerQuery, IWebMessageQuery webMessageQuery, ITransactionLock transactionLock)
        => (_positionQuery, _ledgerQuery, _webMessageQuery, _transactionLock) = (positionsQuery, ledgerQuery, webMessageQuery, transactionLock);

    public async Task<(bool succeeded, string errorMessage)> DepositFunds(int clientId, decimal depositAmount)
    {
        if (depositAmount < 0)
            return (false, "Cannot deposit negative amounts");

        var locker = _transactionLock.GetLocker(clientId);
        lock (locker)
        {
            _ledgerQuery.AddEntry(clientId, (int)SpecialInstruments.Cash, TransactionKind.Deposit, depositAmount).Wait();
            _positionQuery.AddToPosition(clientId, (int)SpecialInstruments.Cash, depositAmount, 1);            
        }

        var clientWebMessageUserId = await _webMessageQuery.GetWebMessageUserId(clientId);
        await _webMessageQuery.AddMessage(
            null, 
            "Deposit Successful",
            $@"Your deposit of ${depositAmount} has been successful, and the funds are immediately available for trading.

Best Wishes,

Alacrity Support
",
            WebMessageKind.General,
            clientWebMessageUserId.Value,
            clientWebMessageUserId.Value,
            (int)SpecialWebMessageUsers.AlacritySupport            
            );

        return (true, null);
    }

    public async Task<(bool succeeded, string errorMessage)> WithdrawFunds(int clientId, decimal withdrawalAmount)
    {
        if (withdrawalAmount < 0)
            return (false, "Cannot withdraw negative amounts");

        var cashPosition = (await _positionQuery.GetPositions(clientId))
            .FirstOrDefault(p => p.InstrumentId == (int)SpecialInstruments.Cash)
            ?.Quantity ?? 0;

        if (cashPosition - withdrawalAmount < 0)
            return (false, "Insufficient funds to perform withdrawal");

        var locker = _transactionLock.GetLocker(clientId);
        lock (locker)
        {
            _ledgerQuery.AddEntry(clientId, (int)SpecialInstruments.Cash, TransactionKind.Withdrawl, -withdrawalAmount).Wait();
            _positionQuery.AddToPosition(clientId, (int)SpecialInstruments.Cash, -withdrawalAmount, 1).Wait();
        }

        var clientWebMessageUserId = await _webMessageQuery.GetWebMessageUserId(clientId);
        await _webMessageQuery.AddMessage(
            null,
            "Withdrawal Successful",
            $@"Your withdrawal of ${withdrawalAmount} has been successful, and the funds have been credited to your account.

Best Wishes,

Alacrity Support
",
            WebMessageKind.General,
            clientWebMessageUserId.Value,
            clientWebMessageUserId.Value,
            (int)SpecialWebMessageUsers.AlacritySupport
            );

        return (true, null);
    }
}

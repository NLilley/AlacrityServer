using AlacrityCore.Models.DTOs;

namespace AlacrityCore.Models.ReqRes.Transaction;
public class WithdrawalRequest
{
    public decimal WithdrawalAmount { get; set; }
    public CardInfoDto CardInfo { get; set; }
}

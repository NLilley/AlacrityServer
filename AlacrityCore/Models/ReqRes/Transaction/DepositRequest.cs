using AlacrityCore.Models.DTOs;

namespace AlacrityCore.Models.ReqRes.Transaction;
public class DepositRequest
{
    public decimal DepositAmount { get; set; }
    public CardInfoDto CardInfo { get; set; }
}

namespace AlacrityCore.Models.DTOs;
public record AccountSummaryDto
{    
    public decimal CashBalance { get; set; }
    public decimal AccountValue { get; set; }
    public decimal ProfitLoss { get; set; }
    public int UnreadMessages { get; set; }
}

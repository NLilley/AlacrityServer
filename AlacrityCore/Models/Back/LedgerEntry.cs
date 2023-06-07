using AlacrityCore.Enums;

namespace AlacrityCore.Models.Back;
internal record LedgerEntry
{
    public DateTime CreatedDate { get; set; }
    public long LedgerId { get; set; }
    public int ClientId { get; set; }
    public long? InstrumentId { get; set; }
    public TransactionKind TransactionKind { get; set; }
    public decimal Quantity { get; set; }
}

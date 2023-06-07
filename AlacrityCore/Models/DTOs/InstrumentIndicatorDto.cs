using AlacrityCore.Enums;

namespace AlacrityCore.Models.DTOs;
public class InstrumentIndicatorDto
{
    public long InstrumentId { get; set; }
    public IndicatorKind IndicatorKind { get; set; }
    public string Name { get; set; }
    public decimal Value { get; set; }
}

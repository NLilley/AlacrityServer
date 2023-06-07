namespace AlacrityCore.Models.DTOs;
public record CandleDto
{
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }

    public void UpdateClose(decimal value)
    {
        if (value > High)
            High = value;
        else if (value < Low)
            Low = value;

        Close = value;
    }
}

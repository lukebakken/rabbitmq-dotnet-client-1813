namespace ExchangeRateManager.Dtos;

public class ExchangeRateResponseDto
{
    public string? FromCurrencyCode { get; set; }
    public string? FromCurrencyName { get; set; }
    public string? ToCurrencyCode { get; set; }
    public string? ToCurrencyName { get; set; }
    public decimal? ExchangeRate { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastRefreshed { get; set; }
    public decimal? BidPrice { get; set; }
    public decimal? AskPrice { get; set; }
}

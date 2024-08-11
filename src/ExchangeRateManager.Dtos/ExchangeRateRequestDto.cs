namespace ExchangeRateManager.Dtos;

public class ExchangeRateRequestDto
{
    public required string FromCurrencyCode { get; set; }
    public required string ToCurrencyCode { get; set; }
}

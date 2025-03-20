namespace ExchangeRateManager.Dtos;

public record ExchangeRateResponseDto(
    string? FromCurrencyCode,
    string? FromCurrencyName,
    string? ToCurrencyCode,
    string? ToCurrencyName,
    decimal? ExchangeRate,
    DateTime? CreatedAt,
    DateTime? UpdatedAt,
    DateTime? LastRefreshed,
    decimal? BidPrice,
    decimal? AskPrice)
{
    public ExchangeRateResponseDto() : this(
        default, default, default, default, default,
        default, default, default, default, default)
    { }
};

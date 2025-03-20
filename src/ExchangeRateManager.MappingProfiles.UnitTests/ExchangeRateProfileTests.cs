using AutoFixture;
using AutoMapper;
using ExchangeRateManager.ApiClients.Responses;
using ExchangeRateManager.Dtos;
using ExchangeRateManager.MappingProfiles;
using Shouldly;

namespace ExchangeRateManager.MappingProfiles.UnitTests.MappingProfiles;

/// <summary>
/// Tests the AutoMapper mapping profiles for ExchangeRate related type conversions.
/// By ensuring that <b>all maps must be tested</b> allows to trust when casting types.
/// </summary>
public class ExchangeRateProfileTests
{
    private readonly IMapper _mapper;
    private readonly Fixture _fixture = new();
    public ExchangeRateProfileTests()
    {
        _mapper = new MapperConfiguration(
            x => x.AddProfile(new ForexRateProfile()))
            .CreateMapper();
    }

    [Fact]
    public void Convert_FromGetCurrencyExchangeRatesResponse_ToExchangeRateResponseDto()
    {
        _fixture.Customize<RealtimeCurrencyExchangeRateResponse>(composer =>
            composer.With(x => x.TimeZone, "UTC"));

        var clientResponse = _fixture.Create<GetCurrencyExchangeRatesResponse>();
        var expectedDto = new ExchangeRateResponseDto
        {
            FromCurrencyCode = clientResponse.RealtimeCurrencyExchangeRate!.FromCurrencyCode,
            FromCurrencyName = clientResponse.RealtimeCurrencyExchangeRate!.FromCurrencyName,
            ToCurrencyCode = clientResponse.RealtimeCurrencyExchangeRate!.ToCurrencyCode,
            ToCurrencyName = clientResponse.RealtimeCurrencyExchangeRate!.ToCurrencyName,
            ExchangeRate = clientResponse.RealtimeCurrencyExchangeRate!.ExchangeRate,
            LastRefreshed = clientResponse.RealtimeCurrencyExchangeRate!.LastRefreshed,
            BidPrice = clientResponse.RealtimeCurrencyExchangeRate!.BidPrice,
            AskPrice = clientResponse.RealtimeCurrencyExchangeRate!.AskPrice,
        };

        var actualDto = _mapper.Map<ExchangeRateResponseDto>(clientResponse);
        expectedDto.ShouldBeEquivalentTo(actualDto);
    }
}
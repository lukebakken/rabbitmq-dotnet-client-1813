using AutoMapper;
using ExchangeRateManager.Dtos;
using ExchangeRateManager.ApiClients.Responses;
using ExchangeRateManager.Repositories.Entities;

namespace ExchangeRateManager.MappingProfiles;

/// <summary>
/// Mapping profiles for Foreign Exchange Rate related classes
/// </summary>
public class ForexRateProfile : Profile
{
	public ForexRateProfile()
    {
        //NOTE: CreateMap vs CreateProjection
        //CreateMap - for object to object mapping
        //CreateProjection - for object to ORM (EF) efficient script generation.

        CreateMap<RealtimeCurrencyExchangeRateResponse, ExchangeRateResponseDto>()
            .ForMember(x => x.LastRefreshed, options => options.MapFrom((src) =>
                TimeZoneInfo.ConvertTimeToUtc(
                    src.LastRefreshed!.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(src.TimeZone!))));

        CreateMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>()
            .IncludeMembers(p => p.RealtimeCurrencyExchangeRate);

        CreateMap<ExchangeRateRequestDto, ForexRateKey>();

        CreateMap<ForexRateEntity, ForexRateKey>();

        CreateMap<ExchangeRateResponseDto, ForexRateKey>();
        CreateMap<ExchangeRateResponseDto, ForexRateEntity>().ReverseMap();
    }
}

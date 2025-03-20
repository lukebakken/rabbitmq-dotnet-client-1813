using AutoFixture;
using AutoMapper;
using ExchangeRateManager.ApiClients.Interfaces;
using ExchangeRateManager.ApiClients.Responses;
using ExchangeRateManager.Common.Configuration;
using ExchangeRateManager.Common.Constants;
using ExchangeRateManager.Common.Exceptions;
using ExchangeRateManager.Dtos;
using ExchangeRateManager.MappingProfiles;
using ExchangeRateManager.Repositories.Entities;
using ExchangeRateManager.Repositories.Interfaces;
using ExchangeRateManager.Services.Interfaces;
using ExchangeRateManager.UnitTests.Base;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace ExchangeRateManager.Services.UnitTests;

/// <summary>
/// Tests for the ExchangeRateService
/// </summary>
public class ExchangeRateServiceTests : MapperTestBase
{
    private readonly ExchangeRateService _service;
    private readonly IForexClient _forexClient = Substitute.For<IForexClient>();
    private readonly IOptionsSnapshot<Settings> _settings = Substitute.For<IOptionsSnapshot<Settings>>();
    private readonly IForexRateRepository _forexRateRepository = Substitute.For<IForexRateRepository>();
    private readonly IBackgroundJobClient _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
    private readonly IMessageQueueService _messageQueueService = Substitute.For<IMessageQueueService>();

    public ExchangeRateServiceTests() : base(new MapperConfiguration(mc => mc.AddProfile(new ForexRateProfile())))
    {
        _service = new ExchangeRateService(
            _forexClient, _forexRateRepository,
            _messageQueueService, _backgroundJobClient,
            _settings, _mockedMapper, NullLogger<ExchangeRateService>.Instance);
    }

    #region PreferLiveData Tests
    [Fact]
    public async Task GetForexRate_PreferLiveData_ReturnsLiveData_UpdatesStored()
    {
        // Arrange
        var requestDto = _fixture.Create<ExchangeRateRequestDto>();
        var expectedClientResponse = _fixture.Create<GetCurrencyExchangeRatesResponse>();
        expectedClientResponse.RealtimeCurrencyExchangeRate!.TimeZone = "UTC";
        var expectedResponseDto = _actualMapper
            .Map<ExchangeRateResponseDto>(expectedClientResponse)
            with
        {
            CreatedAt = null,
            UpdatedAt = null,
        };

        var expectedEntity = _actualMapper.Map<ForexRateEntity>(expectedResponseDto);
        expectedEntity.CreatedAt = DateTime.UtcNow;

        ExchangeRateRequestDto? actualRequestDto = default;
        ExchangeRateResponseDto? actualResponseDto = default;
        ForexRateKey? actualForexRateKey = default;
        Settings settings = new()
        {
            ExchangeRate = new ExchangeRateSettings
            {
                PreferLiveData = true,
            }
        };

        _settings.Value.Returns(settings);

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();
        SetupMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();

        _forexClient
            .GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode)
            .Returns(expectedClientResponse)
            .AndDoes(callInfo => actualRequestDto = new ExchangeRateRequestDto
            {
                FromCurrencyCode = (string)callInfo[0],
                ToCurrencyCode = (string)callInfo[1]
            });

        _forexRateRepository
            .FindByIdAsync(Arg.Any<ForexRateKey>())
            .Returns(expectedEntity)
            .AndDoes(callInfo => actualForexRateKey = callInfo.Arg<ForexRateKey>());

        // Act
        actualResponseDto = await _service.GetForexRate(requestDto);

        // Assert
        await _forexClient
            .Received(1)
            .GetCurrencyExchangeRates(Arg.Any<string>(), Arg.Any<string>());

        await _forexRateRepository
            .Received(1)
            .FindByIdAsync(Arg.Any<ForexRateKey>());

        ReceivedMap<ExchangeRateRequestDto, ForexRateKey>(1);
        ReceivedMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>(1);

        _backgroundJobClient
            .Received(1)
            .Create(Arg.Any<Job>(), Arg.Any<IState>());

        actualRequestDto.ShouldBeEquivalentTo(requestDto);
        expectedResponseDto = expectedResponseDto with
        {
            CreatedAt = actualResponseDto.CreatedAt,
            UpdatedAt = actualResponseDto.UpdatedAt,
        };
        actualResponseDto.ShouldBeEquivalentTo(expectedResponseDto);
        actualResponseDto.CreatedAt!.Value.ShouldBeLessThan(actualResponseDto.UpdatedAt!.Value);
    }

    [Fact]
    public async Task GetCurrencyExchangeRatesResponse_PreferLiveData_BadRequest_ThrowsException()
    {
        // Arrange
        var requestDto = _fixture.Create<ExchangeRateRequestDto>();

        Settings settings = new()
        {
            ExchangeRate = new ExchangeRateSettings
            {
                PreferLiveData = true,
            }
        };

        _settings.Value.Returns(settings);

        _forexClient
            .GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode)
            .ThrowsAsync(new BadHttpRequestException("Test"));

        // Act
        var action = async () => await _service.GetForexRate(requestDto);

        // Assert
        await action.ShouldThrowAsync<InvalidExchangeRateException>();

        await _forexClient
            .Received(1).GetCurrencyExchangeRates(Arg.Any<string>(), Arg.Any<string>());

        await _forexClient
            .Received(1).GetCurrencyExchangeRates(Arg.Any<string>(), Arg.Any<string>());

        ReceivedMap<ExchangeRateRequestDto, ForexRateKey>(1);
        DidNotReceiveMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();

        _backgroundJobClient
            .DidNotReceive()
            .Create(Arg.Any<Job>(), Arg.Any<IState>());
    }

    [Fact]
    public async Task GetCurrencyExchangeRatesResponse_PreferLiveData_LimitReached_ReturnsStoredData()
    {
        // Arrange
        var requestDto = _fixture.Create<ExchangeRateRequestDto>();
        var expectedClientResponse = _fixture.Create<GetCurrencyExchangeRatesResponse>();
        expectedClientResponse.RealtimeCurrencyExchangeRate!.TimeZone = "UTC";
        var expectedResponseDto = _actualMapper.Map<ExchangeRateResponseDto>(expectedClientResponse);
        var expectedEntity = _actualMapper.Map<ForexRateEntity>(expectedResponseDto);
        expectedEntity.CreatedAt = DateTime.UtcNow;
        var expectedRateId = _actualMapper.Map<ForexRateKey>(requestDto);
        ExchangeRateRequestDto? actualRequestDto = default;
        ExchangeRateResponseDto? actualResponseDto = default;
        ForexRateKey? actualRateId = default;
        Settings settings = new()
        {
            ExchangeRate = new ExchangeRateSettings
            {
                PreferLiveData = true,
            }
        };

        _settings.Value.Returns(settings);

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();
        SetupMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();
        SetupMap<ForexRateEntity, ExchangeRateResponseDto>();

        _forexClient
            .GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode)
            .ThrowsAsync(new HttpClientLimitReachedException("Test"))
            .AndDoes(callInfo =>
            {
                actualRequestDto = new ExchangeRateRequestDto
                {
                    FromCurrencyCode = (string)callInfo[0],
                    ToCurrencyCode = (string)callInfo[1]
                };
            });

        _forexRateRepository
            .FindByIdAsync(Arg.Any<ForexRateKey>())
            .Returns(callInfo =>
            {
                actualRateId = (ForexRateKey)callInfo[0];
                return expectedEntity;
            });

        // Act
        var action = async () =>
            actualResponseDto = await _service.GetForexRate(requestDto);

        // Assert
        await action.ShouldNotThrowAsync();

        await _forexClient
            .Received(1)
            .GetCurrencyExchangeRates(Arg.Any<string>(), Arg.Any<string>());

        await _forexRateRepository
            .Received(1)
            .FindByIdAsync(Arg.Any<ForexRateKey>());

        ReceivedMap<ExchangeRateRequestDto, ForexRateKey>(1);
        ReceivedMap<ForexRateEntity, ExchangeRateResponseDto>(1);
        DidNotReceiveMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();

        _backgroundJobClient
            .DidNotReceive()
            .Create(Arg.Any<Job>(), Arg.Any<IState>());

        actualRequestDto.ShouldBeEquivalentTo(requestDto);
        expectedResponseDto = expectedResponseDto with { CreatedAt = actualResponseDto!.CreatedAt };
        expectedResponseDto = expectedResponseDto with { UpdatedAt = actualResponseDto!.UpdatedAt };
        actualResponseDto.ShouldBeEquivalentTo(expectedResponseDto);
        actualRateId.ShouldBeEquivalentTo(expectedRateId);
        actualResponseDto!.CreatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetCurrencyExchangeRatesResponse_PreferLiveData_LimitReached_NoStoredData_ThrowsError()
    {
        // Arrange
        var requestDto = _fixture.Create<ExchangeRateRequestDto>();
        var expectedRateId = _actualMapper.Map<ForexRateKey>(requestDto);
        ExchangeRateRequestDto? actualRequestDto = default;
        ForexRateKey? actualRateId = default;
        Settings settings = new()
        {
            ExchangeRate = new ExchangeRateSettings
            {
                PreferLiveData = true,
            }
        };

        _settings.Value.Returns(settings);

        _forexClient
            .GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode)
            .ThrowsAsync(new HttpClientLimitReachedException("Test"))
            .AndDoes(callInfo =>
            {
                actualRequestDto = new ExchangeRateRequestDto
                {
                    FromCurrencyCode = (string)callInfo[0],
                    ToCurrencyCode = (string)callInfo[1]
                };
            });

        _forexRateRepository
            .When(x => x.FindByIdAsync(Arg.Any<ForexRateKey>()))
            .Do(callInfo => actualRateId = (ForexRateKey)callInfo[0]);

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();

        // Act
        var action = async () => await _service.GetForexRate(requestDto);

        // Assert
        await action.ShouldThrowAsync<HttpClientLimitReachedException>();

        await _forexClient
            .Received(1)
            .GetCurrencyExchangeRates(Arg.Any<string>(), Arg.Any<string>());

        await _forexRateRepository
            .Received(1).FindByIdAsync(Arg.Any<ForexRateKey>());

        ReceivedMap<ExchangeRateRequestDto, ForexRateKey>(1);
        DidNotReceiveMap<ForexRateEntity, ExchangeRateResponseDto>();
        DidNotReceiveMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();

        _backgroundJobClient
            .DidNotReceive()
            .Create(Arg.Any<Job>(), Arg.Any<IState>());

        actualRequestDto.ShouldBeEquivalentTo(requestDto);
        actualRateId.ShouldBeEquivalentTo(expectedRateId);
    }

    [Fact]
    public async Task GetCurrencyExchangeRatesResponse_PreferLiveData_UnexpectedDatabaseError_ThrowsError()
    {
        // Arrange
        var requestDto = _fixture.Create<ExchangeRateRequestDto>();
        Settings settings = new()
        {
            ExchangeRate = new ExchangeRateSettings
            {
                PreferLiveData = true,
            }
        };

        _settings.Value.Returns(settings);

        _forexRateRepository
            .FindByIdAsync(Arg.Any<ForexRateKey>())
            .ThrowsAsync(new InvalidOperationException());

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();

        // Act
        var action = async () => await _service.GetForexRate(requestDto);

        // Assert
        await action.ShouldThrowAsync<InvalidOperationException>();

        await _forexClient
            .Received(1)
            .GetCurrencyExchangeRates(Arg.Any<string>(), Arg.Any<string>());

        await _forexRateRepository
            .Received(1).FindByIdAsync(Arg.Any<ForexRateKey>());

        ReceivedMap<ExchangeRateRequestDto, ForexRateKey>(1);
        DidNotReceiveMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();
        DidNotReceiveMap<ForexRateEntity, ExchangeRateResponseDto>();

        _backgroundJobClient
            .DidNotReceive()
            .Create(Arg.Any<Job>(), Arg.Any<IState>());
    }

    [Fact]
    public async Task GetForexRate_PreferLiveData_ReturnsLiveData_ThrowsErrorOnUpdatingStore()
    {
        // Arrange
        var requestDto = _fixture.Create<ExchangeRateRequestDto>();
        var expectedClientResponse = _fixture.Create<GetCurrencyExchangeRatesResponse>();
        expectedClientResponse.RealtimeCurrencyExchangeRate!.TimeZone = "UTC";
        var expectedResponseDto = _actualMapper.Map<ExchangeRateResponseDto>(expectedClientResponse);
        var expectedEntity = _actualMapper.Map<ForexRateEntity>(expectedResponseDto);
        expectedEntity.CreatedAt = DateTime.UtcNow;

        Settings settings = new()
        {
            ExchangeRate = new ExchangeRateSettings
            {
                PreferLiveData = true,
            }
        };

        _settings.Value.Returns(settings);

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();
        SetupMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();

        _forexClient
            .GetCurrencyExchangeRates(Arg.Any<string>(), Arg.Any<string>())
            .Returns(expectedClientResponse);

        _forexRateRepository
            .FindByIdAsync(Arg.Any<ForexRateKey>())
            .Returns(expectedEntity);

        _backgroundJobClient
            .Create(Arg.Any<Job>(), Arg.Any<IState>())
            .Throws<InvalidOperationException>();

        // Act
        var action = async () => await _service.GetForexRate(requestDto);

        // Assert
        await action.ShouldThrowAsync<InvalidOperationException>();
        await _forexClient
            .Received(1)
            .GetCurrencyExchangeRates(Arg.Any<string>(), Arg.Any<string>());

        await _forexRateRepository
            .Received(1)
            .FindByIdAsync(Arg.Any<ForexRateKey>());

        ReceivedMap<ExchangeRateRequestDto, ForexRateKey>(1);
        ReceivedMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>(1);

        _backgroundJobClient
            .Received(1)
            .Create(Arg.Any<Job>(), Arg.Any<IState>());
    }

    #endregion PreferLiveData Tests

    #region PreferStoredData Tests

    [Fact]
    public async Task GetCurrencyExchangeRatesResponse_PreferStoredData_ReturnsStoredData()
    {
        // Arrange
        var requestDto = _fixture.Create<ExchangeRateRequestDto>();
        var expectedEntity = _fixture.Create<ForexRateEntity>();
        expectedEntity.CreatedAt = DateTime.UtcNow.AddMinutes(5);
        expectedEntity.UpdatedAt = null;

        var expectedResponseDto = _actualMapper.Map<ExchangeRateResponseDto>(expectedEntity);
        var expectedRateId = _actualMapper.Map<ForexRateKey>(requestDto);
        ExchangeRateResponseDto? actualResponseDto = default;
        ForexRateKey? actualRateId = default;
        Settings settings = new()
        {
            ExchangeRate = new ExchangeRateSettings
            {
                PreferLiveData = false,
                ExpirationMinutes = 5
            }
        };

        _settings.Value.Returns(settings);

        _forexRateRepository
            .FindByIdAsync(Arg.Any<ForexRateKey>())
            .Returns(callInfo =>
            {
                actualRateId = (ForexRateKey)callInfo[0];
                return expectedEntity;
            });

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();
        SetupMap<ForexRateEntity, ExchangeRateResponseDto>();

        // Act
        actualResponseDto = await _service.GetForexRate(requestDto);

        await _forexClient
            .DidNotReceive()
            .GetCurrencyExchangeRates(Arg.Any<string>(), Arg.Any<string>());

        await _forexRateRepository
            .Received(1)
            .FindByIdAsync(Arg.Any<ForexRateKey>());

        ReceivedMap<ExchangeRateRequestDto, ForexRateKey>(1);
        ReceivedMap<ForexRateEntity, ExchangeRateResponseDto>(1);

        _backgroundJobClient
            .DidNotReceive().Create(Arg.Any<Job>(), Arg.Any<IState>());

        actualResponseDto.ShouldBeEquivalentTo(expectedResponseDto);
        actualRateId.ShouldBeEquivalentTo(expectedRateId);
    }

    [Fact]
    public async Task GetCurrencyExchangeRatesResponse_PreferStoredData_Expired_ReturnsLiveData()
    {
        // Arrange
        var requestDto = _fixture.Create<ExchangeRateRequestDto>();
        var expectedEntity = _fixture.Create<ForexRateEntity>();
        expectedEntity.CreatedAt = DateTime.UtcNow.AddMinutes(-10);
        expectedEntity.UpdatedAt = null;
        var expectedClientResponse = _fixture.Create<GetCurrencyExchangeRatesResponse>();
        expectedClientResponse.RealtimeCurrencyExchangeRate!.TimeZone = "UTC";
        var expectedResponseDto = _actualMapper.Map<ExchangeRateResponseDto>(expectedClientResponse);
        var expectedRateId = _actualMapper.Map<ForexRateKey>(requestDto);
        ExchangeRateResponseDto? actualResponseDto = default;
        ExchangeRateRequestDto? actualRequestDto = default;
        ForexRateKey? actualRateId = default;
        Settings settings = new()
        {
            ExchangeRate = new ExchangeRateSettings
            {
                PreferLiveData = false,
                ExpirationMinutes = 5
            }
        };

        _settings.Value.Returns(settings);

        _forexRateRepository
            .FindByIdAsync(Arg.Any<ForexRateKey>())
            .Returns(callInfo =>
            {
                actualRateId = (ForexRateKey)callInfo[0];
                return expectedEntity;
            });

        _forexClient
            .GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode)
            .Returns(callInfo =>
            {
                actualRequestDto = new ExchangeRateRequestDto
                {
                    FromCurrencyCode = (string)callInfo[0],
                    ToCurrencyCode = (string)callInfo[1]
                };
                return expectedClientResponse;
            });

        _backgroundJobClient
            .When(x => x.Create(Arg.Any<Job>(), Arg.Any<IState>()))
            .Do(callInfo =>
            {
                expectedResponseDto = expectedResponseDto with
                {
                    UpdatedAt = ((ExchangeRateResponseDto)((Job)callInfo[0]).Args[0]).UpdatedAt,
                    CreatedAt = ((ExchangeRateResponseDto)((Job)callInfo[0]).Args[0]).CreatedAt
                };
            });

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();
        SetupMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();

        // Act
        actualResponseDto = await _service.GetForexRate(requestDto);

        // Assert
        await _forexRateRepository
            .Received(1).FindByIdAsync(Arg.Any<ForexRateKey>());
        await _forexClient
            .Received(1).GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode);

        ReceivedMap<ExchangeRateRequestDto, ForexRateKey>(1);
        ReceivedMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>(1);
        DidNotReceiveMap<ForexRateEntity, ExchangeRateResponseDto>();

        _backgroundJobClient
            .Received(1).Create(Arg.Any<Job>(), Arg.Any<IState>());

        actualRequestDto.ShouldBeEquivalentTo(requestDto);
        actualRateId.ShouldBeEquivalentTo(expectedRateId);
        actualResponseDto.ShouldBeEquivalentTo(expectedResponseDto);
        actualResponseDto.CreatedAt.ShouldNotBeNull();
        actualResponseDto.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetCurrencyExchangeRatesResponse_PreferStoredData_Expired_LimitReached_ReturnsExpired()
    {
        // Arrange
        var requestDto = _fixture.Create<ExchangeRateRequestDto>();
        var expectedEntity = _fixture.Create<ForexRateEntity>();
        expectedEntity.CreatedAt = DateTime.UtcNow.AddMinutes(-10);
        expectedEntity.UpdatedAt = null;
        var expectedResponseDto = _actualMapper.Map<ExchangeRateResponseDto>(expectedEntity);
        var expectedRateId = _actualMapper.Map<ForexRateKey>(requestDto);
        ExchangeRateResponseDto? actualResponseDto = default;
        ExchangeRateRequestDto? actualRequestDto = default;
        ForexRateKey? actualRateId = default;
        Settings settings = new()
        {
            ExchangeRate = new ExchangeRateSettings
            {
                PreferLiveData = false,
                ExpirationMinutes = 5
            }
        };

        _settings.Value.Returns(settings);

        _forexRateRepository
            .FindByIdAsync(Arg.Any<ForexRateKey>())
            .Returns(callInfo =>
            {
                actualRateId = (ForexRateKey)callInfo[0];
                return expectedEntity;
            });

        _forexClient
            .GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode)
            .ThrowsAsync(new HttpClientLimitReachedException("test"))
            .AndDoes(callInfo => actualRequestDto = new ExchangeRateRequestDto
            {
                FromCurrencyCode = (string)callInfo[0],
                ToCurrencyCode = (string)callInfo[1]
            });

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();
        SetupMap<ForexRateEntity, ExchangeRateResponseDto>();

        // Act
        var action = async () => actualResponseDto = await _service.GetForexRate(requestDto);

        // Assert
        await action.ShouldNotThrowAsync();
        await _forexRateRepository
            .Received(1).FindByIdAsync(Arg.Any<ForexRateKey>());
        await _forexClient
            .Received(1).GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode);

        ReceivedMap<ExchangeRateRequestDto, ForexRateKey>(1);
        ReceivedMap<ForexRateEntity, ExchangeRateResponseDto>(1);
        DidNotReceiveMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();

        _backgroundJobClient
            .DidNotReceive()
            .Create(Arg.Any<Job>(), Arg.Any<IState>());

        actualRequestDto.ShouldBeEquivalentTo(requestDto);
        actualRateId.ShouldBeEquivalentTo(expectedRateId);
        actualResponseDto.ShouldBeEquivalentTo(expectedResponseDto);
    }

    [Fact]
    public async Task GetCurrencyExchangeRatesResponse_PreferStoredData_NoData_ReturnsLiveData()
    {
        // Arrange
        var requestDto = _fixture.Create<ExchangeRateRequestDto>();
        var expectedRateId = new ForexRateKey { FromCurrencyCode = requestDto.FromCurrencyCode, ToCurrencyCode = requestDto.ToCurrencyCode };
        var expectedClientResponse = _fixture.Create<GetCurrencyExchangeRatesResponse>();
        expectedClientResponse.RealtimeCurrencyExchangeRate!.TimeZone = "UTC";
        var expectedResponseDto = _actualMapper.Map<ExchangeRateResponseDto>(expectedClientResponse);
        ExchangeRateResponseDto? actualResponseDto = default;
        ExchangeRateRequestDto? actualRequestDto = default;
        Settings settings = new()
        {
            ExchangeRate = new ExchangeRateSettings
            {
                PreferLiveData = false,
                ExpirationMinutes = 5
            }
        };

        _settings.Value.Returns(settings);

        _forexClient
            .GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode)
            .Returns(callInfo =>
            {
                actualRequestDto = new ExchangeRateRequestDto
                {
                    FromCurrencyCode = (string)callInfo[0],
                    ToCurrencyCode = (string)callInfo[1]
                };
                return expectedClientResponse;
            });

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();
        SetupMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();

        // Act
        actualResponseDto = await _service.GetForexRate(requestDto);

        // Assert
        await _forexRateRepository
            .Received(1).FindByIdAsync(Arg.Any<ForexRateKey>());
        await _forexClient
            .Received(1).GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode);

        ReceivedMap<ExchangeRateRequestDto, ForexRateKey>(1);
        ReceivedMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>(1);
        DidNotReceiveMap<ForexRateEntity, ExchangeRateResponseDto>();

        _backgroundJobClient
            .Received(1)
            .Create(Arg.Any<Job>(), Arg.Any<IState>());

        expectedResponseDto = expectedResponseDto with
        {
            CreatedAt = actualResponseDto.CreatedAt,
            UpdatedAt = actualResponseDto.UpdatedAt
        };
        actualRequestDto.ShouldBeEquivalentTo(requestDto);
        actualResponseDto.ShouldBeEquivalentTo(expectedResponseDto);
    }

    [Fact]
    public async Task GetCurrencyExchangeRatesResponse_PreferStoredData_NoData_LimitReached_ThrowsLimitException()
    {
        // Arrange
        var requestDto = _fixture.Create<ExchangeRateRequestDto>();
        Settings settings = new()
        {
            ExchangeRate = new ExchangeRateSettings
            {
                PreferLiveData = false,
                ExpirationMinutes = 5
            }
        };

        _settings.Value.Returns(settings);

        _forexClient
            .GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode)
            .ThrowsAsync(new HttpClientLimitReachedException("test"));

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();

        // Act
        var action = async () => await _service.GetForexRate(requestDto);

        // Assert
        await action.ShouldThrowAsync<HttpClientLimitReachedException>();
        await _forexRateRepository
            .Received(1).FindByIdAsync(Arg.Any<ForexRateKey>());
        await _forexClient
            .Received(1).GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode);

        ReceivedMap<ExchangeRateRequestDto, ForexRateKey>(1);
        DidNotReceiveMap<ForexRateEntity, ExchangeRateResponseDto>();
        DidNotReceiveMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();

        _backgroundJobClient
            .DidNotReceive()
            .Create(Arg.Any<Job>(), Arg.Any<IState>());
    }

    [Fact]
    public async Task GetCurrencyExchangeRatesResponse_PreferStoredData_NoData_BadRequest_ThrowsInvalidRateException()
    {
        // Arrange
        var requestDto = _fixture.Create<ExchangeRateRequestDto>();
        Settings settings = new()
        {
            ExchangeRate = new ExchangeRateSettings
            {
                PreferLiveData = false,
                ExpirationMinutes = 5
            }
        };

        _settings.Value.Returns(settings);

        _forexClient
            .GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode)
            .ThrowsAsync(new BadHttpRequestException("test"));

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();

        // Act
        var action = async () => await _service.GetForexRate(requestDto);

        // Assert
        await action.ShouldThrowAsync<InvalidExchangeRateException>();

        await _forexRateRepository
            .Received(1).FindByIdAsync(Arg.Any<ForexRateKey>());
        await _forexClient
            .Received(1).GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode);

        ReceivedMap<ExchangeRateRequestDto, ForexRateKey>(1);
        DidNotReceiveMap<ForexRateEntity, ExchangeRateResponseDto>();
        DidNotReceiveMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();

        _backgroundJobClient
            .DidNotReceive().Create(Arg.Any<Job>(), Arg.Any<IState>());
    }

    [Fact]
    public async Task GetCurrencyExchangeRatesResponse_PreferStoredData_NoData_UnexpectedClientError_PassesExceptionThrough()
    {
        // Arrange
        var requestDto = _fixture.Create<ExchangeRateRequestDto>();
        Settings settings = new()
        {
            ExchangeRate = new ExchangeRateSettings
            {
                PreferLiveData = false,
                ExpirationMinutes = 5
            }
        };

        _settings.Value.Returns(settings);

        _forexClient
            .GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode)
            .ThrowsAsync(new InvalidOperationException());

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();

        // Act
        var action = async () => await _service.GetForexRate(requestDto);

        // Assert
        await action.ShouldThrowAsync<InvalidOperationException>();

        await _forexRateRepository
            .Received(1).FindByIdAsync(Arg.Any<ForexRateKey>());
        await _forexClient
            .Received(1).GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode);

        ReceivedMap<ExchangeRateRequestDto, ForexRateKey>(1);
        DidNotReceiveMap<ForexRateEntity, ExchangeRateResponseDto>();
        DidNotReceiveMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();

        _backgroundJobClient
            .DidNotReceive().Create(Arg.Any<Job>(), Arg.Any<IState>());
    }

    [Fact]
    public async Task GetCurrencyExchangeRatesResponse_PreferStoredData_UnexpectedDatabaseError_PassesExceptionThrough()
    {
        // Arrange
        var requestDto = _fixture.Create<ExchangeRateRequestDto>();
        Settings settings = new()
        {
            ExchangeRate = new ExchangeRateSettings
            {
                PreferLiveData = false,
                ExpirationMinutes = 5
            }
        };

        _settings.Value.Returns(settings);

        _forexRateRepository
            .FindByIdAsync(Arg.Any<ForexRateKey>())
            .ThrowsAsync(new InvalidOperationException());

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();

        // Act
        var action = async () => await _service.GetForexRate(requestDto);

        // Assert
        await action.ShouldThrowAsync<InvalidOperationException>();

        await _forexRateRepository
            .Received(1).FindByIdAsync(Arg.Any<ForexRateKey>());
        await _forexClient
            .DidNotReceive().GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode);

        ReceivedMap<ExchangeRateRequestDto, ForexRateKey>(1);
        DidNotReceiveMap<ForexRateEntity, ExchangeRateResponseDto>();
        DidNotReceiveMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();

        _backgroundJobClient
            .DidNotReceive().Create(Arg.Any<Job>(), Arg.Any<IState>());
    }

    #endregion PreferStoredData Tests

    #region UpdateRateExchange Tests

    [Fact]
    public async Task UpdateRateExchange_NewRecord_SendsMessage()
    {
        //Arrange
        ExchangeRateResponseDto rateToUpdate = _fixture.Create<ExchangeRateResponseDto>();
        SetupMap<ExchangeRateResponseDto, ForexRateEntity>();

        //Act
        await _service.UpdateForexRate(rateToUpdate);

        await _forexRateRepository
            .Received(1).Upsert(Arg.Any<ForexRateEntity>());

        await _messageQueueService
            .Received(1).SendMessage(MessageQueues.NewForexRate, Arg.Any<ExchangeRateResponseDto>());

        ReceivedMap<ExchangeRateResponseDto, ForexRateEntity>(1);
    }

    [Fact]
    public async Task UpdateRateExchange_IsLatest_SendsMessage()
    {
        //Arrange
        ExchangeRateResponseDto rateToUpdate = _fixture.Create<ExchangeRateResponseDto>();
        SetupMap<ExchangeRateResponseDto, ForexRateEntity>();

        //Act
        await _service.UpdateForexRate(rateToUpdate, rateToUpdate.LastRefreshed!.Value.AddMinutes(-5));

        await _forexRateRepository
            .Received(1).Upsert(Arg.Any<ForexRateEntity>());

        await _messageQueueService
            .Received(1).SendMessage(MessageQueues.NewForexRate, Arg.Any<ExchangeRateResponseDto>());

        ReceivedMap<ExchangeRateResponseDto, ForexRateEntity>(1);
    }

    [Fact]
    public async Task UpdateRateExchange_IsNotTheLatest_OnlyUpdates()
    {
        //Arrange
        ExchangeRateResponseDto rateToUpdate = _fixture.Create<ExchangeRateResponseDto>();
        SetupMap<ExchangeRateResponseDto, ForexRateEntity>();

        //Act
        await _service.UpdateForexRate(rateToUpdate, rateToUpdate.LastRefreshed!.Value.AddMinutes(5));

        await _forexRateRepository
            .Received(1).Upsert(Arg.Any<ForexRateEntity>());

        await _messageQueueService
            .DidNotReceive().SendMessage(MessageQueues.NewForexRate, Arg.Any<ExchangeRateResponseDto>());

        ReceivedMap<ExchangeRateResponseDto, ForexRateEntity>(1);
    }
    #endregion UpdateRateExchange Tests
}

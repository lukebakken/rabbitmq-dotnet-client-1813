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
using ExchangeRateManager.Services;
using ExchangeRateManager.Services.Interfaces;
using ExchangeRateManager.Tests.UnitTests.Base;
using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ExchangeRateManager.Tests.UnitTests.Services;

/// <summary>
/// Tests for the ExchangeRateService
/// </summary>
public class ExchangeRateServiceTests : MapperTestBase
{
    private readonly ExchangeRateService _service;
    private readonly Mock<IForexClient> _forexClientMock = new();
    private readonly Mock<IOptionsSnapshot<Settings>> _settingsMock = new();
    private readonly Mock<IForexRateRepository> _forexRateRepositoryMock = new();
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock = new();
    private readonly Mock<IMessageQueueService> _messageQueueServiceMock = new();

    public ExchangeRateServiceTests() : base(new MapperConfiguration(mc => mc.AddProfile(new ForexRateProfile())))
    {
        _service = new ExchangeRateService(
            _forexClientMock.Object, _forexRateRepositoryMock.Object,
            _messageQueueServiceMock.Object, _backgroundJobClientMock.Object,
            _settingsMock.Object, _mockedMapper, NullLogger<ExchangeRateService>.Instance);
    }

    #region PreferLiveData Tests
    [Fact]
    public async Task GetForexRate_PreferLiveData_ReturnsLiveData_UpdatesStored()
    {
        // Arrange
        var requestDto = _fixture.Create<ExchangeRateRequestDto>();
        var expectedClientResponse = _fixture.Create<GetCurrencyExchangeRatesResponse>();
        expectedClientResponse.RealtimeCurrencyExchangeRate!.TimeZone = "UTC";
        var expectedResponseDto = _actualMapper.Map<ExchangeRateResponseDto>(expectedClientResponse);
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

        _settingsMock
            .Setup(x => x.Value)
            .Returns(settings);

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();
        SetupMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();

        _forexClientMock
            .Setup(x => x.GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode))
            .ReturnsAsync((string a, string b) =>
            {
                actualRequestDto = new ExchangeRateRequestDto { FromCurrencyCode = a, ToCurrencyCode = b };
                return expectedClientResponse;
            });

        _forexRateRepositoryMock
            .Setup(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()))
            .ReturnsAsync((ForexRateKey id) =>
            {
                actualForexRateKey = id;
                return expectedEntity;
            });

        // Act
        actualResponseDto = await _service.GetForexRate(requestDto);

        // Assert
        _forexClientMock
            .Verify(x => x.GetCurrencyExchangeRates(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        _forexRateRepositoryMock
            .Verify(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()), Times.Once);

        VerifyMap<ExchangeRateRequestDto, ForexRateKey>(Times.Once);
        VerifyMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>(Times.Once);

        _backgroundJobClientMock
            .Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);

        actualRequestDto.Should().BeEquivalentTo(requestDto);
        actualResponseDto.Should().BeEquivalentTo(expectedResponseDto, opt => opt
            .Excluding(x => x.CreatedAt).Excluding(x => x.UpdatedAt));
        actualResponseDto.CreatedAt.Should().BeBefore(actualResponseDto.UpdatedAt!.Value);
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

        _settingsMock
            .Setup(x => x.Value)
            .Returns(settings);

        _forexClientMock
            .Setup(x => x.GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode))
            .ThrowsAsync(new BadHttpRequestException("Test"));

        // Act
        var action = async () => await _service.GetForexRate(requestDto);

        // Assert
        await action.Should().ThrowAsync<InvalidExchangeRateException>();

        _forexClientMock
            .Verify(x => x.GetCurrencyExchangeRates(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        _forexClientMock
            .Verify(x => x.GetCurrencyExchangeRates(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        VerifyMap<ExchangeRateRequestDto, ForexRateKey>(Times.Once);
        VerifyMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>(Times.Never);

        _backgroundJobClientMock
            .Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
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

        _settingsMock
            .Setup(x => x.Value)
            .Returns(settings);

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();
        SetupMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();
        SetupMap<ForexRateEntity, ExchangeRateResponseDto>();

        _forexClientMock
            .Setup(x => x.GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode))
            .Callback((string a, string b) =>
            {
                actualRequestDto = new ExchangeRateRequestDto { FromCurrencyCode = a, ToCurrencyCode = b };
            })
            .ThrowsAsync(new HttpClientLimitReachedException("Test"));

        _forexRateRepositoryMock
            .Setup(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()))
            .ReturnsAsync((ForexRateKey x) =>
            {
                actualRateId = x;
                return expectedEntity;
            });

        // Act
        var action = async () =>
            actualResponseDto = await _service.GetForexRate(requestDto);

        // Assert
        await action.Should().NotThrowAsync();

        _forexClientMock
            .Verify(x => x.GetCurrencyExchangeRates(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        _forexRateRepositoryMock
            .Verify(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()), Times.Once);

        VerifyMap<ExchangeRateRequestDto, ForexRateKey>(Times.Once);
        VerifyMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>(Times.Never);
        VerifyMap<ForexRateEntity, ExchangeRateResponseDto>(Times.Once);

        _backgroundJobClientMock
            .Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);

        actualRequestDto.Should().BeEquivalentTo(requestDto);
        actualResponseDto.Should().BeEquivalentTo(expectedResponseDto, opt => opt.Excluding(x => x.CreatedAt));
        actualResponseDto!.CreatedAt.Should().NotBeNull();
        actualRateId.Should().BeEquivalentTo(expectedRateId);
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

        _settingsMock
            .Setup(x => x.Value)
            .Returns(settings);

        _forexClientMock
            .Setup(x => x.GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode))
            .Callback((string a, string b) =>
            {
                actualRequestDto = new ExchangeRateRequestDto { FromCurrencyCode = a, ToCurrencyCode = b };
            })
            .ThrowsAsync(new HttpClientLimitReachedException("Test"));

        _forexRateRepositoryMock
            .Setup(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()))
            .Callback((ForexRateKey x) => actualRateId = x);

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();

        // Act
        var action = async () => await _service.GetForexRate(requestDto);

        // Assert
        await action.Should().ThrowAsync<HttpClientLimitReachedException>();

        _forexClientMock
            .Verify(x => x.GetCurrencyExchangeRates(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        _forexRateRepositoryMock
            .Verify(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()), Times.Once);

        VerifyMap<ExchangeRateRequestDto, ForexRateKey>(Times.Once);
        VerifyMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>(Times.Never);
        VerifyMap<ForexRateEntity, ExchangeRateResponseDto>(Times.Never);

        _backgroundJobClientMock
            .Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);

        actualRequestDto.Should().BeEquivalentTo(requestDto);
        actualRateId.Should().BeEquivalentTo(expectedRateId);
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

        _settingsMock
            .Setup(x => x.Value)
            .Returns(settings);

        _forexRateRepositoryMock
            .Setup(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()))
            .ThrowsAsync(new TestException());

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();

        // Act
        var action = async () => await _service.GetForexRate(requestDto);

        // Assert
        await action.Should().ThrowAsync<TestException>();

        _forexClientMock
            .Verify(x => x.GetCurrencyExchangeRates(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        _forexRateRepositoryMock
            .Verify(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()), Times.Once);

        VerifyMap<ExchangeRateRequestDto, ForexRateKey>(Times.Once);
        VerifyMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>(Times.Never);
        VerifyMap<ForexRateEntity, ExchangeRateResponseDto>(Times.Never);

        _backgroundJobClientMock
            .Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
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

        _settingsMock
            .Setup(x => x.Value)
            .Returns(settings);

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();
        SetupMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();

        _forexClientMock
            .Setup(x => x.GetCurrencyExchangeRates(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(expectedClientResponse);

        _forexRateRepositoryMock
            .Setup(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()))
            .ReturnsAsync(expectedEntity);

        _backgroundJobClientMock
            .Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Throws<TestException>();

        // Act
        var action = async () => await _service.GetForexRate(requestDto);

        // Assert
        await action.Should().ThrowAsync<TestException>();
        _forexClientMock
            .Verify(x => x.GetCurrencyExchangeRates(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        _forexRateRepositoryMock
            .Verify(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()), Times.Once);

        VerifyMap<ExchangeRateRequestDto, ForexRateKey>(Times.Once);
        VerifyMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>(Times.Once);

        _backgroundJobClientMock
            .Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
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


        _settingsMock
            .Setup(x => x.Value)
            .Returns(settings);

        _forexRateRepositoryMock
            .Setup(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()))
            .ReturnsAsync((ForexRateKey x) =>
            {
                actualRateId = x;
                return expectedEntity;
            });

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();
        SetupMap<ForexRateEntity, ExchangeRateResponseDto>();

        // Act
        actualResponseDto = await _service.GetForexRate(requestDto);

        _forexClientMock
            .Verify(x => x.GetCurrencyExchangeRates(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        _forexRateRepositoryMock
            .Verify(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()), Times.Once);
        
        VerifyMap<ExchangeRateRequestDto, ForexRateKey>(Times.Once);
        VerifyMap<ForexRateEntity, ExchangeRateResponseDto>(Times.Once);

        _backgroundJobClientMock
            .Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);

        actualResponseDto.Should().BeEquivalentTo(expectedResponseDto);
        actualRateId.Should().BeEquivalentTo(expectedRateId);
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

        _settingsMock
            .Setup(x => x.Value)
            .Returns(settings);

        _forexRateRepositoryMock
            .Setup(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()))
            .ReturnsAsync((ForexRateKey x) =>
            {
                actualRateId = x;
                return expectedEntity;
            });

        _forexClientMock
            .Setup(x => x.GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode))
            .ReturnsAsync((string a, string b) =>
            {
                actualRequestDto = new ExchangeRateRequestDto { FromCurrencyCode = a, ToCurrencyCode = b };
                return expectedClientResponse;
            });

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();
        SetupMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();


        // Act
        actualResponseDto = await _service.GetForexRate(requestDto);

        // Assert
        _forexRateRepositoryMock
            .Verify(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()), Times.Once);
        _forexClientMock
            .Verify(x => x.GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode), Times.Once);

        VerifyMap<ExchangeRateRequestDto, ForexRateKey>(Times.Once);
        VerifyMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>(Times.Once);
        VerifyMap<ForexRateEntity, ExchangeRateResponseDto>(Times.Never);

        _backgroundJobClientMock
            .Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);

        actualRequestDto.Should().BeEquivalentTo(requestDto);
        actualRateId.Should().BeEquivalentTo(expectedRateId);
        actualResponseDto.Should().BeEquivalentTo(expectedResponseDto, opt => opt
            .Excluding(x => x.CreatedAt).Excluding(x => x.UpdatedAt));
        actualResponseDto.CreatedAt.Should().NotBeNull();
        actualResponseDto.UpdatedAt.Should().NotBeNull();
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

        _settingsMock
            .Setup(x => x.Value)
            .Returns(settings);

        _forexRateRepositoryMock
            .Setup(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()))
            .ReturnsAsync((ForexRateKey x) =>
            {
                actualRateId = x;
                return expectedEntity;
            });

        _forexClientMock
            .Setup(x => x.GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode))
            .Callback((string a, string b) => actualRequestDto = new ExchangeRateRequestDto { FromCurrencyCode = a, ToCurrencyCode = b })
            .ThrowsAsync(new HttpClientLimitReachedException("test"));

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();
        SetupMap<ForexRateEntity, ExchangeRateResponseDto>();

        // Act
        var action = async () => actualResponseDto = await _service.GetForexRate(requestDto);

        // Assert
        await action.Should().NotThrowAsync();
        _forexRateRepositoryMock
            .Verify(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()), Times.Once);
        _forexClientMock
            .Verify(x => x.GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode), Times.Once);

        VerifyMap<ExchangeRateRequestDto, ForexRateKey>(Times.Once);
        VerifyMap<ForexRateEntity, ExchangeRateResponseDto>(Times.Once);
        VerifyMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>(Times.Never);

        _backgroundJobClientMock
            .Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);

        actualRequestDto.Should().BeEquivalentTo(requestDto);
        actualRateId.Should().BeEquivalentTo(expectedRateId);
        actualResponseDto.Should().BeEquivalentTo(expectedResponseDto);
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

        _settingsMock
            .Setup(x => x.Value)
            .Returns(settings);

        _forexClientMock
            .Setup(x => x.GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode))
            .ReturnsAsync((string a, string b) =>
            {
                actualRequestDto = new ExchangeRateRequestDto { FromCurrencyCode = a, ToCurrencyCode = b };
                return expectedClientResponse;
            });

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();
        SetupMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>();

        // Act
        actualResponseDto = await _service.GetForexRate(requestDto);

        // Assert
        _forexRateRepositoryMock
            .Verify(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()), Times.Once);
        _forexClientMock
            .Verify(x => x.GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode), Times.Once);

        VerifyMap<ExchangeRateRequestDto, ForexRateKey>(Times.Once);
        VerifyMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>(Times.Once);
        VerifyMap<ForexRateEntity, ExchangeRateResponseDto>(Times.Never);

        _backgroundJobClientMock
            .Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);

        actualRequestDto.Should().BeEquivalentTo(requestDto);
        actualResponseDto.Should().BeEquivalentTo(expectedResponseDto, opt => opt.Excluding(x => x.CreatedAt));
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

        _settingsMock
            .Setup(x => x.Value)
            .Returns(settings);

        _forexClientMock
            .Setup(x => x.GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode))
            .ThrowsAsync(new HttpClientLimitReachedException("test"));

        SetupMap<ExchangeRateRequestDto,ForexRateKey>();

        // Act
        var action = async () => await _service.GetForexRate(requestDto);

        // Assert
        await action.Should().ThrowAsync<HttpClientLimitReachedException>();
        _forexRateRepositoryMock
            .Verify(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()), Times.Once);
        _forexClientMock
            .Verify(x => x.GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode), Times.Once);

        VerifyMap<ExchangeRateRequestDto, ForexRateKey>(Times.Once);
        VerifyMap<ForexRateEntity, ExchangeRateResponseDto>(Times.Never);
        VerifyMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>(Times.Never);

        _backgroundJobClientMock
            .Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
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

        _settingsMock
            .Setup(x => x.Value)
            .Returns(settings);

        _forexClientMock
            .Setup(x => x.GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode))
            .ThrowsAsync(new BadHttpRequestException("test"));

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();

        // Act
        var action = async () => await _service.GetForexRate(requestDto);

        // Assert
        await action.Should().ThrowAsync<InvalidExchangeRateException>();

        _forexRateRepositoryMock
            .Verify(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()), Times.Once);
        _forexClientMock
            .Verify(x => x.GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode), Times.Once);

        VerifyMap<ExchangeRateRequestDto, ForexRateKey>(Times.Once);
        VerifyMap<ForexRateEntity, ExchangeRateResponseDto>(Times.Never);
        VerifyMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>(Times.Never);

        _backgroundJobClientMock
            .Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
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

        _settingsMock
            .Setup(x => x.Value)
            .Returns(settings);

        _forexClientMock
            .Setup(x => x.GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode))
            .ThrowsAsync(new TestException());

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();

        // Act
        var action = async () => await _service.GetForexRate(requestDto);

        // Assert
        await action.Should().ThrowAsync<TestException>();

        _forexRateRepositoryMock
            .Verify(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()), Times.Once);
        _forexClientMock
            .Verify(x => x.GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode), Times.Once);

        VerifyMap<ExchangeRateRequestDto, ForexRateKey>(Times.Once);
        VerifyMap<ForexRateEntity, ExchangeRateResponseDto>(Times.Never);
        VerifyMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>(Times.Never);

        _backgroundJobClientMock
            .Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
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

        _settingsMock
            .Setup(x => x.Value)
            .Returns(settings);

        _forexRateRepositoryMock
            .Setup(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()))
            .ThrowsAsync(new TestException());

        SetupMap<ExchangeRateRequestDto, ForexRateKey>();

        // Act
        var action = async () => await _service.GetForexRate(requestDto);

        // Assert
        await action.Should().ThrowAsync<TestException>();

        _forexRateRepositoryMock
            .Verify(x => x.FindByIdAsync(It.IsAny<ForexRateKey>()), Times.Once);
        _forexClientMock
            .Verify(x => x.GetCurrencyExchangeRates(requestDto.FromCurrencyCode, requestDto.ToCurrencyCode), Times.Never);

        VerifyMap<ExchangeRateRequestDto, ForexRateKey>(Times.Once);
        VerifyMap<ForexRateEntity, ExchangeRateResponseDto>(Times.Never);
        VerifyMap<GetCurrencyExchangeRatesResponse, ExchangeRateResponseDto>(Times.Never);

        _backgroundJobClientMock
            .Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
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

        _forexRateRepositoryMock
            .Verify(x => x.Upsert(It.IsAny<ForexRateEntity>()), Times.Once);

        _messageQueueServiceMock
            .Verify(x => x.SendMessageAsync(MessageQueues.NewForexRate, It.IsAny<ExchangeRateResponseDto>()), Times.Once);

        VerifyMap<ExchangeRateResponseDto, ForexRateEntity>(Times.Once);
    }

    [Fact]
    public async Task UpdateRateExchange_IsLatest_SendsMessage()
    {
        //Arrange
        ExchangeRateResponseDto rateToUpdate = _fixture.Create<ExchangeRateResponseDto>();
        SetupMap<ExchangeRateResponseDto, ForexRateEntity>();

        //Act
        await _service.UpdateForexRate(rateToUpdate, rateToUpdate.LastRefreshed!.Value.AddMinutes(-5));

        _forexRateRepositoryMock
            .Verify(x => x.Upsert(It.IsAny<ForexRateEntity>()), Times.Once);

        _messageQueueServiceMock
            .Verify(x => x.SendMessageAsync(MessageQueues.NewForexRate, It.IsAny<ExchangeRateResponseDto>()), Times.Once);

        VerifyMap<ExchangeRateResponseDto, ForexRateEntity>(Times.Once);
    }

    [Fact]
    public async Task UpdateRateExchange_IsNotTheLatest_OnlyUpdates()
    {
        //Arrange
        ExchangeRateResponseDto rateToUpdate = _fixture.Create<ExchangeRateResponseDto>();
        SetupMap<ExchangeRateResponseDto, ForexRateEntity>();

        //Act
        await _service.UpdateForexRate(rateToUpdate, rateToUpdate.LastRefreshed!.Value.AddMinutes(5));

        _forexRateRepositoryMock
            .Verify(x => x.Upsert(It.IsAny<ForexRateEntity>()), Times.Once);

        _messageQueueServiceMock
            .Verify(x => x.SendMessageAsync(MessageQueues.NewForexRate, It.IsAny<ExchangeRateResponseDto>()), Times.Never);

        VerifyMap<ExchangeRateResponseDto, ForexRateEntity>(Times.Once);
    }
    #endregion UpdateRateExchange Tests
}

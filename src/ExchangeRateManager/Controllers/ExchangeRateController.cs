using ExchangeRateManager.Dtos;
using ExchangeRateManager.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ExchangeRateManager.Controllers
{
    /// <summary>
    /// Gets the latest rate exchange from a third party broker, caching the rates for faster updates.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class ExchangeRateController(
        IExchangeRateService exchangeRateService) : ControllerBase
    {
        private readonly IExchangeRateService _exchangeRateService = exchangeRateService;

        /// <summary>
        /// Gets the latest rate exchange for a specific currency pair.
        /// </summary>
        /// <param name="fromCurrency">The source currency.</param>
        /// <param name="toCurrency">The target currency.</param>
        /// <returns>The exchange rates</returns>
        [HttpGet(Name = $"{nameof(GetLatestRateExchange)}/{{SourceCurrency}}/{{TargetCurrency}}")]
        [ProducesResponseType<ExchangeRateResponseDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> GetLatestRateExchange([Required] string fromCurrency, [Required] string toCurrency)
        {
            var exchangeRate = new ExchangeRateRequestDto
            { 
                FromCurrencyCode = fromCurrency,
                ToCurrencyCode = toCurrency
            };

            var result = await _exchangeRateService.GetForexRate(exchangeRate);
            return Ok(result);
        }
    }
}

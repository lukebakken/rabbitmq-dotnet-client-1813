using ExchangeRateManager.Repositories.Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExchangeRateManager.Repositories.Entities
{
    public class ForexRateEntity : EntityBase
    {
        [Column(Order = 0)]
        public required string FromCurrencyCode { get; set; }

        public required string FromCurrencyName { get; set; }

        [Column(Order = 1)]
        public required string ToCurrencyCode { get; set; }

        public required string ToCurrencyName { get; set; }

        public required decimal ExchangeRate { get; set; }

        public DateTime LastRefreshed { get; set; }

        public required decimal BidPrice { get; set; }

        public required decimal AskPrice { get; set; }
    }

    public class ForexRateKey
    {
        [Column(Order = 0)]
        public string FromCurrencyCode { get; set; } = "";

        [Column(Order = 1)]
        public string ToCurrencyCode { get; set; } = "";
    }

    public class ForexRateEntityConfiguration : IEntityTypeConfiguration<ForexRateEntity>
    {
        public void Configure(EntityTypeBuilder<ForexRateEntity> builder)
        {
            builder.HasKey(x => new
            {
                x.FromCurrencyCode, 
                x.ToCurrencyCode
            });
        }
    }
}
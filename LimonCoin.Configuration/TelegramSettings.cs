using System.ComponentModel.DataAnnotations;

namespace LimonCoin.Configuration
{
    public sealed class TelegramSettings
    {
        [Required]
        public string Token { get; init; }

        [Required]
        [Url]
        public string WebAppUrl { get; init; }
    }
}

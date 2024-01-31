using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LimonCoin.Models
{
    public class ApplicationUser
    {
        [Key]
        public int Id { get; init; }

        public long TelegramId { get; init; }

        public ulong Coins { get; set; }

        public ulong CoinsThisDay { get; set; }

        public ulong CoinsThisWeek { get; set; }

        public DateTime LastTimeClicked { get; set; }

        public uint CoinsPerClick { get; set; }

        public uint Energy { get; set; }

        public uint EnergyPerSecond { get; set; }

        public uint MaxEnergy { get; set; }

        public int? ReferrerId { get; init; }

        public int ClickerLevel { get; set; }
        
        public int EnergyRecoveryLevel { get; set; }
        
        public int EnergyCapacityLevel { get; set; }

        [ForeignKey(nameof(ReferrerId))]
        public ApplicationUser? Referrer { get; init; }

        public List<Guid> CompletedTasks { get; init; }

        public List<Guid> AwardedTasks { get; init; }
    }
}

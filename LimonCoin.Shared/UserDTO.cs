namespace LimonCoin.Shared
{
    public sealed class UserDTO
    {
        public string? Username { get; init; }

        public long TelegramId { get; init; }

        public DateTime LastTimeClicked { get; init; }

        public ulong Coins { get; init; }

        public ulong CoinsThisDay { get; init; }

        public ulong CoinsThisWeek { get; init; }

        public uint CoinsPerClick { get; init; }

        public uint Energy { get; init; }

        public uint EnergyPerSecond { get; init; }

        public uint MaxEnergy { get; init; }

        public long? ReferrerId { get; init; }

        public int ClickerLevel { get; init; }

        public int EnergyRecoveryLevel { get; init; }

        public int EnergyCapacityLevel { get; init; }

        public IEnumerable<long> ReferralTelegramIds { get; init; }

        public List<Guid> CompletedTasks { get; init; }

        public List<Guid> AwardedTasks { get; init; }
    }
}

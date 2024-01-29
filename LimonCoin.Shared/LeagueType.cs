namespace LimonCoin.Shared
{
    public enum LeagueType
    {
        Bronze = 0,
        Gold = 1,
        Platinum = 2,
        Diamond = 3
    }

    public static class LeagueTypeExtensions
    {
        public static string ToDisplayName(this LeagueType type)
        {
            return type switch
            {
                LeagueType.Bronze => "Бронзовая",
                LeagueType.Gold => "Золотая",
                LeagueType.Platinum => "Платиновая",
                LeagueType.Diamond => "Алмазная"
            };
        }

        public static string ToProgramName(this LeagueType type)
        {
            return type switch
            {
                LeagueType.Bronze => "bronze",
                LeagueType.Gold => "gold",
                LeagueType.Platinum => "platinum",
                LeagueType.Diamond => "diamond"
            };
        }

        public static LeagueType GetLeagueFromCoins(ulong coins)
        {
            return coins switch
            {
                var t when t <= 100_000 => LeagueType.Bronze,
                var t when t > 100_000 && t <= 2_000_000 => LeagueType.Gold,
                var t when t > 2_000_000 && t <= 10_000_000 => LeagueType.Platinum,
                var t when t > 10_000_000 => LeagueType.Diamond
            };
        }

        public static ulong GetMaxCoinsForLeague(this LeagueType type)
        {
            return type switch
            {
                LeagueType.Bronze => 100_000,
                LeagueType.Gold => 2_000_000,
                LeagueType.Platinum => 10_000_000,
                LeagueType.Diamond => 1_000_000_000
            };
        }

        public static ulong GetMinCoinsForLeague(this LeagueType type)
        {
            return type switch
            {
                LeagueType.Bronze => 0,
                LeagueType.Gold => 100_000,
                LeagueType.Platinum => 2_000_000,
                LeagueType.Diamond => 10_000_000
            };
        }


    }
}

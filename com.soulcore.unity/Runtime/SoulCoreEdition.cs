namespace SoulCore
{
    /// <summary>
    /// 产品版本：免费 / 标准 / 高级。
    /// 通过编译宏区分：<c>SOUL_EDITION_FREE</c> / <c>SOUL_EDITION_PRO</c>（都不定义时=标准版）。
    /// 单机侧无法 100% 防篡改，以协议与交付形态为主。
    /// </summary>
    public enum SoulEditionTier
    {
        Free = 0,
        Standard = 1,
        Pro = 2
    }

    public static class SoulCoreEdition
    {
        public const int FreeMaxActiveNpcs = 10;

        public static SoulEditionTier Tier =>
#if SOUL_EDITION_FREE
            SoulEditionTier.Free;
#elif SOUL_EDITION_PRO
            SoulEditionTier.Pro;
#else
            SoulEditionTier.Standard;
#endif

        public static bool IsFree => Tier == SoulEditionTier.Free;
        public static bool IsStandard => Tier == SoulEditionTier.Standard;
        public static bool IsPro => Tier == SoulEditionTier.Pro;

        public static int MaxActiveNpcSlots => IsFree ? FreeMaxActiveNpcs : int.MaxValue;
        public static bool CanUseSnapshot => !IsFree;
        public static bool CanUseOpenAgentApi => IsPro;

        public static string TierLabelZh => Tier switch
        {
            SoulEditionTier.Free => "免费版 4.0",
            SoulEditionTier.Standard => "标准版 4.0",
            SoulEditionTier.Pro => "高级版 6.1.8",
            _ => "标准版 4.0"
        };
    }
}

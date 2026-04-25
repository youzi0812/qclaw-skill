using System;
using UnityEngine;

namespace SoulCore
{
    /// <summary>
    /// 本包发行渠道。用于日志、功能开关或与商店元数据对应（见 package.json 与 Documentation~）。
    /// </summary>
    public enum SoulCoreChannel
    {
        /// <summary>未识别或需手动在 Player Settings 里设 Scripting Define。</summary>
        Unknown = 0,
        /// <summary>Unity 国际版 / Hub 非 China 安装源（典型版本号无 c1 后缀等）。</summary>
        UnityEditorInternational = 1,
        /// <summary>团结引擎 / 中国版 Editor（如版本号含 2022.3.62f3c1 等 c1 标识）。</summary>
        TuanjieEditor = 2
    }

    /// <summary>探测当前 Editor 与包目标，不做网络请求。</summary>
    public static class SoulCoreEngineInfo
    {
        /// <summary>本包在 package.json 中声明的最低 Unity 版本线（国际版/团结内核共用 LTS 线）。</summary>
        public const string PackageMinimumUnity = "2022.3";

        /// <summary>团结引擎产品侧：推荐验证版本（与 Hub「推荐」一致时可填）。</summary>
        public const string TuanjieProductRecommended = "1.8.5";

        /// <summary>允许向下兼容的团结产品小版本（需自行工程验证）。</summary>
        public const string TuanjieProductTested = "1.8.4, 1.8.5";

        public static SoulCoreChannel Channel => _resolveChannel();

        public static string UnityVersionString => Application.unityVersion ?? string.Empty;

        public static string ChannelLabel => Channel switch
        {
            SoulCoreChannel.TuanjieEditor => "团结引擎 (Tuanjie)",
            SoulCoreChannel.UnityEditorInternational => "Unity 国际版",
            _ => "未识别 (见 Documentation~/… 或设 Scripting Define)"
        };

        private static SoulCoreChannel _resolveChannel()
        {
#if SOULCORE_CHANNEL_TUANJIE
            return SoulCoreChannel.TuanjieEditor;
#elif SOULCORE_CHANNEL_UNITY
            return SoulCoreChannel.UnityEditorInternational;
#else
            var v = Application.unityVersion ?? string.Empty;
            if (v.IndexOf("c1", StringComparison.Ordinal) >= 0) return SoulCoreChannel.TuanjieEditor;
            if (v.IndexOf("Tuanjie", StringComparison.OrdinalIgnoreCase) >= 0) return SoulCoreChannel.TuanjieEditor;
            if (v.Length > 0) return SoulCoreChannel.UnityEditorInternational;
            return SoulCoreChannel.Unknown;
#endif
        }
    }
}

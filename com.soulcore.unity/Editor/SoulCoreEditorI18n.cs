using System.Collections.Generic;
using SoulCore;
using UnityEditor;
using UnityEngine;

namespace SoulCore.Editor
{
    /// <summary>
    /// 编辑器界面文案：团结渠道恒为简体中文；Unity 国际由 Project Settings「魂核」或 EditorPrefs「SoulCore.EditorLang」控制。
    /// </summary>
    public static class SoulCoreEditorI18n
    {
        public const string EditorPrefsLangKey = "SoulCore.EditorLang";

        private static readonly Dictionary<string, (string zh, string en)> Table = new()
        {
            ["ed.settings.title"] = ("魂核 · 语言 / Language", "Soul Core · Language"),
            ["ed.settings.tuanjie"] = ("团结引擎：界面与插件文案固定为简体中文（与 Unity 国际版包结构相同，仅文案不同）。", "Tuanjie: plugin UI is fixed to Simplified Chinese."),
            ["ed.settings.unity"] = ("选择 Asset 与 Inspector 中本包使用的语言（运行时 Perceive 提示等使用同一设置，会写入 PlayerPrefs）。", "Language for this package in Project window & Inspector (also syncs runtime tooltips to PlayerPrefs)."),
            ["ed.lang.zh"] = ("简体中文", "简体中文 (Chinese)"),
            ["ed.lang.en"] = ("English", "English"),

            ["ed.banner.tj.title"] = ("魂核 · 当前按「团结引擎」渠道识别 / 运行。", "Channel: Tuanjie Engine."),
            ["ed.banner.tj.body"] = ("目标: 团结 {0}（推荐 {1}）· 内核 ≥ {2}\n说明: Documentation~/Tuanjie-1.8.5-团结引擎.md", "Targets: Tuanjie {0} (recommended {1}) · core ≥ {2}\nDocs: Documentation~/Tuanjie-1.8.5-团结引擎.md"),
            ["ed.banner.unity.title"] = ("魂核 · 当前按「Unity 国际版」渠道识别 / 运行。", "Channel: Unity (international)."),
            ["ed.banner.unity.body"] = ("目标: Unity {0} LTS\n说明: Documentation~/Unity-2022.3-国际版.md", "Target: Unity {0} LTS\nDocs: Documentation~/Unity-2022.3-国际版.md"),
            ["ed.banner.unknown"] = ("未识别渠道。可查看 Documentation~ 或菜单 SoulCore / 魂核 → Channel / 渠道。", "Unknown channel. See Documentation~ or SoulCore → Channel menu."),

            ["ed.inspect.runtimeFold"] = ("运行态调试 (仅 Play)", "Runtime debug (Play Mode)"),
            ["ed.inspect.playHint"] = ("进入 Play 后显示：主导情绪、记忆条数、对象池、上次决策、调度器队列等。", "Visible in Play: dominant emotion, memories, pool, last decision, scheduler queue."),
            ["ed.inspect.multi"] = ("多选时仅可查看序列化字段。", "Multi-edit: limited to serialized fields."),
            ["ed.inspect.soulNull"] = ("Soul 未初始化", "Soul is not initialized."),
            ["ed.inspect.id"] = ("身份", "Identity"),
            ["ed.inspect.idField"] = ("编号", "ID"),
            ["ed.inspect.stats"] = ("统计", "Statistics"),
            ["ed.inspect.exp"] = ("经历", "Experiences"),
            ["ed.inspect.dec"] = ("决策次数", "Decisions"),
            ["ed.inspect.day"] = ("当日互动", "Daily interactions"),
            ["ed.inspect.emoMem"] = ("情感 / 记忆", "Emotion / Memory"),
            ["ed.inspect.domEmo"] = ("主导情绪", "Dominant emotion"),
            ["ed.inspect.memCount"] = ("记忆条数", "Memory count"),
            ["ed.inspect.memForg"] = ("已遗忘(累计)", "Forgotten (total)"),
            ["ed.inspect.pool"] = ("记忆池 (空闲条数)", "Pooled memories"),
            ["ed.inspect.memMax"] = ("记忆上限", "Memory cap"),
            ["ed.inspect.relCh"] = ("关系变化(累计)", "Relationship changes"),
            ["ed.inspect.relTh"] = ("关系阈触发", "Relationship thresholds"),
            ["ed.inspect.last"] = ("上次感知", "Last perception"),
            ["ed.inspect.none"] = ("(尚无)", "(none)"),
            ["ed.inspect.action"] = ("行动", "Action"),
            ["ed.inspect.emoInt"] = ("情绪强度", "Emotion intensity"),
            ["ed.inspect.expl"] = ("说明", "Explanation"),
            ["ed.inspect.skip"] = ("最近跳过", "Last skip"),
            ["ed.inspect.sched"] = ("分帧调度器", "Frame scheduler"),
            ["ed.inspect.queue"] = ("排队条数", "Queued"),
            ["ed.inspect.perFrame"] = ("每帧上限", "Max / frame"),
            ["ed.inspect.copy"] = ("复制说明到剪贴板", "Copy profile to clipboard"),
            ["ed.inspect.alive"] = ("存活", "Alive"),
            ["ed.inspect.name"] = ("名称", "Name"),

            ["ed.npc.header.identity"] = ("身份", "Identity"),
            ["ed.npc.citizenId"] = ("公民或角色编号（内部 ID）", "Citizen / NPC ID (internal)"),
            ["ed.npc.displayName"] = ("显示名称（台词/日志里叫这个）", "Display name (shown in lines & logs)"),
            ["ed.npc.nameHint"] = ("「显示名称」= 对外称呼，会写进 Soul 与「某某决定……」；「公民或角色编号」= 内部键。改显示名称后请重新进入 Play。", "Display name → Soul.Name and decision text. Citizen ID is internal. Re-enter Play after changing display name."),
            ["ed.npc.header.openAgent"] = ("开放式智能体 API（可选）", "Open agent API (optional)"),
            ["ed.npc.openAgentHint"] = ("在此指定仅本物体使用的智能体配置；不指定则使用 Project Settings 或 `Resources/SoulOpenAgentDefault`。注意：该能力仅在「高级版 6.1.8」(SOUL_EDITION_PRO) 可用。", "Override profile for this NPC or use Project Settings/Resources. Available only in ADVANCED 6.1.8 (SOUL_EDITION_PRO)."),
            ["ed.npc.openAgentOverride"] = ("智能体配置 (覆盖工程默认)", "Open agent profile (overrides project default)"),
            ["ed.openAgent.title"] = ("开放式智能体 API", "Open agent API"),
            ["ed.openAgent.hint"] = ("创建后放到 `Resources/SoulOpenAgentDefault` 可全局加载；或只在「魂核 NPC」上指定覆盖。密钥勿提交到公开仓库。仅「高级版 6.1.8」会在运行时读取此配置。", "Put under Resources for global load, or assign per-NPC. Do not commit keys. Read at runtime only in ADVANCED 6.1.8."),
            ["ed.openAgent.createDefault"] = ("在 Resources 中创建/选中默认资源", "Create or select Resources default"),
            ["ed.openAgent.createAnywhere"] = ("创建配置资源(任意路径)", "Create config asset (any path)"),
            ["ed.npc.randomSeed"] = ("随机种子（0=自动）", "Random seed (0 = TickCount)"),
            ["ed.npc.config"] = ("配置资源", "Config (ScriptableObject)"),
            ["ed.npc.header.day"] = ("日更（按游戏内天）", "Daily reset (game days)"),
            ["ed.npc.secondsPerDay"] = ("一游戏日等于多少秒", "Seconds per game day"),
            ["ed.npc.runDaily"] = ("启用自动日更", "Auto daily reset"),
            ["ed.npc.dailyRange"] = ("仅在与焦点同距离内时日更", "Daily only in range"),
            ["ed.npc.header.res"] = ("资源稀缺度（0–1）", "Resource scarcity 0–1"),
            ["ed.npc.resourceScarcity"] = ("稀缺度", "Scarcity"),
            ["ed.npc.useScarcityProvider"] = ("用上方滑条作为资源稀缺度", "Use slider as resource provider"),
            ["ed.npc.header.perf"] = ("距离与感知频率", "Distance & perception rate"),
            ["ed.npc.maxDist"] = ("最大距离（米，0=不限制）", "Max distance (m, 0=unlimited)"),
            ["ed.npc.focus"] = ("焦点/玩家变换", "Focus / player transform"),
            ["ed.npc.maxPerSec"] = ("每秒最多感知次数（0=不限制）", "Max perceives / sec (0=unlimited)"),
            ["ed.npc.enforceBudget"] = ("启用距离与频率限制", "Enforce distance & rate budget"),
            ["ed.npc.header.sched"] = ("分帧调度（可选）", "Frame scheduler (optional)"),
            ["ed.npc.processScheduler"] = ("分帧调度器", "Scheduler reference"),
            ["ed.npc.autoEnsure"] = ("在唤醒时自动创建全局调度器", "Ensure global scheduler on Awake"),
            ["ed.npc.header.debug"] = ("调试", "Debug"),
            ["ed.npc.showDebug"] = ("显示调试信息", "Show debug status"),
            ["ed.npc.debugStatus"] = ("调试用状态文本", "Debug text"),

            ["ed.npc.flavorHeader"] = ("人性辅文案（玩笑/补句，不影响主决策）", "Flavor (jokes & extra lines; not main decision)"),
            ["ed.npc.flavorMode"] = ("辅文案来源", "Flavor source"),
            ["ed.npc.flavorModeTip"] = ("内置：用引擎随机轻量模板。自设：不生成，由你的界面/台本填。", "Built-in: random templates. Custom: leave null for your own copy/UI."),
            ["ed.npc.flavorEnumBuiltIn"] = ("内置轻量模板", "Built-in templates"),
            ["ed.npc.flavorEnumCustom"] = ("不生成、留空自填", "Custom (leave empty)"),
        };

        /// <summary>未在 Project 设置里选过语言时，默认使用中文（与 package 主市场一致；国际版可在 Project Settings 切 English）。</summary>
        public static bool UseChinese =>
            SoulCoreEngineInfo.Channel == SoulCoreChannel.TuanjieEditor ||
            EditorPrefs.GetString(EditorPrefsLangKey, SoulCoreLocalization.LangZh) == SoulCoreLocalization.LangZh;

        public static string Ed(string key)
        {
            if (!Table.TryGetValue(key, out var pair))
                return key;
            return UseChinese ? pair.zh : pair.en;
        }

        public static GUIContent Gui(string key, string tooltipKey = null)
        {
            var t = tooltipKey != null ? Ed(tooltipKey) : null;
            return new GUIContent(Ed(key), string.IsNullOrEmpty(t) ? null : t);
        }

        public static void ApplyEditorLangToPlayerPrefs()
        {
            SoulCoreLocalization.SetRuntimeLanguage(UseChinese ? SoulCoreLocalization.LangZh : SoulCoreLocalization.LangEn);
        }
    }
}

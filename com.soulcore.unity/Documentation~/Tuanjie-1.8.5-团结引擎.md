# 意识流魂核 — 团结引擎 1.8.5 / 1.8.4

## 目标环境

- **团结引擎产品版本**：推荐在 **1.8.5** 上完成验证；**1.8.4 通常可用**（同一大版本内、未使用 1.8.5 独占 API 时，与之前说明一致，仍建议在 1.8.4 工程内编译+跑通一遍）。
- **内核**：与 Unity 国际版一样属于 **2022.3 LTS 系**；Hub 中常见为 **2022.3.x…c1** 形式（`c1` 表示中国渠道/团结构建），本包在运行时**据此倾向识别为「团结」**（见 `SoulCoreEngineInfo.Channel`）。

## 使用方式

- 与 Unity 国际版 **共用同一 UPM 包** `com.soulcore.unity`；发行到 **团结资源商店** 时，仅 **商店元数据/截图/合规** 与 Unity Asset Store 区分即可，**包内代码不强制二选一**。

## 渠道识别与强制

- **自动（启发式）**：`Application.unityVersion` 含 `c1` 或 `Tuanjie` 时，**SoulInspector** 等界面会标为「团结」。
- **强制为团结**（避免误判）：在 **Player Settings → Other Settings → Scripting Define Symbols** 中增加：  
  `SOULCORE_CHANNEL_TUANJIE`  
  并**不要**同时定义 `SOULCORE_CHANNEL_UNITY`。
- **强制为 Unity 国际**（在团结 Editor 里仍想按国际版逻辑分支时）：`SOULCORE_CHANNEL_UNITY`（与上互斥）。

## 与 Unity 国际版关系

- 同一 C# 与 `.asmdef`；**最低内核线** 以 `package.json` 的 `unity: "2022.3"` 为准，与团结 **1.8.4/1.8.5** 对应的 Editor 需满足该 LTS 线。

## 界面语言

- **团结渠道**下，本包 **Inspector、Project Settings、AddComponentMenu（在定义了 `SOULCORE_CHANNEL_TUANJIE` 时）** 等以 **简体中文** 为主；运行时 `Perceive` 等提示亦恒为中文（与 `SoulCoreLocalization` 行为一致）。

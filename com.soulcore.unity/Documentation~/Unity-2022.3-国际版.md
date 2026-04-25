# 意识流魂核 — Unity 国际版

## 目标环境

- **Unity Editor：2022.3 LTS 系**（`package.json` 中 `unity: "2022.3"` 表示**最低**版本线请不低于 2022.3）
- 安装来源：**Unity Hub 国际** / 官方 LTS 安装包
- 典型版本号示例：`2022.3.62f1` 等（**无**中国渠道常见的 `c1` 尾缀时，本包会按 **Unity 国际版** 进行渠道识别，见 `SoulCoreEngineInfo`）

## 使用方式

- 与团结版 **共用同一 UPM 包** `com.soulcore.unity`；**不在包名中区分** Unity / 团结，由 Editor 与文档区分使用场景即可。
- 将包加入工程 `Packages/manifest.json` 的 `dependencies` 后导入即可；示例见根目录 `Samples~/BasicDemo`。

## 与团结版的差异（工程侧）

- 商店、合规与发行渠道在 **你方工程** 与 **市场元数据** 中区分；**运行时 API 一致**。
- 若需**强制**将渠道标为国际版，可在 **Player Settings → Scripting Define Symbols** 中增加：  
  `SOULCORE_CHANNEL_UNITY`  
  并**不要**同时定义 `SOULCORE_CHANNEL_TUANJIE`（详见团结引擎说明文档）。

## 界面语言（中英可切）

- 打开 **Edit → Project Settings → Soul Core 魂核**，在 **Unity 国际版** 下可选择 **English / 简体中文**；会写入 `EditorPrefs` 与 `PlayerPrefs`（`SoulCore.Lang`），用于 **Inspector 文案**与 **运行时** `SoulCoreLocalization`（如跳过感知、灵魂说明等）。
- 菜单除中文路径外，另提供 **SoulCore/…** 英文路径，便于英文环境查找。

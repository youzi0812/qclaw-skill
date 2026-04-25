# 免费版 / 标准版 / 高级版 — 发布流程（同一套源码）

本仓库 **`com.soulcore.unity`** 通过编译宏区分产品形态：

| 形态 | 宏状态 | 主要能力 |
|------|-----|------------------------|
| **免费版 4.0** | 定义 `SOUL_EDITION_FREE` | 同场景最多 **10** 个有效 `SoulNPC`；**无**快照存档 |
| **标准版 4.0（默认）** | 不定义 `SOUL_EDITION_FREE` / `SOUL_EDITION_PRO` | NPC 数量不限；快照存档可用 |
| **高级版 6.1.8** | 定义 `SOUL_EDITION_PRO` | 含标准版能力，并可在运行时读取开放式智能体 API 配置（`SoulOpenAgentProfile`） |

单机无法 100% 防篡改；以**商店描述 + EULA + 交付物**为主。

## 在工程里切换（发包前）

菜单：

- **魂核 → 产品版本 → 切换为 免费版 编译** — 为若干平台组加入 `SOUL_EDITION_FREE`，等待脚本重编。
- **魂核 → 产品版本 → 切换为 标准版 编译** — 移除该宏。
- **魂核 → 产品版本 → 切换为 高级版 6.1.8 编译** — 加入 `SOUL_EDITION_PRO`（并移除 `SOUL_EDITION_FREE`）。

英文路径：**SoulCore → Product → …**

## 导出到商店

1. **免费资产 4.0**：切到免费版，确认第 11 个 `SoulNPC` 无 Soul 且出现一次提示，再导出。
2. **标准资产 4.0**：切到标准版，确认快照可用再导出。
3. **高级资产 6.1.8**：切到高级版，确认 `SoulOpenAgent.ResolveFor` 能读到配置再导出。

## 可选：元包 `com.soulcore.free`

若使用同目录下的 **`com.soulcore.free`**（依赖 `com.soulcore.unity`），用于在说明里引导「先装依赖再装元包」；也可仅使用本说明 + 菜单，不单独发元包。

## 玩家端行为

- 第 11 个及以后的 `SoulNPC`：`Awake` 不创建 `Soul`，可认 `IsBlockedByFreeEdition`。
- 首次触达上限或首次调用存档 API：运行时 **OnGUI** 提示（每类各提示一次，见 `PlayerPrefs` 键）。
- 高级版下 `SoulOpenAgent.ResolveFor` 才会返回配置；免费/标准版会返回 `null`（即便你创建了资源）。

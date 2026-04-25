# 三档发布速查（4.0 免费 / 4.0 标准 / 6.1.8 高级）

菜单统一入口：`魂核 → 产品版本`

- 切换为 免费版 编译：加入 `SOUL_EDITION_FREE`
- 切换为 标准版 编译：移除 `SOUL_EDITION_FREE` 与 `SOUL_EDITION_PRO`
- 切换为 高级版 6.1.8 编译：加入 `SOUL_EDITION_PRO`

核心能力差异：

- 免费版 4.0：最多 10 个有效 NPC，快照不可用
- 标准版 4.0：NPC 不限，快照可用
- 高级版 6.1.8：含标准版能力 + 可读取开放式智能体配置（`SoulOpenAgentProfile`）

发布前建议最小验证：

1. 免费版：放 11 个 `SoulNPC`，确认第 11 个被限制。
2. 标准版：调用 `ExportSnapshot` / `ApplySnapshot` 正常。
3. 高级版：`SoulOpenAgent.ResolveFor(npc)` 可返回启用的 profile。

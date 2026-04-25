# OpenBehavior 插件使用者调参速查表（5分钟版）

本文面向接入 `OpenBehaviorDriver` 的项目同学，帮助快速把系统调到“能跑、可控、可迭代”状态。

## 一、最小可用配置

1. 场景放置一个 `AdvancedOrchestrator`。
2. 场景放置一个 `OpenBehaviorDriver`（建议只放一个全局驱动）。
3. 确保 NPC 挂有 `SoulNPC`，并能被驱动器收集到（自动收集或手动列表）。

## 二、优先调这 6 个参数

在 `OpenBehaviorDriver` 中优先调整：

1. `每分钟最大社交次数`：全局总量阀门，先控总噪声。
2. `每个NPC每分钟上限`：防单个 NPC 话痨。
3. `每对NPC每分钟上限`：防固定二人复读。
4. `社交冷却秒数`：控制节奏密度。
5. `任务NPC社交策略`：任务期间行为规则（无限制/范围限制/忙碌静默）。
6. `紧急任务强制静默优先`：关键任务 NPC 防打断（建议保持开启）。

## 三、任务 NPC 约束怎么配

`任务NPC社交策略` 推荐如下：

- 普通任务：`范围限制` + `任务NPC社交半径(米)`（建议 2~6）。
- 关键任务：`忙碌静默` + 配置 `紧急任务标签` 或 `紧急任务NPC直指定`。

任务识别可用 4 种方式：

- 标签
- 图层
- 直指定列表
- 自定义提供器（`IOpenBehaviorTaskConstraintProvider`）

## 四、区域化（A/B）建议

使用 `OpenBehaviorZoneVolume + OpenBehaviorZoneProfile` 做区域差异：

- 区域A（社交活跃）：可覆盖为 `无限制`，提高人格表达权重缩放。
- 区域B（克制专注）：维持 `忙碌静默` 或改为 `范围限制` 并缩小半径。

这样通常可以做到“同一套驱动，不同区域不同气质”。

## 五、人格表达建议

在 `OpenBehaviorDriver` 打开“人格参与社交表达”后，按需调：

- `人格影响目标选择`
- `人格影响对话语气`
- `人格影响话题偏好`

建议先用 `OpenBehaviorPersonalityPreset` 预设资产起步，再微调组件参数。

## 六、诊断排障开关（建议仅联调时开启）

- `驱动诊断日志`
- `诊断附带人格信号`
- `任务约束诊断日志`
- `诊断附带人格生效权重`

排障口诀：

- 看 `isTaskNpc / isUrgentTaskNpc` 是否命中；
- 看 `effectivePolicy` 是否符合预期（是否被区域覆盖）；
- 看 `willSilenceSocialize` 与 `urgentForceSilent` 是否一致；
- 看 `urgentSilentHits / urgentSilentPerMin` 是否按预期增长。

## 七、上线前建议

1. 先关掉全部诊断日志，避免刷屏。
2. 保留 `紧急任务强制静默优先=开`。
3. 用 A/B 区域各跑一次 5~10 分钟，观察是否出现过密社交或任务 NPC 脱线。
4. 预设参数资产化（人格预设、区域配置），不要只改场景临时值。


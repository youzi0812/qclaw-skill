# 集成自己的 UI、对话与 Perceive

## 建议实施顺序 (1 → 4)

**① UI 接 `Perceive` / `TryEnqueuePerceive`**

- **包内示例脚本**：导入 Sample「基础输入演示」后，使用 **`DialogPerceiveUiExample`**（与 `BasicDemoInput` 同文件夹）：挂到 Canvas 或任意物体，拖入 `SoulNPC`、`InputField`、`Text`，在 **Button** 的 **OnClick** 里绑定该脚本的 **`OnSendClicked`**。  
- 亦可自己新建脚本：`SerializeField` 引用 `SoulNPC`；发话后构造 `PerceptionContext`，用 **`Perceive`** 或 **`TryEnqueuePerceive`**，把 `Explanation` 填到 `Text` / `TMP_Text`。  
- 可删除或停用 `BasicDemoInput`，避免与正式输入重复。

**② Soul NPC 配置 (ScriptableObject)**

- 在 **Project** 中：**右键 → 创建 → 魂核 → Soul NPC 配置**（或菜单 **魂核/资源/…** 若你有类似扩展）。得到资源后调到人格预设、记忆上限、辅文案、模块开关等。  
- 把该资源拖到魂核的 **「配置资源」**；改完 **显示名称** 后仍建议 **重进 Play** 才见剧情名一致。  
- 当前已提供快捷按钮：在 `Soul NPC` Inspector 的「配置资源」下方可点 **创建并绑定 SoulNPCConfig**。

**③ 距离、焦点、分帧（有场景/移动时再做）**

- 在魂核上拖 **玩家 / 主目标** 的 `Transform` 到 **「焦点/玩家变换」**；**「最大距离（米）」** 设 &gt;0 则只在范围内日更/感知（以包内逻辑为准）。  
- 需要降低单帧尖刺：可勾 **在唤醒时自动创建全局调度器** 或场景里加 **分帧调度器** 并引用。
- 验证建议：Play 后移动焦点，观察 `LastSkipReason` 或运行态调试里是否出现跳过（距离/频限）。

**④ 接开放式智能体 HTTP（可选、最后做）**

- 在 **魂核 面板** 或 `Resources/SoulOpenAgentDefault` 配好 **`SoulOpenAgentProfile`**。  
- 在你方网络脚本中：`var p = SoulOpenAgent.ResolveFor(_npc);`，若 `p != null && p.Enabled` 再拼 JSON、发 `UnityWebRequest`，**与 `Perceive` 谁先谁后、如何合并** 由你方设计。详 **Documentation~/开放式智能体-API-配置.md**。
- 包内最小脚本：`OpenAgentHttpUiExample`（`Samples~/BasicDemo`）。可挂在按钮对象并绑定 `OnSendClicked`，先跑通“读取 profile + 发请求 + 回显”链路。

---

## 1. 名字在日志/台词里用哪一个？

- 决策里 **`某某决定……`** 中的 **`某某`** 来自 **Soul 的名字**（`Soul.Name`），在 Inspector 里就是 **显示名称**；若为空，唤醒时会用 `GameObject` 名。
- 要固定成 **「店小二」** 等：在 **魂核 NPC** 上填 **显示名称**（**不是**「公民或角色编号」；后者是内部 ID，如 `npc_01`）。改完 **显示名称** 后需 **重新进入 Play**，Soul 才会用新名字。
- 代码里请用 **`soulNPC.CharacterName`**，与 `Soul.Name`、台本里名字一致，不要只写 `name` / `transform.name`（那仅代表层级里的对象名）。

## 2. 同步决策：`Perceive`（同帧出结果）

```csharp
using SoulCore;
using UnityEngine;
using UnityEngine.UI;

public class MyDialogUi : MonoBehaviour
{
    [SerializeField] private SoulNPC _npc;
    [SerializeField] private Text _line;

    public void OnPlayerSaid(string text)
    {
        var ctx = new PerceptionContext("conversation", text, 0.6f, "player_1");
        var d = _npc.Perceive(ctx);
        if (d == null) return;
        _line.text = d.Explanation;           // 主说明
        // d.Action, d.Emotion, d.EmotionIntensity 等按需接 UI
    }
}
```

## 3. 分帧决策：`TryEnqueuePerceive`（避免单帧算太多）

有 **SoulProcessScheduler** 时（示例里用 `EnsureInHierarchy` 或场景里挂调度器）：

```csharp
var ctx = new PerceptionContext("conversation", text, 0.6f, "player_1");
_npc.TryEnqueuePerceive(ctx, d =>
{
    if (d == null) return;
    _line.text = d.Explanation;
}, bypassBudget: false);
```

`PerceptionContext` 含义：`type`（事件类）、`content`（文本/描述）、`intensity`（0~1 左右）、`userId`（谁发起的）。

## 4. 与 `Debug.Log` 无关

`BasicDemoInput` 里的 **`Debug.Log` 仅作示例**；你方游戏应把 **`SoulDecision`** 接到 UGUI、TextMeshPro、自研对话系统或网络层，**不必** 打日志。

## 5. 与开放式智能体 API 配置的关系

- 在 **Project Settings → Soul Core 魂核** 或 **魂核 NPC 面板** 里可配置 `SoulOpenAgentProfile`；**本包不发起网络请求**，仅存 URL/模型/密钥等。  
- 详见 **Documentation~/开放式智能体-API-配置.md**。

## 6. 中文显示名（行动/情绪键）

在团结或默认中文运行时，可用 `SoulCoreActionDisplay.Action(d.Action)`、`SoulCoreActionDisplay.Emotion(d.Emotion)` 把内部英文键变成短中文（与控制台示例相同逻辑）。

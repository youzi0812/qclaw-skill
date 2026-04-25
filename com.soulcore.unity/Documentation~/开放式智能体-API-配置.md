# 开放式智能体 API 配置

魂核的 **`Perceive` / 本地决策** 不依赖大模型。若要把 **HTTP 智能体**（如兼容 OpenAI 的网关、自建 Agent）和对话 UI 接在一起，请使用 **`SoulOpenAgentProfile`** 保存 URL、模型、超时、**API Key**、系统提示等，并在**你方**脚本里用 `UnityWebRequest` / `HttpClient` 发请求。

## 如何「创建」这份资源（重要）

- **`Create → 魂核 → …` 在「项目 / Project」窗口里**，不是 **Hierarchy（层级）** 里。在 **Project** 里先点中 `Assets` 或某文件夹 → **右键 → 创建 / Create** → 展开 **魂核** 子菜单。  
- 若仍找不到：用顶部菜单 **魂核 → 资源 → 创建 开放式智能体 API 配置**（会创建到当前在 Project 中选中的文件夹下，未选则 `Assets`）。  
- 或 **Edit → Project Settings → Soul Core 魂核** 页底部按钮。

## 三种放置方式（任选）

1. **工程级默认**：`Assets/Resources/SoulOpenAgentDefault.asset`（与类常量 `SoulOpenAgent.DefaultResourceName` 一致），由 `Resources.Load` 在运行时取到。  
2. **Project Settings → Soul Core 魂核**：底部「开放式智能体 API」有按钮可创建该 Resources 资源或任意路径资源。  
3. **每 NPC 覆盖**：在 **魂核 NPC (Soul NPC)** 面板的 **「智能体配置 (覆盖工程默认)」** 中指定一份 `SoulOpenAgentProfile`。

解析顺序：`NPC 上覆盖` → 否则 `Resources/SoulOpenAgentDefault`。

## 代码里取配置

```csharp
var profile = SoulOpenAgent.ResolveFor(mySoulNpc);
if (profile == null || !profile.Enabled) { /* 走纯本地 / 不调用 API */ return; }
// profile.BaseUrl, profile.Model, profile.ApiKey, profile.TimeoutSeconds ...
```

## 安全

- 不要把含真实 Key 的资源提交到**公开** Git。  
- 真机/商店包建议用环境变量、远程配置或服务器代理签 Token，而不是把长密钥打进客户端。

## 与「按顺序集成」的对应

1. 先用 **UI + `Perceive`** 跑通（见《集成-UI与Perceive》）。  
2. 再创建 **`SoulOpenAgentProfile`** 并在你方发 HTTP 时读取。  
3. 最后再把 LLM 回复与 `SoulDecision` 拼进同一条对话流（顺序由你方定）。

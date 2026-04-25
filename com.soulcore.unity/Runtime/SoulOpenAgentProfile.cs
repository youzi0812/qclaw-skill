using UnityEngine;

namespace SoulCore
{
    /// <summary>
    /// 开放式智能体（HTTP/兼容 OpenAI 等）的**配置**：URL、模型、超时、系统提示、密钥等。
    /// 本包不内置网络请求；你在游戏中读取本资源后自行发起 <c>UnityWebRequest</c> 等调用。
    /// </summary>
    [CreateAssetMenu(fileName = "SoulOpenAgentProfile", menuName = "魂核/开放式智能体 API 配置", order = 20)]
    public class SoulOpenAgentProfile : ScriptableObject
    {
        [Header("总开关")]
        [Tooltip("为真且由你方代码发起请求时，才使用本配置；魂核仍默认走本地规则决策。")]
        public bool Enabled;

        [Header("端点与模型")]
        [Tooltip("例如 https://api.openai.com/v1/chat/completions 或自建网关")]
        public string BaseUrl = "https://api.openai.com/v1/chat/completions";

        public string Model = "gpt-4o-mini";

        [Min(1)] public int TimeoutSeconds = 60;

        [Header("鉴权（勿提交到公开仓库；正式环境建议用 CI/密钥管理）")]
        [Tooltip("Bearer 或你方网关在 Header 中需要的密钥")]
        [TextArea(1, 3)] public string ApiKey = "";

        [Header("其它 Header（每行 键: 值 可选）")]
        [TextArea(2, 4)] public string OptionalHeaders = "";

        [Header("系统提示（可选，按你方对话协议拼接）")]
        [TextArea(3, 8)] public string SystemPrompt = "你是一个游戏中的 NPC 角色，回答简短、符合人设。";
    }
}

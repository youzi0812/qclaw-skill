using System.Collections;
using System.Collections.Generic;
using System.Text;
using SoulCore;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace SoulCore.Samples
{
    /// <summary>
    /// 第④最小示例：读取 SoulOpenAgentProfile 后发 HTTP，再与本地 Perceive 结果拼接显示。
    /// 注意：这里只给最小通路，response 解析按你实际网关 JSON 结构自行调整。
    /// </summary>
    public class OpenAgentHttpUiExample : MonoBehaviour
    {
        [SerializeField, InspectorName("NPC对象")] private SoulNPC _npc;
        [SerializeField, InspectorName("输入")] private InputField _input;
        [SerializeField, InspectorName("输出")] private Text _output;
        [SerializeField, InspectorName("文本网格输入")] private TMP_InputField _tmpInput;
        [SerializeField, InspectorName("文本网格输出")] private TMP_Text _tmpOutput;
        [SerializeField, InspectorName("聊天日志文本")] private Text _chatLogText;
        [SerializeField, InspectorName("聊天日志文本网格")] private TMP_Text _chatLogTmpText;
        [SerializeField, InspectorName("聊天滚动区")] private ScrollRect _chatScrollRect;
        [SerializeField, InspectorName("发送按钮")] private Button _sendButton;
        [SerializeField, InspectorName("玩家编号")] private string _playerId = "player_1";
        [SerializeField, InspectorName("失败时回退本地感知")] private bool _alsoRunLocalPerceive = true;
        [SerializeField, InspectorName("显示状态提示")] private bool _showStatusTips = true;
        [SerializeField, InspectorName("界面显示详细网络错误")] private bool _showDetailedHttpErrorInUi = false;
        [SerializeField, InspectorName("最大令牌数")] private int _maxTokens = 220;
        [SerializeField, InspectorName("温度")] private float _temperature = 0.9f;
        [SerializeField, InspectorName("回复长度")] private ReplyLengthPreset _replyLength = ReplyLengthPreset.Standard;
        [SerializeField, InspectorName("轮次记忆数")] private int _historyTurns = 4;
        [SerializeField, InspectorName("记忆模式")] private MemoryMode _memoryMode = MemoryMode.Dynamic;
        [SerializeField, InspectorName("记忆召回条数")] private int _memoryRecallTopK = 6;
        [SerializeField, InspectorName("记忆预算字符数")] private int _memoryCharBudget = 900;
        [SerializeField, Range(1, 10), InspectorName("记忆最小重要度")] private int _memoryMinImportance = 4;
        [SerializeField, InspectorName("记忆去重")] private bool _deduplicateMemories = true;
        [SerializeField, InspectorName("最大聊天行数")] private int _maxChatLines = 40;
        private readonly List<ChatMessage> _history = new List<ChatMessage>();
        private readonly List<string> _chatLines = new List<string>();
        private bool _isSending;

        private enum ReplyLengthPreset
        {
            [InspectorName("简短")]
            Short = 0,
            [InspectorName("标准")]
            Standard = 1,
            [InspectorName("详细")]
            Detailed = 2
        }

        private enum MemoryMode
        {
            [InspectorName("仅轮次记忆")]
            TurnsOnly = 0,
            [InspectorName("动态记忆（推荐）")]
            Dynamic = 1,
            [InspectorName("长期记忆")]
            LongTerm = 2
        }

        [System.Serializable]
        private class ChatMessage
        {
            public string role;
            public string content;
        }

        [System.Serializable]
        private class Req
        {
            public string model;
            public ChatMessage[] messages;
            public int max_tokens;
            public float temperature;
        }

        [System.Serializable]
        private class RespMessage
        {
            public string role;
            public string content;
        }

        [System.Serializable]
        private class RespChoice
        {
            public int index;
            public RespMessage message;
            public string finish_reason;
        }

        [System.Serializable]
        private class Resp
        {
            public string id;
            public string model;
            public RespChoice[] choices;
        }

        public void OnSendClicked()
        {
            if (_npc == null) return;
            if (_isSending)
            {
                if (_showStatusTips) _setOut("请求进行中，请稍候...");
                return;
            }
            var text = _readInputText();
            if (string.IsNullOrWhiteSpace(text)) text = "你好";
            _appendChatLine("你", text);
            StartCoroutine(_send(text));
        }

        public void OnClearHistoryClicked()
        {
            _history.Clear();
            _chatLines.Clear();
            _refreshChatLog();
            _setOut("已清空对话记忆。");
        }

        private IEnumerator _send(string userText)
        {
            _setSending(true);
            if (_showStatusTips) _setOut("发送中...");
            var profile = SoulOpenAgent.ResolveFor(_npc);
            if (profile == null || !profile.Enabled)
            {
                var msg = "未启用高级版 API 配置，改走本地 Perceive。";
                if (_alsoRunLocalPerceive)
                {
                    msg = _buildFallbackText(msg, _runLocal(userText));
                }
                _setOut(msg);
                _setSending(false);
                yield break;
            }

            var messages = new List<ChatMessage>
            {
                new ChatMessage
                {
                    role = "system",
                    content = _buildSystemPrompt(profile)
                }
            };
            _appendMemoryContext(messages, userText);
            messages.Add(new ChatMessage
            {
                role = "user",
                content = userText
            });

            var reqBody = new Req
            {
                model = string.IsNullOrEmpty(profile.Model) ? "gpt-4o-mini" : profile.Model,
                messages = messages.ToArray(),
                max_tokens = _resolveMaxTokens(),
                temperature = Mathf.Clamp(_temperature, 0f, 2f)
            };
            var json = JsonUtility.ToJson(reqBody);
            Debug.Log("[SoulCore/OpenAgent] 开始请求 URL=" + profile.BaseUrl + " model=" + reqBody.model);

            using (var req = new UnityWebRequest(profile.BaseUrl, UnityWebRequest.kHttpVerbPOST))
            {
                var data = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(data);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.timeout = Mathf.Max(1, profile.TimeoutSeconds);
                req.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrEmpty(profile.ApiKey))
                    req.SetRequestHeader("Authorization", "Bearer " + profile.ApiKey);

                if (!string.IsNullOrEmpty(profile.OptionalHeaders))
                {
                    var lines = profile.OptionalHeaders.Split('\n');
                    foreach (var l in lines)
                    {
                        var t = l.Trim();
                        if (string.IsNullOrEmpty(t)) continue;
                        var i = t.IndexOf(':');
                        if (i <= 0 || i >= t.Length - 1) continue;
                        var k = t.Substring(0, i).Trim();
                        var v = t.Substring(i + 1).Trim();
                        if (k.Length > 0) req.SetRequestHeader(k, v);
                    }
                }

                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    var fullErr = _buildHttpError(req, profile.BaseUrl, json);
                    Debug.LogError("[SoulCore/OpenAgent] " + fullErr);
                    var msg = _showDetailedHttpErrorInUi ? fullErr : _buildFriendlyHttpError(req);
                    if (_alsoRunLocalPerceive)
                    {
                        var briefReason = "HTTP " + req.responseCode + " / " + req.error;
                        msg = _buildFallbackText("云端请求失败（" + briefReason + "）", _runLocal(userText));
                    }
                    _setOut(msg);
                    _setSending(false);
                    yield break;
                }

                var resp = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
                var assistantText = _extractAssistantText(resp);
                Debug.Log("[SoulCore/OpenAgent] 请求成功, code=" + req.responseCode);
                if (string.IsNullOrEmpty(assistantText))
                {
                    _setOut("【API原始返回】\n" + resp);
                    _appendChatLine("NPC", "【API原始返回】" + resp);
                }
                else
                {
                    _setOut(assistantText);
                    _appendChatLine("NPC", assistantText);
                    _pushHistory("user", userText);
                    _pushHistory("assistant", assistantText);
                }
            }
            _setSending(false);
        }

        private string _buildHttpError(UnityWebRequest req, string url, string requestJson)
        {
            var resp = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
            if (string.IsNullOrEmpty(resp)) resp = "<empty>";
            return "HTTP 请求失败\n"
                   + "URL: " + url + "\n"
                   + "状态码: " + req.responseCode + "\n"
                   + "结果: " + req.result + "\n"
                   + "错误: " + req.error + "\n"
                   + "响应体: " + resp + "\n"
                   + "请求体: " + requestJson;
        }

        private string _runLocal(string userText)
        {
            var ctx = new PerceptionContext("conversation", userText, 0.6f, _playerId);
            var d = _npc.Perceive(ctx);
            if (d == null) return "【本地】无返回";
            return "【本地】" + d.Explanation;
        }

        private string _extractAssistantText(string respJson)
        {
            if (string.IsNullOrEmpty(respJson)) return string.Empty;
            try
            {
                var resp = JsonUtility.FromJson<Resp>(respJson);
                if (resp == null || resp.choices == null || resp.choices.Length == 0) return string.Empty;
                var msg = resp.choices[0] != null ? resp.choices[0].message : null;
                if (msg == null || string.IsNullOrEmpty(msg.content)) return string.Empty;
                return msg.content.Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private int _resolveMaxTokens()
        {
            switch (_replyLength)
            {
                case ReplyLengthPreset.Short:
                    return Mathf.Max(64, Mathf.Min(_maxTokens, 120));
                case ReplyLengthPreset.Detailed:
                    return Mathf.Max(220, _maxTokens);
                default:
                    return Mathf.Max(120, _maxTokens);
            }
        }

        private string _buildSystemPrompt(SoulOpenAgentProfile profile)
        {
            var basePrompt = profile.SystemPrompt;
            if (string.IsNullOrWhiteSpace(basePrompt))
            {
                basePrompt = "你是一个游戏中的 NPC 角色，请使用自然中文并保持人设。";
            }

            string lengthRule;
            switch (_replyLength)
            {
                case ReplyLengthPreset.Short:
                    lengthRule = "回复控制在1-2句，简洁但不生硬。";
                    break;
                case ReplyLengthPreset.Detailed:
                    lengthRule = "回复不少于3句，增加场景细节和情绪描写，避免空话。";
                    break;
                default:
                    lengthRule = "回复不少于2句，包含一定细节，避免只回很短的一句话。";
                    break;
            }

            return basePrompt + " " + lengthRule;
        }

        private void _appendHistory(List<ChatMessage> target)
        {
            if (target == null || _history.Count == 0) return;
            for (var i = 0; i < _history.Count; i++)
            {
                var item = _history[i];
                if (item == null || string.IsNullOrEmpty(item.role) || string.IsNullOrEmpty(item.content)) continue;
                target.Add(new ChatMessage { role = item.role, content = item.content });
            }
        }

        private void _appendMemoryContext(List<ChatMessage> target, string userText)
        {
            if (target == null) return;
            switch (_memoryMode)
            {
                case MemoryMode.TurnsOnly:
                    _appendHistory(target);
                    break;
                case MemoryMode.Dynamic:
                    _appendHistory(target);
                    _appendDynamicSoulMemory(target, userText);
                    break;
                case MemoryMode.LongTerm:
                    _appendLongTermSoulMemory(target, userText);
                    _appendHistory(target);
                    break;
            }
        }

        private void _appendDynamicSoulMemory(List<ChatMessage> target, string userText)
        {
            var soul = _npc != null ? _npc.Soul : null;
            if (soul == null || soul.Memory == null) return;
            var topK = Mathf.Clamp(_memoryRecallTopK, 1, 20);
            var hits = soul.Memory.Recall(userText ?? string.Empty, null, topK);
            if (hits == null || hits.Count == 0) return;

            var budget = _resolveMemoryCharBudget();
            var used = 0;
            var sb = new StringBuilder(256);
            _appendBoundedLine(sb, "以下是与当前输入相关的 NPC 记忆，请保持一致性：", ref used, budget);
            var seen = _deduplicateMemories ? new HashSet<string>() : null;
            var added = 0;
            for (var i = 0; i < hits.Count; i++)
            {
                var m = hits[i];
                if (_tryAppendMemoryBullet(sb, seen, m, ref used, budget)) added++;
            }
            if (added <= 0) return;

            target.Add(new ChatMessage
            {
                role = "system",
                content = sb.ToString().Trim()
            });
        }

        private void _appendLongTermSoulMemory(List<ChatMessage> target, string userText)
        {
            var soul = _npc != null ? _npc.Soul : null;
            if (soul == null || soul.Memory == null) return;

            var budget = _resolveMemoryCharBudget();
            var used = 0;
            var sb = new StringBuilder(512);
            _appendBoundedLine(sb, "以下是 NPC 的长期记忆概要，请优先保持角色长期一致性。", ref used, budget);
            _appendBoundedLine(sb, "角色名: " + soul.Name, ref used, budget);

            var desc = soul.Explain();
            if (!string.IsNullOrEmpty(desc))
            {
                if (desc.Length > 420) desc = desc.Substring(0, 420) + "...";
                _appendBoundedLine(sb, desc, ref used, budget);
            }

            var hits = soul.Memory.Recall(userText ?? string.Empty, null, Mathf.Clamp(_memoryRecallTopK, 3, 24));
            var seen = _deduplicateMemories ? new HashSet<string>() : null;
            var added = 0;
            if (hits != null && hits.Count > 0)
            {
                _appendBoundedLine(sb, "关键记忆:", ref used, budget);
                for (var i = 0; i < hits.Count; i++)
                {
                    var m = hits[i];
                    if (_tryAppendMemoryBullet(sb, seen, m, ref used, budget)) added++;
                }
            }
            if (added <= 0 && used <= 0) return;

            target.Add(new ChatMessage
            {
                role = "system",
                content = sb.ToString().Trim()
            });
        }

        private int _resolveMemoryCharBudget() => Mathf.Max(200, _memoryCharBudget);

        private bool _appendBoundedLine(StringBuilder sb, string line, ref int used, int budget)
        {
            if (sb == null || string.IsNullOrEmpty(line)) return false;
            var t = line.Trim();
            if (t.Length == 0) return false;
            if (used + t.Length + 1 > budget) return false;
            sb.AppendLine(t);
            used += t.Length + 1;
            return true;
        }

        private bool _tryAppendMemoryBullet(
            StringBuilder sb,
            HashSet<string> seen,
            Memory memory,
            ref int used,
            int budget)
        {
            if (memory == null || string.IsNullOrEmpty(memory.Content)) return false;
            if (memory.Importance < Mathf.Clamp(_memoryMinImportance, 1, 10)) return false;
            var content = memory.Content.Trim();
            if (content.Length == 0) return false;
            if (seen != null && !seen.Add(content)) return false;
            return _appendBoundedLine(sb, "- " + content, ref used, budget);
        }

        private void _pushHistory(string role, string content)
        {
            if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(content)) return;
            _history.Add(new ChatMessage { role = role, content = content });
            var maxMessages = Mathf.Max(0, _historyTurns) * 2;
            if (maxMessages <= 0)
            {
                _history.Clear();
                return;
            }

            while (_history.Count > maxMessages)
            {
                _history.RemoveAt(0);
            }
        }

        private void _setOut(string text)
        {
            var t = text ?? string.Empty;
            if (_output != null) _output.text = t;
            if (_tmpOutput != null) _tmpOutput.text = t;
        }

        private void _appendChatLine(string speaker, string text)
        {
            var who = string.IsNullOrEmpty(speaker) ? "系统" : speaker;
            var body = string.IsNullOrEmpty(text) ? string.Empty : text.Trim();
            _chatLines.Add(who + "： " + body);
            var max = Mathf.Max(1, _maxChatLines);
            while (_chatLines.Count > max)
            {
                _chatLines.RemoveAt(0);
            }
            _refreshChatLog();
        }

        private void _refreshChatLog()
        {
            if (_chatLogText == null && _chatLogTmpText == null) return;
            var sb = new StringBuilder(512);
            for (var i = 0; i < _chatLines.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(_chatLines[i]);
            }

            var all = sb.ToString();
            if (_chatLogText != null) _chatLogText.text = all;
            if (_chatLogTmpText != null) _chatLogTmpText.text = all;
            StartCoroutine(_scrollToBottomNextFrame());
        }

        private IEnumerator _scrollToBottomNextFrame()
        {
            if (_chatScrollRect == null) yield break;
            yield return null;
            _chatScrollRect.verticalNormalizedPosition = 0f;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_chatScrollRect.content);
            _chatScrollRect.verticalNormalizedPosition = 0f;
        }

        private void _setSending(bool sending)
        {
            _isSending = sending;
            if (_sendButton != null) _sendButton.interactable = !sending;
            if (_input != null) _input.interactable = !sending;
            if (_tmpInput != null) _tmpInput.interactable = !sending;
        }

        private string _buildFallbackText(string reason, string localText)
        {
            var local = string.IsNullOrEmpty(localText) ? "【本地】无返回" : localText;
            _appendChatLine("系统", "云端不可用，已自动切换本地。");
            _appendChatLine("NPC", local);
            return "【云端不可用，已自动切换本地】\n"
                   + local
                   + "\n\n"
                   + "原因: " + reason;
        }

        private string _buildFriendlyHttpError(UnityWebRequest req)
        {
            if (req == null) return "云端请求失败，请稍后重试。";
            if (req.responseCode == 401) return "云端鉴权失败（401）：请检查 API Key 是否正确。";
            if (req.responseCode == 404) return "云端地址无效（404）：请检查 BaseUrl 路径。";
            if (req.result == UnityWebRequest.Result.ConnectionError) return "网络连接失败：请检查网络或代理设置。";
            if (req.result == UnityWebRequest.Result.DataProcessingError) return "响应解析失败：请稍后重试。";
            return "云端请求失败（HTTP " + req.responseCode + "）：请查看 Console 获取详细信息。";
        }

        private string _readInputText()
        {
            if (_tmpInput != null) return _tmpInput.text;
            if (_input != null) return _input.text;
            return string.Empty;
        }
    }
}

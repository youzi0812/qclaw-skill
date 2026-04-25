# QClaw 指令映射草案（v1）

本文定义“自然语言指令 -> Skill API”的最小映射规则，便于后续接入 QClaw。

## 1) 聊天回复（带人格与记忆）

- 用户指令示例：`按我一贯风格回复这封邮件`
- 映射接口：`POST /v1/chat`
- 关键字段：
  - `session_id`：会话 ID
  - `user_id`：用户 ID
  - `input`：用户输入
  - `persona_id`：人格模板（如 `formal_assistant`）
  - `memory_top_k`：建议 4~8

## 2) 记录偏好/事实

- 用户指令示例：`记住我以后邮件都用正式语气`
- 映射接口：`POST /v1/memory/write`
- 关键字段：
  - `content`：提炼后的记忆文本
  - `tags`：如 `style`, `mail`
  - `importance`：建议 0.7~0.95
  - `pinned`：核心偏好可设为 `true`

## 3) 查询历史记忆

- 用户指令示例：`我之前说过邮件要怎么写？`
- 映射接口：`POST /v1/memory/recall`
- 关键字段：
  - `query`：用户问题
  - `top_k`：建议 3~8

## 4) 管理记忆

- 列表：`GET /v1/memory/list?user_id={user_id}&limit=20&offset=0`
- 删除：`POST /v1/memory/delete`
- 置顶：`POST /v1/memory/pin`

## 5) 诊断追踪

- 用户指令示例：`刚才那条回复用了哪些记忆？`
- 映射接口：`GET /v1/diagnostics/last?session_id={session_id}`

## 建议

- 在 QClaw 端维护 `session_id`，便于诊断连续追踪。
- 将“明确偏好类指令”默认走 `memory.write`，再触发一次 `chat` 验证结果。
- 将用户主动强调“永远/必须/固定风格”的记忆默认 `pinned=true`。


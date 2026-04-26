# SoulCore QClaw Skill（MVP）

这个目录用于开发一个脱离 Unity/团结插件的 QClaw Skill 原型，目标是把 6.1.8 的核心能力抽象为通用 Agent 内核：

- 人格一致性
- 长期记忆回注
- 可观测诊断输出

## 目录说明

- `spec/skill-api-v1.json`：接口协议草案（请求/响应字段）
- `spec/implementation-plan.md`：分阶段实施计划
- `spec/qclaw-command-mapping.md`：QClaw 指令到接口的映射草案
- `server/app.py`：FastAPI 最小服务（MVP）
- `demo/demo_requests.py`：本地演示调用脚本
- `data/memory.db`：运行后自动创建的 SQLite 数据库

## MVP 范围（第一阶段）

1. `chat`：输入用户消息，输出人格一致回复
2. `memory.write`：写入长期记忆
3. `memory.recall`：按 query 召回记忆
4. `diagnostics.last`：查看最近一次回复的记忆命中与权重

## v3 增强（当前已实现）

- 写入去重：同一用户写入相同内容时会合并更新，不重复堆积
- 召回去重：同内容仅保留最高分命中
- 时间衰减：非置顶记忆会随时间轻微衰减
- 置顶记忆：`memory.write` 支持 `pinned=true`
- 管理接口：`memory.list` / `memory.delete` / `memory.pin`

## 非目标（当前不做）

- 自动操作本机文件/浏览器
- 多 Agent 协作编排
- 深度情绪仿真

## 下一步建议

1. 先把 `skill-api-v1.json` 定稿
2. 再实现一个本地 HTTP 服务（可用 Python/FastAPI 或 Node）
3. 最后接 QClaw Skill 调用适配层

## 本地快速运行

```bash
cd qclaw-skill
python -m pip install -r requirements.txt
python -m uvicorn server.app:app --host 127.0.0.1 --port 8000 --reload
```

另开一个终端运行 Demo：

```bash
cd qclaw-skill
python demo/demo_requests.py
```

v4 管理接口演示脚本：

```bash
cd qclaw-skill
python demo/demo_v4_manage.py
```

全链路联调演示脚本（health/chat/diagnostics/export/import）：

```bash
cd qclaw-skill
python demo/demo_e2e_full.py
```

快速烟雾检查（失败返回非 0，适合 CI）：

```bash
cd qclaw-skill
python demo/smoke_check.py
```

GitHub Actions 已提供最小自动验收工作流：

- `.github/workflows/qclaw-skill-smoke.yml`
- 触发条件：`main` 分支 push、相关 PR、手动触发

## 本地单元测试

```bash
cd qclaw-skill
python -m unittest discover -s tests -p "test_*.py"
```

## 一键本地检查（PowerShell）

在 `qclaw-skill` 目录执行：

```powershell
.\run_local_check.ps1
```

鉴权模式：

```powershell
.\run_local_check.ps1 -Mode auth -ApiKey "demo-key"
```

## 新增接口（交付收尾）

- `GET /v1/health`：服务健康检查
- `GET /v1/memory/export?user_id=...`：导出指定用户记忆
- `POST /v1/memory/import`：导入记忆（自动按内容合并去重）

## 历史数据清洗（建议从 v3 升级后执行一次）

```bash
cd qclaw-skill
python tools/migrate_v3_cleanup.py
```

清洗内容：

- 修复 `updated_at <= 0` 的旧记录
- 按 `user_id + content_hash` 合并重复记忆
- 合并后保留更高 `importance/hit_count` 与 `pinned` 状态

## 可选：接入真实 LLM（OpenAI 兼容）

默认不配置时，服务会使用内置模板回复。  
配置以下环境变量后，会自动调用真实模型：

```bash
set SOULCORE_LLM_BASE_URL=https://api.openai.com/v1/chat/completions
set SOULCORE_LLM_API_KEY=你的密钥
set SOULCORE_LLM_MODEL=gpt-4o-mini
set SOULCORE_LLM_TIMEOUT_SECONDS=20
```

说明：

- 只要设置了 `SOULCORE_LLM_BASE_URL`，即视为启用远端模型。
- 远端调用失败会自动回退到本地模板回复，不会中断接口。

## 接口鉴权（可选）

默认不配置时，不启用鉴权。  
设置以下环境变量后，除 `GET /v1/health` 外的接口都需要请求头 `x-api-key`：

```bash
set SOULCORE_API_KEY=你的密钥
```

统一错误响应结构示例：

```json
{
  "ok": false,
  "error": {
    "code": "AUTH_INVALID_API_KEY",
    "message": "Invalid API key"
  }
}
```

## 发布启动建议（鉴权与否）

### 方案 A：不启用鉴权（内网联调/快速演示）

```powershell
cd qclaw-skill
python -m uvicorn server.app:app --host 127.0.0.1 --port 8000
```

### 方案 B：启用鉴权（推荐对外服务）

```powershell
cd qclaw-skill
$env:SOULCORE_API_KEY="your-api-key"
python -m uvicorn server.app:app --host 127.0.0.1 --port 8000
```

客户端调用时必须带请求头：

```text
x-api-key: your-api-key
```

可通过健康接口确认当前是否启用鉴权（`auth_enabled` 字段）：

```bash
GET /v1/health
```


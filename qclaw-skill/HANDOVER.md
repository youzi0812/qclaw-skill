# QClaw Skill 交接说明

## 当前状态

已完成可运行的本地服务（FastAPI），支持：

- `chat`（人格一致性 + 记忆回注）
- `memory.write/recall/list/delete/pin`
- `memory.export/import`
- `diagnostics.last`
- `health`

并完成：

- 写入去重（按 `user_id + content_hash`）
- 召回去重（同内容仅保留最高分）
- 时间衰减 + 置顶记忆
- 历史数据清洗脚本

## 关键目录

- `server/app.py`：核心服务
- `demo/`：演示脚本
- `tools/migrate_v3_cleanup.py`：历史数据清洗
- `spec/`：接口协议与 QClaw 指令映射

## 新电脑继续开发步骤

1. 进入目录：`qclaw-skill`
2. 安装依赖：`python -m pip install -r requirements.txt`
3. 启动服务：`python -m uvicorn server.app:app --host 127.0.0.1 --port 8000 --reload`
4. 运行演示：
   - `python demo/demo_requests.py`
   - `python demo/demo_v4_manage.py`
5. 若有旧数据，执行：`python tools/migrate_v3_cleanup.py`

## 可选 LLM 配置

- `SOULCORE_LLM_BASE_URL`
- `SOULCORE_LLM_API_KEY`
- `SOULCORE_LLM_MODEL`
- `SOULCORE_LLM_TIMEOUT_SECONDS`

配置后会自动启用远端模型，失败则回退本地模板。


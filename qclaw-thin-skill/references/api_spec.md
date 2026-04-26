# SoulCore QClaw API Reference (Thin Skill)

Base URL: `http://127.0.0.1:8000`

If auth is enabled, send header:

```text
x-api-key: <SOULCORE_API_KEY>
```

## Health

`GET /v1/health`

Purpose:

- Check service availability
- Check `llm_enabled`
- Check `auth_enabled`

## Chat

`POST /v1/chat`

Request example:

```json
{
  "session_id": "s1",
  "user_id": "u1",
  "input": "帮我安排今天开发计划",
  "persona_id": "formal_assistant",
  "memory_top_k": 5,
  "options": {
    "enable_memory": true,
    "enable_persona_guard": true,
    "temperature": 0.6
  }
}
```

Response key fields:

- `reply`
- `used_memories`
- `persona_alignment`
- `trace_id`

## Memory Write

`POST /v1/memory/write`

Use for durable information:

- user style preference
- long-term workflow preference
- stable profile facts

## Memory Recall

`POST /v1/memory/recall`

Use to search relevant memory by query.

## Memory Management

- `GET /v1/memory/list?user_id=...&limit=20&offset=0`
- `POST /v1/memory/pin`
- `POST /v1/memory/delete`
- `GET /v1/memory/export?user_id=...`
- `POST /v1/memory/import`

## Diagnostics

`GET /v1/diagnostics/last?session_id=...`

Use after `chat` to inspect:

- `memory_candidates`
- `memory_used`
- `persona_alignment`
- `latency_ms`

## Unified Error Shape

```json
{
  "ok": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Request validation failed",
    "details": {
      "issues": []
    }
  }
}
```

Common codes:

- `AUTH_MISSING_API_KEY`
- `AUTH_INVALID_API_KEY`
- `VALIDATION_ERROR`
- `HTTP_ERROR`
- `INTERNAL_ERROR`

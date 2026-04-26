---
name: soulcore-memory-chat-router
description: Routes requests to the SoulCore QClaw HTTP backend for persona-consistent chat, long-term memory operations, and diagnostics. Use when users ask to remember preferences, retrieve historical context, or produce replies aligned with prior memory records.
---

# SoulCore Memory Chat Router

## Purpose

This skill is a thin routing layer.  
It does not replace the backend logic in `qclaw-skill/server/app.py`.

Use this skill when an AI runtime needs clear rules for when and how to call the SoulCore HTTP API.

## Backend Base URL

- Local default: `http://127.0.0.1:8000`
- If backend is deployed remotely, replace with the deployed base URL.

## Auth Rule

- If backend `auth_enabled=false`: no auth header required.
- If backend `auth_enabled=true`: send header `x-api-key: <SOULCORE_API_KEY>`.
- Always check `GET /v1/health` before first business request.

## Routing Rules

### 1) Persona-consistent reply

Use `POST /v1/chat` when user asks for a direct response and memory/context should be considered.

Required fields:

- `session_id`
- `user_id`
- `input`

Optional fields:

- `persona_id`
- `memory_top_k`
- `options.enable_memory`
- `options.enable_persona_guard`
- `options.temperature`

### 2) Write long-term preference or fact

Use `POST /v1/memory/write` when user explicitly provides durable preference/fact:

- style preference
- workflow preference
- long-lived profile info

Do not write short-lived one-off information unless user requests persistence.

### 3) Retrieve memory

Use `POST /v1/memory/recall` when user asks:

- "根据我之前偏好..."
- "你还记得我..."
- "按我的历史风格..."

### 4) Memory maintenance

- List: `GET /v1/memory/list`
- Pin/unpin: `POST /v1/memory/pin`
- Delete: `POST /v1/memory/delete`
- Export: `GET /v1/memory/export`
- Import: `POST /v1/memory/import`

### 5) Diagnostics

Use `GET /v1/diagnostics/last?session_id=...` after `chat` when debugging quality, checking memory hit count, or inspecting latency.

## Failure Handling

1. `401` auth errors:
   - Verify `x-api-key`.
   - Verify backend `auth_enabled`.
2. `422` validation errors:
   - Rebuild payload with required fields and retry once.
3. `5xx` or timeout:
   - Return graceful fallback message to user.
   - Avoid infinite retries; retry at most once.

## Minimal Call Sequence

1. `GET /v1/health`
2. (Optional) `POST /v1/memory/write` for durable preference
3. `POST /v1/chat`
4. (Optional) `GET /v1/diagnostics/last`

## Reference

- API details: [references/api_spec.md](references/api_spec.md)

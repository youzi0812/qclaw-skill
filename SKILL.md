---
name: soulcore-memory-chat-router
description: Routes requests to the SoulCore QClaw HTTP backend for persona-consistent chat, long-term memory operations, and diagnostics. Use when users ask to remember preferences, retrieve historical context, or produce replies aligned with prior memory records.
---

# SoulCore Memory Chat Router

This repository exposes a thin skill layer for the `qclaw-skill` FastAPI backend.

## Entry Rule

If your platform imports only one `SKILL.md`, use this root file as the entry point.

Primary detailed docs:

- `qclaw-thin-skill/SKILL.md`
- `qclaw-thin-skill/references/api_spec.md`

## Backend Base URL

- Local default: `http://127.0.0.1:8000`
- Replace with deployed URL in cloud environments.

For local-first users, start backend with:

- `qclaw-skill/start_local.bat` (Windows double-click)
- or `qclaw-skill/start_local.ps1 -Mode noauth`

## Auth Rule

- If `GET /v1/health` returns `auth_enabled=false`, no `x-api-key` needed.
- If `auth_enabled=true`, send header `x-api-key: <SOULCORE_API_KEY>`.

## Minimal Call Sequence

1. `GET /v1/health`
2. `POST /v1/chat`
3. Optional memory and diagnostics endpoints as needed

## Core Endpoints

- `POST /v1/chat`
- `POST /v1/memory/write`
- `POST /v1/memory/recall`
- `GET /v1/memory/list`
- `POST /v1/memory/pin`
- `POST /v1/memory/delete`
- `GET /v1/memory/export`
- `POST /v1/memory/import`
- `GET /v1/diagnostics/last`

## Error Handling

- `401`: verify `x-api-key` and `auth_enabled`
- `422`: fix payload fields and retry once
- `5xx/timeout`: return graceful fallback, avoid infinite retries

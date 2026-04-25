import hashlib
import json
import os
import sqlite3
import time
import uuid
from typing import List, Optional
from urllib import request as urlrequest
from urllib.error import URLError, HTTPError

from fastapi import FastAPI, HTTPException, Query
from pydantic import BaseModel, Field


BASE_DIR = os.path.dirname(os.path.dirname(__file__))
DATA_DIR = os.path.join(BASE_DIR, "data")
DB_PATH = os.path.join(DATA_DIR, "memory.db")


class LlmConfig:
    def __init__(self) -> None:
        self.base_url = os.getenv("SOULCORE_LLM_BASE_URL", "").strip()
        self.api_key = os.getenv("SOULCORE_LLM_API_KEY", "").strip()
        self.model = os.getenv("SOULCORE_LLM_MODEL", "gpt-4o-mini").strip()
        self.timeout_seconds = int(os.getenv("SOULCORE_LLM_TIMEOUT_SECONDS", "20"))
        self.enabled = bool(self.base_url)


LLM = LlmConfig()


def _ensure_db() -> None:
    os.makedirs(DATA_DIR, exist_ok=True)
    with sqlite3.connect(DB_PATH) as conn:
        conn.execute(
            """
            CREATE TABLE IF NOT EXISTS memories (
                memory_id TEXT PRIMARY KEY,
                user_id TEXT NOT NULL,
                content TEXT NOT NULL,
                content_hash TEXT NOT NULL,
                tags TEXT NOT NULL,
                importance REAL NOT NULL,
                pinned INTEGER NOT NULL DEFAULT 0,
                source TEXT NOT NULL,
                hit_count INTEGER NOT NULL DEFAULT 0,
                created_at INTEGER NOT NULL,
                updated_at INTEGER NOT NULL
            )
            """
        )
        cols = {
            row[1]
            for row in conn.execute("PRAGMA table_info(memories)").fetchall()
        }
        if "content_hash" not in cols:
            conn.execute("ALTER TABLE memories ADD COLUMN content_hash TEXT NOT NULL DEFAULT ''")
        if "pinned" not in cols:
            conn.execute("ALTER TABLE memories ADD COLUMN pinned INTEGER NOT NULL DEFAULT 0")
        if "hit_count" not in cols:
            conn.execute("ALTER TABLE memories ADD COLUMN hit_count INTEGER NOT NULL DEFAULT 0")
        if "updated_at" not in cols:
            conn.execute("ALTER TABLE memories ADD COLUMN updated_at INTEGER NOT NULL DEFAULT 0")
        conn.execute(
            """
            CREATE INDEX IF NOT EXISTS idx_memories_user_hash
            ON memories(user_id, content_hash)
            """
        )
        conn.execute(
            """
            CREATE TABLE IF NOT EXISTS diagnostics (
                session_id TEXT PRIMARY KEY,
                trace_id TEXT NOT NULL,
                input_summary TEXT NOT NULL,
                reply_summary TEXT NOT NULL,
                memory_candidates INTEGER NOT NULL,
                memory_used INTEGER NOT NULL,
                persona_alignment REAL NOT NULL,
                latency_ms INTEGER NOT NULL,
                updated_at INTEGER NOT NULL
            )
            """
        )


def _tokenize(text: str) -> set[str]:
    filtered = "".join(ch if ch.isalnum() else " " for ch in text.lower())
    return {w for w in filtered.split() if len(w) >= 2}


def _char_ngrams(text: str, n: int) -> set[str]:
    compact = "".join(ch for ch in text.lower() if not ch.isspace())
    if len(compact) < n:
        return {compact} if compact else set()
    return {compact[i : i + n] for i in range(0, len(compact) - n + 1)}


def _jaccard(a: set[str], b: set[str]) -> float:
    if not a or not b:
        return 0.0
    return len(a.intersection(b)) / max(1, len(a.union(b)))


def _score_memory(query: str, content: str, importance: float) -> float:
    q = _tokenize(query)
    c = _tokenize(content)
    token_j = _jaccard(q, c)
    bi_j = _jaccard(_char_ngrams(query, 2), _char_ngrams(content, 2))
    tri_j = _jaccard(_char_ngrams(query, 3), _char_ngrams(content, 3))
    mixed = token_j * 0.4 + bi_j * 0.4 + tri_j * 0.2
    return max(0.0, min(1.0, mixed * 0.8 + importance * 0.2))


def _persona_keywords(persona_id: str) -> List[str]:
    persona = (persona_id or "").lower()
    if "formal" in persona or "official" in persona:
        return ["请", "感谢", "建议", "可以", "您"]
    if "friendly" in persona or "warm" in persona:
        return ["我们", "一起", "可以", "好的", "我来"]
    return ["可以", "建议", "先", "然后"]


def _persona_alignment(reply: str, persona_id: str) -> tuple[float, List[str]]:
    keywords = _persona_keywords(persona_id)
    hits = 0
    for kw in keywords:
        if kw in reply:
            hits += 1
    score = hits / max(1, len(keywords))
    flags = []
    if score < 0.25:
        flags.append("persona_low_alignment")
    if score < 0.5:
        flags.append("persona_needs_rewrite")
    return score, flags


def _rewrite_with_persona(reply: str, persona_id: str) -> str:
    persona = (persona_id or "").lower()
    if "formal" in persona or "official" in persona:
        return f"建议如下：{reply} 如需我继续细化，我可以按步骤展开。"
    if "friendly" in persona or "warm" in persona:
        return f"我们可以这样做：{reply} 你要是愿意，我马上继续帮你细化。"
    return reply


def _safe_excerpt(text: str, max_len: int = 80) -> str:
    t = (text or "").strip().replace("\n", " ")
    if len(t) <= max_len:
        return t
    return t[:max_len] + "..."


def _normalize_content(text: str) -> str:
    return " ".join((text or "").strip().split())


def _content_hash(text: str) -> str:
    return hashlib.sha1(_normalize_content(text).lower().encode("utf-8")).hexdigest()


class ChatOptions(BaseModel):
    enable_memory: bool = True
    enable_persona_guard: bool = True
    temperature: float = 0.7


class ChatRequest(BaseModel):
    session_id: str
    user_id: str
    input: str = Field(min_length=1)
    persona_id: str = "default"
    memory_top_k: int = 6
    options: ChatOptions = ChatOptions()


class UsedMemory(BaseModel):
    memory_id: str
    score: float
    reason: str


class PersonaAlignment(BaseModel):
    score: float
    flags: List[str]


class ChatResponse(BaseModel):
    reply: str
    intent: str
    used_memories: List[UsedMemory]
    persona_alignment: PersonaAlignment
    trace_id: str


class MemoryWriteRequest(BaseModel):
    user_id: str
    content: str = Field(min_length=1)
    tags: List[str] = []
    importance: float = 0.75
    source: str = "chat"
    pinned: bool = False


class MemoryWriteResponse(BaseModel):
    memory_id: str
    stored: bool


class RecallRequest(BaseModel):
    user_id: str
    query: str = Field(min_length=1)
    top_k: int = 6


class RecallHit(BaseModel):
    memory_id: str
    content: str
    score: float
    tags: List[str]


class RecallResponse(BaseModel):
    hits: List[RecallHit]


class DiagnosticsResponse(BaseModel):
    trace_id: str
    input_summary: str
    reply_summary: str
    memory_candidates: int
    memory_used: int
    persona_alignment: float
    latency_ms: int


class MemoryListItem(BaseModel):
    memory_id: str
    content: str
    tags: List[str]
    importance: float
    pinned: bool
    hit_count: int
    updated_at: int


class MemoryListResponse(BaseModel):
    total: int
    items: List[MemoryListItem]


class MemoryDeleteRequest(BaseModel):
    user_id: str
    memory_id: str


class MemoryDeleteResponse(BaseModel):
    deleted: bool


class MemoryPinRequest(BaseModel):
    user_id: str
    memory_id: str
    pinned: bool


class MemoryPinResponse(BaseModel):
    updated: bool


class MemoryExportItem(BaseModel):
    memory_id: str
    user_id: str
    content: str
    tags: List[str]
    importance: float
    pinned: bool
    source: str
    hit_count: int
    created_at: int
    updated_at: int


class MemoryExportResponse(BaseModel):
    total: int
    items: List[MemoryExportItem]


class MemoryImportItem(BaseModel):
    user_id: str
    content: str
    tags: List[str] = []
    importance: float = 0.7
    pinned: bool = False
    source: str = "import"
    hit_count: int = 0
    created_at: Optional[int] = None
    updated_at: Optional[int] = None


class MemoryImportRequest(BaseModel):
    items: List[MemoryImportItem]


class MemoryImportResponse(BaseModel):
    imported: int
    merged: int


app = FastAPI(title="SoulCore QClaw Skill API", version="v1")
_ensure_db()


def _list_user_memories(user_id: str) -> list[sqlite3.Row]:
    with sqlite3.connect(DB_PATH) as conn:
        conn.row_factory = sqlite3.Row
        rows = conn.execute(
            """
            SELECT memory_id, content, content_hash, tags, importance, pinned,
                   source, hit_count, created_at, updated_at
            FROM memories
            WHERE user_id = ?
            ORDER BY pinned DESC, updated_at DESC
            """,
            (user_id,),
        ).fetchall()
    return rows


def _select_memories(user_id: str, query: str, top_k: int) -> list[dict]:
    rows = _list_user_memories(user_id)
    scored = []
    now = int(time.time())
    query_l = (query or "").lower()
    for row in rows:
        base = _score_memory(query, row["content"], float(row["importance"]))
        age_days = max(0.0, (now - max(1, int(row["updated_at"] or row["created_at"] or now))) / 86400.0)
        # 30 天半衰近似：recency 越老越低；pinned 不受衰减
        recency = 1.0 if int(row["pinned"]) == 1 else (0.5 ** (age_days / 30.0))
        usage_boost = min(0.15, int(row["hit_count"]) * 0.01)
        tags = [x for x in row["tags"].split(",") if x]
        tag_boost = 0.0
        if tags and query_l:
            for t in tags:
                if t.lower() in query_l:
                    tag_boost = 0.08
                    break
        phrase_boost = 0.0
        content_l = (row["content"] or "").lower()
        if query_l and (query_l in content_l or content_l[:12] in query_l):
            phrase_boost = 0.06
        score = max(0.0, min(1.0, base * recency + usage_boost + tag_boost + phrase_boost))
        scored.append(
            {
                "memory_id": row["memory_id"],
                "content": row["content"],
                "content_hash": row["content_hash"],
                "score": score,
                "tags": tags,
                "pinned": int(row["pinned"]) == 1,
            }
        )
    scored.sort(key=lambda x: x["score"], reverse=True)
    # 召回去重：同内容 hash 仅保留最高分一条
    dedup = []
    seen = set()
    for s in scored:
        h = s["content_hash"] or _content_hash(s["content"])
        if h in seen:
            continue
        seen.add(h)
        dedup.append(s)
    return dedup[: max(1, min(20, top_k))]


def _build_reply(user_input: str, memories: list[dict], persona_id: str) -> str:
    if not memories:
        return f"我已收到你的输入：{_safe_excerpt(user_input, 60)}。建议先明确目标，再分 2~3 步推进。"
    m = memories[0]["content"]
    return (
        f"基于你之前的记录“{_safe_excerpt(m, 50)}”，"
        f"我建议先处理“{_safe_excerpt(user_input, 40)}”的核心问题，"
        "然后再补充细节和边界条件。"
    )


def _build_llm_messages(user_input: str, persona_id: str, memories: list[dict]) -> list[dict]:
    style_hint = (
        "你是一个中文助手，强调结构化、可执行步骤。"
        "保持人格一致，不要与历史偏好冲突。"
    )
    persona_block = f"persona_id={persona_id or 'default'}"
    memory_lines = []
    for i, m in enumerate(memories[:6], start=1):
        memory_lines.append(f"{i}. {m['content']} (score={m['score']:.2f})")
    memory_block = "\n".join(memory_lines) if memory_lines else "无可用记忆。"
    system_content = (
        f"{style_hint}\n"
        f"{persona_block}\n"
        "以下是可参考记忆，请优先保持一致：\n"
        f"{memory_block}\n"
        "请用中文回答。"
    )
    return [
        {"role": "system", "content": system_content},
        {"role": "user", "content": user_input},
    ]


def _call_llm(messages: list[dict], temperature: float) -> Optional[str]:
    if not LLM.enabled:
        return None
    payload = {
        "model": LLM.model,
        "messages": messages,
        "temperature": max(0.0, min(2.0, temperature)),
        "max_tokens": 380,
    }
    data = json.dumps(payload).encode("utf-8")
    req = urlrequest.Request(
        LLM.base_url,
        data=data,
        method="POST",
        headers={"Content-Type": "application/json"},
    )
    if LLM.api_key:
        req.add_header("Authorization", f"Bearer {LLM.api_key}")
    try:
        with urlrequest.urlopen(req, timeout=max(5, LLM.timeout_seconds)) as resp:
            body = resp.read().decode("utf-8")
            parsed = json.loads(body)
            choices = parsed.get("choices") or []
            if not choices:
                return None
            msg = choices[0].get("message") or {}
            content = (msg.get("content") or "").strip()
            return content if content else None
    except (URLError, HTTPError, TimeoutError, json.JSONDecodeError):
        return None


def _save_diagnostics(
    session_id: str,
    trace_id: str,
    user_input: str,
    reply: str,
    candidates: int,
    used: int,
    alignment: float,
    latency_ms: int,
) -> None:
    now = int(time.time())
    with sqlite3.connect(DB_PATH) as conn:
        conn.execute(
            """
            INSERT INTO diagnostics (
                session_id, trace_id, input_summary, reply_summary,
                memory_candidates, memory_used, persona_alignment, latency_ms, updated_at
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            ON CONFLICT(session_id) DO UPDATE SET
                trace_id=excluded.trace_id,
                input_summary=excluded.input_summary,
                reply_summary=excluded.reply_summary,
                memory_candidates=excluded.memory_candidates,
                memory_used=excluded.memory_used,
                persona_alignment=excluded.persona_alignment,
                latency_ms=excluded.latency_ms,
                updated_at=excluded.updated_at
            """,
            (
                session_id,
                trace_id,
                _safe_excerpt(user_input, 120),
                _safe_excerpt(reply, 120),
                candidates,
                used,
                alignment,
                latency_ms,
                now,
            ),
        )


@app.post("/v1/chat", response_model=ChatResponse)
def chat(req: ChatRequest) -> ChatResponse:
    t0 = time.perf_counter()
    trace_id = hashlib.sha1(
        f"{req.session_id}|{req.user_id}|{uuid.uuid4()}".encode("utf-8")
    ).hexdigest()[:16]

    memories: list[dict] = []
    if req.options.enable_memory:
        memories = _select_memories(req.user_id, req.input, req.memory_top_k)

    llm_messages = _build_llm_messages(req.input, req.persona_id, memories)
    reply = _call_llm(llm_messages, req.options.temperature) or _build_reply(
        req.input, memories, req.persona_id
    )
    align_score, align_flags = _persona_alignment(reply, req.persona_id)

    if req.options.enable_persona_guard and align_score < 0.5:
        reply = _rewrite_with_persona(reply, req.persona_id)
        align_score, align_flags = _persona_alignment(reply, req.persona_id)

    used = [
        UsedMemory(
            memory_id=m["memory_id"],
            score=round(float(m["score"]), 4),
            reason="semantic_recall",
        )
        for m in memories[: min(4, len(memories))]
    ]
    # 命中的记忆增加使用计数与更新时间，帮助后续排序更稳
    if used:
        now = int(time.time())
        with sqlite3.connect(DB_PATH) as conn:
            for item in used:
                conn.execute(
                    """
                    UPDATE memories
                    SET hit_count = hit_count + 1, updated_at = ?
                    WHERE memory_id = ?
                    """,
                    (now, item.memory_id),
                )

    latency_ms = max(1, int((time.perf_counter() - t0) * 1000))
    _save_diagnostics(
        session_id=req.session_id,
        trace_id=trace_id,
        user_input=req.input,
        reply=reply,
        candidates=len(memories),
        used=len(used),
        alignment=float(align_score),
        latency_ms=latency_ms,
    )

    intent = "plan" if any(k in req.input for k in ["计划", "方案", "步骤"]) else "general"
    return ChatResponse(
        reply=reply,
        intent=intent,
        used_memories=used,
        persona_alignment=PersonaAlignment(
            score=round(float(align_score), 4),
            flags=align_flags,
        ),
        trace_id=trace_id,
    )


@app.post("/v1/memory/write", response_model=MemoryWriteResponse)
def memory_write(req: MemoryWriteRequest) -> MemoryWriteResponse:
    if not req.user_id.strip():
        raise HTTPException(status_code=400, detail="user_id is required")
    normalized = _normalize_content(req.content)
    if not normalized:
        raise HTTPException(status_code=400, detail="content is empty after normalization")
    c_hash = _content_hash(normalized)
    now = int(time.time())
    tags_list = [t.strip() for t in req.tags if t.strip()]
    tags = ",".join(tags_list)
    with sqlite3.connect(DB_PATH) as conn:
        conn.row_factory = sqlite3.Row
        existing = conn.execute(
            """
            SELECT memory_id, tags, importance, pinned, hit_count
            FROM memories
            WHERE user_id = ? AND content_hash = ?
            LIMIT 1
            """,
            (req.user_id.strip(), c_hash),
        ).fetchone()
        if existing is not None:
            merged_tags = set([x for x in (existing["tags"] or "").split(",") if x])
            merged_tags.update(tags_list)
            merged_tags_str = ",".join(sorted(merged_tags))
            new_importance = max(float(existing["importance"]), max(0.0, min(1.0, req.importance)))
            new_pinned = 1 if (int(existing["pinned"]) == 1 or req.pinned) else 0
            conn.execute(
                """
                UPDATE memories
                SET content = ?, tags = ?, importance = ?, pinned = ?,
                    source = ?, updated_at = ?
                WHERE memory_id = ?
                """,
                (
                    normalized,
                    merged_tags_str,
                    new_importance,
                    new_pinned,
                    req.source.strip() or "chat",
                    now,
                    existing["memory_id"],
                ),
            )
            return MemoryWriteResponse(memory_id=existing["memory_id"], stored=True)
        memory_id = "mem_" + uuid.uuid4().hex[:16]
        conn.execute(
            """
            INSERT INTO memories (
                memory_id, user_id, content, content_hash, tags, importance,
                pinned, source, hit_count, created_at, updated_at
            )
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """,
            (
                memory_id,
                req.user_id.strip(),
                normalized,
                c_hash,
                tags,
                max(0.0, min(1.0, req.importance)),
                1 if req.pinned else 0,
                req.source.strip() or "chat",
                0,
                now,
                now,
            ),
        )
    return MemoryWriteResponse(memory_id=memory_id, stored=True)


@app.post("/v1/memory/recall", response_model=RecallResponse)
def memory_recall(req: RecallRequest) -> RecallResponse:
    hits = _select_memories(req.user_id, req.query, req.top_k)
    return RecallResponse(
        hits=[
            RecallHit(
                memory_id=h["memory_id"],
                content=h["content"],
                score=round(float(h["score"]), 4),
                tags=h["tags"],
            )
            for h in hits
        ]
    )


@app.get("/v1/memory/list", response_model=MemoryListResponse)
def memory_list(
    user_id: str = Query(..., min_length=1),
    limit: int = Query(20, ge=1, le=200),
    offset: int = Query(0, ge=0),
) -> MemoryListResponse:
    with sqlite3.connect(DB_PATH) as conn:
        conn.row_factory = sqlite3.Row
        total = conn.execute(
            "SELECT COUNT(1) AS c FROM memories WHERE user_id = ?",
            (user_id,),
        ).fetchone()["c"]
        rows = conn.execute(
            """
            SELECT memory_id, content, tags, importance, pinned, hit_count, updated_at
            FROM memories
            WHERE user_id = ?
            ORDER BY pinned DESC, updated_at DESC
            LIMIT ? OFFSET ?
            """,
            (user_id, limit, offset),
        ).fetchall()
    return MemoryListResponse(
        total=int(total),
        items=[
            MemoryListItem(
                memory_id=r["memory_id"],
                content=r["content"],
                tags=[x for x in (r["tags"] or "").split(",") if x],
                importance=float(r["importance"]),
                pinned=int(r["pinned"]) == 1,
                hit_count=int(r["hit_count"]),
                updated_at=int(r["updated_at"]),
            )
            for r in rows
        ],
    )


@app.post("/v1/memory/delete", response_model=MemoryDeleteResponse)
def memory_delete(req: MemoryDeleteRequest) -> MemoryDeleteResponse:
    with sqlite3.connect(DB_PATH) as conn:
        cur = conn.execute(
            "DELETE FROM memories WHERE user_id = ? AND memory_id = ?",
            (req.user_id.strip(), req.memory_id.strip()),
        )
        deleted = cur.rowcount > 0
    return MemoryDeleteResponse(deleted=deleted)


@app.post("/v1/memory/pin", response_model=MemoryPinResponse)
def memory_pin(req: MemoryPinRequest) -> MemoryPinResponse:
    with sqlite3.connect(DB_PATH) as conn:
        cur = conn.execute(
            """
            UPDATE memories
            SET pinned = ?, updated_at = ?
            WHERE user_id = ? AND memory_id = ?
            """,
            (1 if req.pinned else 0, int(time.time()), req.user_id.strip(), req.memory_id.strip()),
        )
        updated = cur.rowcount > 0
    return MemoryPinResponse(updated=updated)


@app.get("/v1/health")
def health() -> dict:
    return {
        "ok": True,
        "service": "soulcore-qclaw-skill",
        "version": "v1",
        "llm_enabled": LLM.enabled,
        "db_path": DB_PATH,
        "ts": int(time.time()),
    }


@app.get("/v1/memory/export", response_model=MemoryExportResponse)
def memory_export(user_id: str = Query(..., min_length=1)) -> MemoryExportResponse:
    with sqlite3.connect(DB_PATH) as conn:
        conn.row_factory = sqlite3.Row
        rows = conn.execute(
            """
            SELECT memory_id, user_id, content, tags, importance, pinned, source,
                   hit_count, created_at, updated_at
            FROM memories
            WHERE user_id = ?
            ORDER BY pinned DESC, updated_at DESC
            """,
            (user_id,),
        ).fetchall()
    items = [
        MemoryExportItem(
            memory_id=r["memory_id"],
            user_id=r["user_id"],
            content=r["content"],
            tags=[x for x in (r["tags"] or "").split(",") if x],
            importance=float(r["importance"]),
            pinned=int(r["pinned"]) == 1,
            source=r["source"] or "import",
            hit_count=int(r["hit_count"]),
            created_at=int(r["created_at"]),
            updated_at=int(r["updated_at"]),
        )
        for r in rows
    ]
    return MemoryExportResponse(total=len(items), items=items)


@app.post("/v1/memory/import", response_model=MemoryImportResponse)
def memory_import(req: MemoryImportRequest) -> MemoryImportResponse:
    imported = 0
    merged = 0
    now = int(time.time())
    with sqlite3.connect(DB_PATH) as conn:
        conn.row_factory = sqlite3.Row
        for item in req.items:
            user_id = (item.user_id or "").strip()
            content = _normalize_content(item.content)
            if not user_id or not content:
                continue
            tags = [t.strip() for t in (item.tags or []) if t.strip()]
            tags_str = ",".join(sorted(set(tags)))
            c_hash = _content_hash(content)
            importance = max(0.0, min(1.0, item.importance))
            pinned = 1 if item.pinned else 0
            source = (item.source or "import").strip() or "import"
            hit_count = max(0, int(item.hit_count))
            created_at = int(item.created_at or now)
            updated_at = int(item.updated_at or max(created_at, now))
            existing = conn.execute(
                """
                SELECT memory_id, tags, importance, pinned, hit_count, created_at, updated_at
                FROM memories
                WHERE user_id = ? AND content_hash = ?
                LIMIT 1
                """,
                (user_id, c_hash),
            ).fetchone()
            if existing is not None:
                merged += 1
                merged_tags = set([x for x in (existing["tags"] or "").split(",") if x])
                merged_tags.update(tags)
                conn.execute(
                    """
                    UPDATE memories
                    SET tags = ?, importance = ?, pinned = ?, source = ?, hit_count = ?,
                        created_at = ?, updated_at = ?
                    WHERE memory_id = ?
                    """,
                    (
                        ",".join(sorted(merged_tags)),
                        max(float(existing["importance"]), importance),
                        max(int(existing["pinned"]), pinned),
                        source,
                        max(int(existing["hit_count"]), hit_count),
                        min(int(existing["created_at"]), created_at),
                        max(int(existing["updated_at"]), updated_at),
                        existing["memory_id"],
                    ),
                )
                continue
            memory_id = "mem_" + uuid.uuid4().hex[:16]
            conn.execute(
                """
                INSERT INTO memories (
                    memory_id, user_id, content, content_hash, tags, importance,
                    pinned, source, hit_count, created_at, updated_at
                )
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    memory_id,
                    user_id,
                    content,
                    c_hash,
                    tags_str,
                    importance,
                    pinned,
                    source,
                    hit_count,
                    created_at,
                    updated_at,
                ),
            )
            imported += 1
    return MemoryImportResponse(imported=imported, merged=merged)


@app.get("/v1/diagnostics/last", response_model=DiagnosticsResponse)
def diagnostics_last(session_id: str = Query(..., min_length=1)) -> DiagnosticsResponse:
    with sqlite3.connect(DB_PATH) as conn:
        conn.row_factory = sqlite3.Row
        row = conn.execute(
            """
            SELECT trace_id, input_summary, reply_summary, memory_candidates,
                   memory_used, persona_alignment, latency_ms
            FROM diagnostics
            WHERE session_id = ?
            """,
            (session_id,),
        ).fetchone()
    if row is None:
        raise HTTPException(status_code=404, detail="No diagnostics found for session_id")
    return DiagnosticsResponse(
        trace_id=row["trace_id"],
        input_summary=row["input_summary"],
        reply_summary=row["reply_summary"],
        memory_candidates=int(row["memory_candidates"]),
        memory_used=int(row["memory_used"]),
        persona_alignment=float(row["persona_alignment"]),
        latency_ms=int(row["latency_ms"]),
    )


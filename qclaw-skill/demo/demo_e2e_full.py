import json
import os
from urllib import request


BASE = "http://127.0.0.1:8000"
SRC_USER = "demo-user-e2e-src"
DST_USER = "demo-user-e2e-dst"
SESSION_ID = "demo-session-e2e-001"
API_KEY = os.getenv("SOULCORE_API_KEY", "").strip()


def _headers() -> dict:
    headers = {"Content-Type": "application/json"}
    if API_KEY:
        headers["x-api-key"] = API_KEY
    return headers


def post(path: str, payload: dict) -> dict:
    req = request.Request(
        BASE + path,
        method="POST",
        data=json.dumps(payload).encode("utf-8"),
        headers=_headers(),
    )
    with request.urlopen(req, timeout=10) as resp:
        return json.loads(resp.read().decode("utf-8"))


def get(path: str) -> dict:
    req = request.Request(BASE + path, method="GET", headers=_headers())
    with request.urlopen(req, timeout=10) as resp:
        return json.loads(resp.read().decode("utf-8"))


def pretty(title: str, data: dict) -> None:
    print(f"\n=== {title} ===")
    print(json.dumps(data, ensure_ascii=False, indent=2))


def main() -> None:
    pretty("1) 健康检查", get("/v1/health"))

    w1 = post(
        "/v1/memory/write",
        {
            "user_id": SRC_USER,
            "content": "用户偏好先给结论，再给步骤。",
            "tags": ["workflow", "style"],
            "importance": 0.9,
            "source": "demo",
            "pinned": True,
        },
    )
    w2 = post(
        "/v1/memory/write",
        {
            "user_id": SRC_USER,
            "content": "用户要中文简洁回复，必要时补充下一步。",
            "tags": ["language", "tone"],
            "importance": 0.85,
            "source": "demo",
            "pinned": False,
        },
    )
    pretty("2) 写入源用户记忆", {"w1": w1, "w2": w2})

    recall = post(
        "/v1/memory/recall",
        {
            "user_id": SRC_USER,
            "query": "请先总结再给执行步骤",
            "top_k": 5,
        },
    )
    pretty("3) 源用户召回", recall)

    chat = post(
        "/v1/chat",
        {
            "session_id": SESSION_ID,
            "user_id": SRC_USER,
            "input": "帮我给今天开发安排一个最小可执行计划",
            "persona_id": "formal_assistant",
            "memory_top_k": 5,
            "options": {
                "enable_memory": True,
                "enable_persona_guard": True,
                "temperature": 0.5,
            },
        },
    )
    pretty("4) 对话结果", chat)

    diag = get(f"/v1/diagnostics/last?session_id={SESSION_ID}")
    pretty("5) 最后诊断", diag)

    exported = get(f"/v1/memory/export?user_id={SRC_USER}")
    pretty("6) 导出源用户记忆", exported)

    import_items = []
    for item in exported.get("items", []):
        import_items.append(
            {
                "user_id": DST_USER,
                "content": item["content"],
                "tags": item.get("tags", []),
                "importance": item.get("importance", 0.7),
                "pinned": item.get("pinned", False),
                "source": "import",
                "hit_count": item.get("hit_count", 0),
                "created_at": item.get("created_at"),
                "updated_at": item.get("updated_at"),
            }
        )
    imported = post("/v1/memory/import", {"items": import_items})
    pretty("7) 导入到目标用户", imported)

    dst_list = get(f"/v1/memory/list?user_id={DST_USER}&limit=20&offset=0")
    pretty("8) 目标用户列表", dst_list)


if __name__ == "__main__":
    main()

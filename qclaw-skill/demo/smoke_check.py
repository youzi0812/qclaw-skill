import json
import os
import sys
from urllib import error, request


BASE = "http://127.0.0.1:8000"
USER_ID = "smoke-user"
SESSION_ID = "smoke-session-001"
API_KEY = os.getenv("SOULCORE_API_KEY", "").strip()


def _headers() -> dict:
    headers = {"Content-Type": "application/json"}
    if API_KEY:
        headers["x-api-key"] = API_KEY
    return headers


def _post(path: str, payload: dict) -> dict:
    req = request.Request(
        BASE + path,
        method="POST",
        data=json.dumps(payload).encode("utf-8"),
        headers=_headers(),
    )
    with request.urlopen(req, timeout=10) as resp:
        return json.loads(resp.read().decode("utf-8"))


def _get(path: str) -> dict:
    req = request.Request(BASE + path, method="GET", headers=_headers())
    with request.urlopen(req, timeout=10) as resp:
        return json.loads(resp.read().decode("utf-8"))


def _expect(condition: bool, msg: str) -> None:
    if not condition:
        raise AssertionError(msg)


def main() -> int:
    try:
        health = _get("/v1/health")
        _expect(bool(health.get("ok")), "health.ok 应为 true")

        write_res = _post(
            "/v1/memory/write",
            {
                "user_id": USER_ID,
                "content": "smoke check memory content",
                "tags": ["smoke", "test"],
                "importance": 0.8,
                "source": "smoke",
                "pinned": False,
            },
        )
        _expect(bool(write_res.get("stored")), "memory.write 未返回 stored=true")
        memory_id = write_res.get("memory_id")
        _expect(bool(memory_id), "memory.write 未返回 memory_id")

        recall = _post(
            "/v1/memory/recall",
            {"user_id": USER_ID, "query": "memory content", "top_k": 5},
        )
        hits = recall.get("hits") or []
        _expect(len(hits) > 0, "memory.recall 未召回任何结果")

        chat = _post(
            "/v1/chat",
            {
                "session_id": SESSION_ID,
                "user_id": USER_ID,
                "input": "给我一个两步执行方案",
                "persona_id": "formal_assistant",
                "memory_top_k": 5,
                "options": {
                    "enable_memory": True,
                    "enable_persona_guard": True,
                    "temperature": 0.6,
                },
            },
        )
        _expect(bool(chat.get("reply")), "chat.reply 为空")
        _expect(bool(chat.get("trace_id")), "chat.trace_id 为空")

        diag = _get(f"/v1/diagnostics/last?session_id={SESSION_ID}")
        _expect(bool(diag.get("trace_id")), "diagnostics.last trace_id 为空")

        mem_list = _get(f"/v1/memory/list?user_id={USER_ID}&limit=20&offset=0")
        _expect(int(mem_list.get("total", 0)) >= 1, "memory.list total 应 >= 1")

        exported = _get(f"/v1/memory/export?user_id={USER_ID}")
        _expect(int(exported.get("total", 0)) >= 1, "memory.export total 应 >= 1")

        imported = _post(
            "/v1/memory/import",
            {
                "items": [
                    {
                        "user_id": f"{USER_ID}-imported",
                        "content": "smoke imported content",
                        "tags": ["smoke", "import"],
                        "importance": 0.75,
                        "pinned": False,
                        "source": "import",
                    }
                ]
            },
        )
        _expect(
            int(imported.get("imported", 0)) + int(imported.get("merged", 0)) >= 1,
            "memory.import 未导入或合并任何记录",
        )

        print("SMOKE CHECK PASSED")
        return 0
    except (AssertionError, error.URLError, error.HTTPError, json.JSONDecodeError) as ex:
        print(f"SMOKE CHECK FAILED: {ex}")
        return 1


if __name__ == "__main__":
    raise SystemExit(main())

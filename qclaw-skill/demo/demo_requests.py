import json
from urllib import request


BASE = "http://127.0.0.1:8000"


def post(path: str, payload: dict) -> dict:
    req = request.Request(
        BASE + path,
        method="POST",
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json"},
    )
    with request.urlopen(req, timeout=10) as resp:
        return json.loads(resp.read().decode("utf-8"))


def get(path: str) -> dict:
    with request.urlopen(BASE + path, timeout=10) as resp:
        return json.loads(resp.read().decode("utf-8"))


def main() -> None:
    user_id = "demo-user"
    session_id = "demo-session-001"

    print("1) 写入两条记忆")
    w1 = post(
        "/v1/memory/write",
        {
            "user_id": user_id,
            "content": "用户偏好正式语气，邮件结尾使用此致敬礼。",
            "tags": ["style", "mail"],
            "importance": 0.9,
            "source": "import",
        },
    )
    print(" ->", w1)
    w2 = post(
        "/v1/memory/write",
        {
            "user_id": user_id,
            "content": "用户在项目里优先要求先给可执行步骤，再给扩展建议。",
            "tags": ["workflow", "preference"],
            "importance": 0.85,
            "source": "import",
        },
    )
    print(" ->", w2)

    print("\n2) 召回记忆")
    recall = post(
        "/v1/memory/recall",
        {
            "user_id": user_id,
            "query": "写邮件时语气和结尾格式",
            "top_k": 3,
        },
    )
    print(json.dumps(recall, ensure_ascii=False, indent=2))

    print("\n3) 进行对话")
    chat = post(
        "/v1/chat",
        {
            "session_id": session_id,
            "user_id": user_id,
            "input": "请帮我写一段发给合作方的更新邮件",
            "persona_id": "formal_assistant",
            "memory_top_k": 5,
            "options": {
                "enable_memory": True,
                "enable_persona_guard": True,
                "temperature": 0.6,
            },
        },
    )
    print(json.dumps(chat, ensure_ascii=False, indent=2))

    print("\n4) 查看最后诊断")
    diag = get(f"/v1/diagnostics/last?session_id={session_id}")
    print(json.dumps(diag, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()


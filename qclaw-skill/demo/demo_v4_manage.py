import json
from urllib import request


BASE = "http://127.0.0.1:8000"
USER_ID = "demo-user"


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


def print_list(title: str) -> dict:
    print(f"\n=== {title} ===")
    data = get(f"/v1/memory/list?user_id={USER_ID}&limit=20&offset=0")
    print(json.dumps(data, ensure_ascii=False, indent=2))
    return data


def main() -> None:
    # 确保至少有一条可操作记忆
    seeded = post(
        "/v1/memory/write",
        {
            "user_id": USER_ID,
            "content": "这是用于 v4 管理接口演示的测试记忆。",
            "tags": ["demo", "manage"],
            "importance": 0.8,
            "source": "demo",
            "pinned": False,
        },
    )
    print("seeded:", seeded)

    before = print_list("1) 初始列表")
    if not before.get("items"):
        print("没有可操作记忆，结束。")
        return

    target_id = before["items"][0]["memory_id"]
    print(f"\n选择目标记忆: {target_id}")

    pin_res = post(
        "/v1/memory/pin",
        {"user_id": USER_ID, "memory_id": target_id, "pinned": True},
    )
    print("\n2) 置顶结果:")
    print(json.dumps(pin_res, ensure_ascii=False, indent=2))

    after_pin = print_list("3) 置顶后列表")

    # 删除最后一条（尽量不删刚置顶的第一条）
    delete_target = None
    if after_pin.get("items"):
        delete_target = after_pin["items"][-1]["memory_id"]
    if delete_target:
        del_res = post(
            "/v1/memory/delete",
            {"user_id": USER_ID, "memory_id": delete_target},
        )
        print(f"\n4) 删除目标 {delete_target} 结果:")
        print(json.dumps(del_res, ensure_ascii=False, indent=2))
    else:
        print("\n4) 没有可删除目标。")

    print_list("5) 删除后列表")


if __name__ == "__main__":
    main()


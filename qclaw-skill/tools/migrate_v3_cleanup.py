import os
import sqlite3
import time


BASE_DIR = os.path.dirname(os.path.dirname(__file__))
DB_PATH = os.path.join(BASE_DIR, "data", "memory.db")


def _ensure_db_exists() -> None:
    if not os.path.exists(DB_PATH):
        raise FileNotFoundError(f"数据库不存在: {DB_PATH}")


def _fill_missing_timestamps(conn: sqlite3.Connection) -> int:
    now = int(time.time())
    cur = conn.execute(
        """
        UPDATE memories
        SET updated_at = CASE
            WHEN created_at > 0 THEN created_at
            ELSE ?
        END
        WHERE updated_at IS NULL OR updated_at <= 0
        """,
        (now,),
    )
    return cur.rowcount


def _merge_duplicates(conn: sqlite3.Connection) -> tuple[int, int]:
    conn.row_factory = sqlite3.Row
    rows = conn.execute(
        """
        SELECT memory_id, user_id, content_hash, content, tags, importance, pinned,
               source, hit_count, created_at, updated_at
        FROM memories
        ORDER BY user_id, content_hash, pinned DESC, importance DESC, hit_count DESC, updated_at DESC
        """
    ).fetchall()

    groups: dict[tuple[str, str], list[sqlite3.Row]] = {}
    for r in rows:
        key = (r["user_id"], r["content_hash"] or "")
        groups.setdefault(key, []).append(r)

    merged_groups = 0
    deleted_rows = 0

    for key, items in groups.items():
        if len(items) <= 1:
            continue
        merged_groups += 1
        keeper = items[0]
        extras = items[1:]

        merged_tags = set()
        max_importance = float(keeper["importance"])
        max_pinned = int(keeper["pinned"])
        total_hit = int(keeper["hit_count"])
        min_created_at = int(keeper["created_at"])
        max_updated_at = int(keeper["updated_at"])
        source = keeper["source"] or "chat"
        content = keeper["content"]

        for item in items:
            tags = [t for t in (item["tags"] or "").split(",") if t]
            merged_tags.update(tags)
            max_importance = max(max_importance, float(item["importance"]))
            max_pinned = max(max_pinned, int(item["pinned"]))
            total_hit += 0 if item["memory_id"] == keeper["memory_id"] else int(item["hit_count"])
            min_created_at = min(min_created_at, int(item["created_at"]))
            max_updated_at = max(max_updated_at, int(item["updated_at"]))

        conn.execute(
            """
            UPDATE memories
            SET content = ?, tags = ?, importance = ?, pinned = ?, source = ?,
                hit_count = ?, created_at = ?, updated_at = ?
            WHERE memory_id = ?
            """,
            (
                content,
                ",".join(sorted(merged_tags)),
                max_importance,
                max_pinned,
                source,
                total_hit,
                min_created_at,
                max_updated_at,
                keeper["memory_id"],
            ),
        )

        for item in extras:
            conn.execute("DELETE FROM memories WHERE memory_id = ?", (item["memory_id"],))
            deleted_rows += 1

    return merged_groups, deleted_rows


def main() -> None:
    _ensure_db_exists()
    with sqlite3.connect(DB_PATH) as conn:
        fixed_ts = _fill_missing_timestamps(conn)
        merged_groups, deleted_rows = _merge_duplicates(conn)
        conn.commit()

        total = conn.execute("SELECT COUNT(1) FROM memories").fetchone()[0]

    print("清洗完成:")
    print(f"- 修复 updated_at<=0 条数: {fixed_ts}")
    print(f"- 合并重复分组数: {merged_groups}")
    print(f"- 删除重复记录条数: {deleted_rows}")
    print(f"- 当前记忆总数: {total}")


if __name__ == "__main__":
    main()


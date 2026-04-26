import os
import sqlite3
import unittest

import server.app as appmod
from server.app import (
    ChatRequest,
    MemoryWriteRequest,
    RecallRequest,
    chat,
    diagnostics_last,
    memory_recall,
    memory_write,
)


class QClawSkillCoreTests(unittest.TestCase):
    def setUp(self) -> None:
        appmod.DB_PATH = os.path.join(
            os.path.dirname(__file__),
            "_tmp_memory_test.db",
        )
        appmod._ensure_db()
        with sqlite3.connect(appmod.DB_PATH) as conn:
            conn.execute("DELETE FROM diagnostics")
            conn.execute("DELETE FROM memories")

    def tearDown(self) -> None:
        pass

    def test_memory_write_merges_same_content_hash(self) -> None:
        first = memory_write(
            MemoryWriteRequest(
                user_id="u1",
                content="用户偏好先给步骤",
                tags=["workflow"],
                importance=0.6,
                source="chat",
                pinned=False,
            )
        )
        second = memory_write(
            MemoryWriteRequest(
                user_id="u1",
                content="  用户偏好先给步骤  ",
                tags=["style"],
                importance=0.9,
                source="import",
                pinned=True,
            )
        )
        self.assertEqual(first.memory_id, second.memory_id)

        listed = appmod.memory_list(user_id="u1", limit=20, offset=0)
        self.assertEqual(1, listed.total)
        item = listed.items[0]
        self.assertTrue(item.pinned)
        self.assertAlmostEqual(0.9, item.importance, places=4)
        self.assertCountEqual(["style", "workflow"], item.tags)

    def test_memory_recall_prefers_pinned_and_dedupes(self) -> None:
        memory_write(
            MemoryWriteRequest(
                user_id="u2",
                content="请先给我执行步骤再解释细节",
                tags=["workflow"],
                importance=0.9,
                pinned=False,
            )
        )
        memory_write(
            MemoryWriteRequest(
                user_id="u2",
                content="请先给我执行步骤再解释细节",
                tags=["workflow", "dup"],
                importance=0.4,
                pinned=True,
            )
        )
        memory_write(
            MemoryWriteRequest(
                user_id="u2",
                content="今天心情不错，先闲聊一下",
                tags=["misc"],
                importance=0.2,
                pinned=False,
            )
        )

        recalled = memory_recall(
            RecallRequest(
                user_id="u2",
                query="给我一个执行步骤",
                top_k=10,
            )
        )
        self.assertGreaterEqual(len(recalled.hits), 1)
        # 同内容应被去重，只保留一条
        same_content_hits = [
            h for h in recalled.hits if "执行步骤" in h.content
        ]
        self.assertEqual(1, len(same_content_hits))
        # 相关记忆应排在前面
        self.assertIn("执行步骤", recalled.hits[0].content)

    def test_chat_writes_diagnostics_with_trace(self) -> None:
        memory_write(
            MemoryWriteRequest(
                user_id="u3",
                content="用户希望先结论后步骤",
                tags=["preference"],
                importance=0.8,
                pinned=True,
            )
        )
        resp = chat(
            ChatRequest(
                session_id="s3",
                user_id="u3",
                input="给我一个今天开发计划",
                persona_id="formal_assistant",
                memory_top_k=5,
            )
        )
        self.assertTrue(resp.trace_id)
        self.assertTrue(resp.reply)
        self.assertGreaterEqual(len(resp.used_memories), 1)

        diag = diagnostics_last(session_id="s3")
        self.assertEqual(resp.trace_id, diag.trace_id)
        self.assertGreaterEqual(diag.memory_candidates, 1)
        self.assertGreaterEqual(diag.memory_used, 1)
        self.assertGreater(diag.latency_ms, 0)


if __name__ == "__main__":
    unittest.main()

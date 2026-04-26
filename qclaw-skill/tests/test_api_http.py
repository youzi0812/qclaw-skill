import os
import sqlite3
import unittest

from fastapi.testclient import TestClient

import server.app as appmod


class QClawSkillApiHttpTests(unittest.TestCase):
    def setUp(self) -> None:
        self._old_db_path = appmod.DB_PATH
        self._old_api_key = appmod.API_KEY
        appmod.DB_PATH = os.path.join(
            os.path.dirname(__file__),
            "_tmp_api_http_test.db",
        )
        appmod.API_KEY = ""
        appmod._ensure_db()
        with sqlite3.connect(appmod.DB_PATH) as conn:
            conn.execute("DELETE FROM diagnostics")
            conn.execute("DELETE FROM memories")
        self.client = TestClient(appmod.app)

    def tearDown(self) -> None:
        self.client.close()
        appmod.DB_PATH = self._old_db_path
        appmod.API_KEY = self._old_api_key

    def test_validation_error_shape(self) -> None:
        # 缺少必须字段 input，应命中统一 VALIDATION_ERROR 结构
        resp = self.client.post(
            "/v1/chat",
            json={
                "session_id": "s-http-1",
                "user_id": "u-http-1",
                "persona_id": "default",
            },
        )
        self.assertEqual(422, resp.status_code)
        body = resp.json()
        self.assertEqual(False, body.get("ok"))
        self.assertEqual("VALIDATION_ERROR", body.get("error", {}).get("code"))
        self.assertIn("issues", body.get("error", {}).get("details", {}))

    def test_auth_missing_key_returns_401(self) -> None:
        appmod.API_KEY = "demo-key"
        resp = self.client.get("/v1/memory/list?user_id=u-http-2")
        self.assertEqual(401, resp.status_code)
        body = resp.json()
        self.assertEqual(False, body.get("ok"))
        self.assertEqual("AUTH_MISSING_API_KEY", body.get("error", {}).get("code"))

    def test_auth_invalid_key_returns_401(self) -> None:
        appmod.API_KEY = "demo-key"
        resp = self.client.get(
            "/v1/memory/list?user_id=u-http-3",
            headers={"x-api-key": "wrong-key"},
        )
        self.assertEqual(401, resp.status_code)
        body = resp.json()
        self.assertEqual(False, body.get("ok"))
        self.assertEqual("AUTH_INVALID_API_KEY", body.get("error", {}).get("code"))

    def test_auth_valid_key_allows_request(self) -> None:
        appmod.API_KEY = "demo-key"
        resp = self.client.get(
            "/v1/memory/list?user_id=u-http-4&limit=20&offset=0",
            headers={"x-api-key": "demo-key"},
        )
        self.assertEqual(200, resp.status_code)
        body = resp.json()
        self.assertIn("total", body)
        self.assertIn("items", body)

    def test_chat_and_diagnostics_flow(self) -> None:
        # 准备一条记忆，确保 chat 命中且写入诊断
        write_resp = self.client.post(
            "/v1/memory/write",
            json={
                "user_id": "u-http-5",
                "content": "用户希望先给结论再给步骤",
                "tags": ["preference"],
                "importance": 0.8,
                "source": "test",
                "pinned": True,
            },
        )
        self.assertEqual(200, write_resp.status_code)
        self.assertTrue(write_resp.json().get("stored"))

        chat_resp = self.client.post(
            "/v1/chat",
            json={
                "session_id": "s-http-5",
                "user_id": "u-http-5",
                "input": "给我今天开发安排",
                "persona_id": "formal_assistant",
                "memory_top_k": 5,
            },
        )
        self.assertEqual(200, chat_resp.status_code)
        chat_body = chat_resp.json()
        self.assertTrue(chat_body.get("trace_id"))
        self.assertTrue(chat_body.get("reply"))

        diag_resp = self.client.get("/v1/diagnostics/last?session_id=s-http-5")
        self.assertEqual(200, diag_resp.status_code)
        diag = diag_resp.json()
        self.assertEqual(chat_body.get("trace_id"), diag.get("trace_id"))
        self.assertGreaterEqual(int(diag.get("memory_candidates", 0)), 1)


if __name__ == "__main__":
    unittest.main()

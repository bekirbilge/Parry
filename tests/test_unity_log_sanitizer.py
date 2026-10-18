import importlib.util
import tempfile
import unittest
from pathlib import Path


HELPER_PATH = Path(__file__).resolve().parents[1] / "tools" / "sanitize-unity-log.py"
SPEC = importlib.util.spec_from_file_location("sanitize_unity_log", HELPER_PATH)
SANITIZER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(SANITIZER)


class UnityLogSanitizerTests(unittest.TestCase):
    def test_redacts_multiline_access_token_flag(self):
        source = "Unity command:\n-accessToken\nsynthetic-multiline-token\n--projectPath /tmp/game\n"

        cleaned = SANITIZER.redact(source)

        self.assertEqual(
            cleaned,
            "Unity command:\n-accessToken\n[REDACTED]\n--projectPath /tmp/game\n",
        )
        self.assertNotIn("synthetic-multiline-token", cleaned)

    def test_redacts_inline_flags_and_json_credentials(self):
        source = (
            "--accessToken=synthetic-inline-token "
            '{"refreshToken":"synthetic-refresh-token", '
            '"client_secret": "synthetic-client-secret"}\n'
        )

        cleaned = SANITIZER.redact(source)

        self.assertIn("--accessToken=[REDACTED]", cleaned)
        self.assertIn('"refreshToken":"[REDACTED]"', cleaned)
        self.assertIn('"client_secret": "[REDACTED]"', cleaned)
        for synthetic_secret in (
            "synthetic-inline-token",
            "synthetic-refresh-token",
            "synthetic-client-secret",
        ):
            self.assertNotIn(synthetic_secret, cleaned)

    def test_redacts_authorization_headers(self):
        source = (
            "Authorization: Bearer synthetic-bearer-token\n"
            "authorization: Basic synthetic-basic-token\n"
        )

        cleaned = SANITIZER.redact(source)

        self.assertEqual(
            cleaned,
            "Authorization: Bearer [REDACTED]\n"
            "authorization: Basic [REDACTED]\n",
        )

    def test_preserves_ordinary_success_and_error_text(self):
        source = (
            "Build succeeded.\n"
            "error CS1234: synthetic compile failure in PlayerController.cs\n"
        )

        self.assertEqual(SANITIZER.redact(source), source)

    def test_in_place_sanitize_is_idempotent(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            log_path = Path(temporary_directory) / "unity-validation.log"
            log_path.write_text(
                "--accessToken synthetic-in-place-token\nBuild succeeded.\n",
                encoding="utf-8",
            )

            SANITIZER.sanitize(log_path)
            once = log_path.read_text(encoding="utf-8")
            SANITIZER.sanitize(log_path)
            twice = log_path.read_text(encoding="utf-8")

            self.assertEqual(once, twice)
            self.assertIn("[REDACTED]", once)
            self.assertNotIn("synthetic-in-place-token", once)
            self.assertIn("Build succeeded.", once)


if __name__ == "__main__":
    unittest.main()

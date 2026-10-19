#!/usr/bin/env python3
"""Redact credentials from known Unity editor, test, build and player logs in place."""

import argparse
import os
import re
import tempfile
from pathlib import Path


LOG_NAMES = (
    "unity-validation.log",
    "unity-validation.cli.log",
    "unity-build.log",
    "unity-build.cli.log",
    "unity-test.log",
    "unity-test.cli.log",
    "runtime.stdout.log",
    "runtime.stderr.log",
)
SECRET_NAME = (
    r"access[-_]?token|refresh[-_]?token|id[-_]?token|auth[-_]?token|"
    r"client[-_]?secret|api[-_]?key|password|passwd|"
    r"android[-_]?keystore[-_]?base64|android[-_]?keystore[-_]?password|"
    r"android[-_]?key[-_]?alias[-_]?password"
)
FLAG_ALONE = re.compile(rf"^\s*-{{1,2}}(?:{SECRET_NAME})\s*$", re.IGNORECASE)
FLAG_VALUE = re.compile(
    rf"(?P<prefix>(?<![\w-])-{{1,2}}(?:{SECRET_NAME})(?:\s*[:=]\s*|\s+))"
    r"(?P<value>\"[^\"]*\"|'[^']*'|[^\s,]+)",
    re.IGNORECASE,
)
JSON_VALUE = re.compile(
    rf"(?P<prefix>[\"'](?:{SECRET_NAME})[\"']\s*:\s*)"
    r"(?P<value>\"(?:\\.|[^\"\\])*\"|'(?:\\.|[^'\\])*'|[^\s,}}]+)",
    re.IGNORECASE,
)
KEY_VALUE = re.compile(
    rf"(?P<prefix>(?<![\w-])(?:{SECRET_NAME})\s*[:=]\s*)"
    r"(?P<value>[^\s,&]+)",
    re.IGNORECASE,
)
AUTH_HEADER = re.compile(r"(?P<prefix>\bAuthorization\s*:\s*(?:Bearer|Basic)\s+)\S+", re.IGNORECASE)
BEARER = re.compile(r"(?P<prefix>\bBearer\s+)\S+", re.IGNORECASE)


def redact_line(line: str) -> str:
    line = FLAG_VALUE.sub(lambda match: match.group("prefix") + "[REDACTED]", line)
    line = JSON_VALUE.sub(lambda match: match.group("prefix") + '"[REDACTED]"', line)
    line = KEY_VALUE.sub(lambda match: match.group("prefix") + "[REDACTED]", line)
    line = AUTH_HEADER.sub(lambda match: match.group("prefix") + "[REDACTED]", line)
    return BEARER.sub(lambda match: match.group("prefix") + "[REDACTED]", line)


def redact(content: str) -> str:
    lines = content.splitlines(keepends=True)
    result = []
    redact_next = False
    for line in lines:
        if redact_next:
            ending = "\r\n" if line.endswith("\r\n") else "\n" if line.endswith("\n") else ""
            result.append("[REDACTED]" + ending)
            redact_next = False
            continue
        redact_next = bool(FLAG_ALONE.match(line.rstrip("\r\n")))
        result.append(redact_line(line))
    return "".join(result)


def sanitize(path: Path) -> None:
    if not path.is_file():
        return
    original = path.read_text(encoding="utf-8", errors="replace")
    clean = redact(original)
    if clean == original:
        return
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(mode="w", encoding="utf-8", dir=path.parent, delete=False) as handle:
            temporary = Path(handle.name)
            handle.write(clean)
        os.chmod(temporary, 0o600)
        os.replace(temporary, path)
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("logs_dir", type=Path)
    args = parser.parse_args()
    for name in LOG_NAMES:
        sanitize(args.logs_dir / name)


if __name__ == "__main__":
    main()

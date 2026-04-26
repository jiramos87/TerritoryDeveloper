"""Entry point for `python -m src` / `python -m sprite_gen`.

Dispatches the first argv token: when it equals ``serve`` the FastAPI
service mode boots (TECH-1433); otherwise the legacy CLI dispatcher runs.
Existing ``python -m src render``, ``palette extract`` etc. invocations are
unchanged because argparse owns argv from index 0.
"""

from __future__ import annotations

import sys


def _dispatch(argv: list[str]) -> int:
    if argv and argv[0] == "serve":
        from .serve import main as _serve_main

        # Strip the dispatcher token so serve.main sees a clean argv.
        return _serve_main(argv[1:])

    from .cli import main as _cli_main

    return _cli_main(argv)


raise SystemExit(_dispatch(sys.argv[1:]))

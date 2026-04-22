"""HTTP client for tg-catalog-api (TECH-674..TECH-675)."""

from __future__ import annotations

import os
from pathlib import Path
from typing import Any, Optional

import requests

_TOOL_ROOT = Path(__file__).resolve().parent.parent


class RegistryClientError(Exception):
    """Base for registry client failures."""


class CatalogConfigError(RegistryClientError):
    """Missing catalog base URL when required."""


class RegistryConnectionError(RegistryClientError):
    """Transport failure (avoid shadowing builtins.ConnectionError in public API)."""


class ConflictError(RegistryClientError):
    """HTTP 409 from create."""

    def __init__(self, message: str = "conflict", body: Any = None):
        super().__init__(message)
        self.body = body


class ValidationError(RegistryClientError):
    """HTTP 422 or validation."""

    def __init__(self, message: str, errors: Any = None):
        super().__init__(message)
        self.errors = errors


def resolve_catalog_url() -> str:
    env_raw = os.environ.get("TG_CATALOG_API_URL", "").strip()
    if env_raw:
        return env_raw.rstrip("/")
    cfg = _TOOL_ROOT / "config.toml"
    if cfg.exists():
        import tomllib

        data = tomllib.loads(cfg.read_text(encoding="utf-8"))
        cat = data.get("catalog") or {}
        url = str(cat.get("url") or "").strip()
        if url:
            return url.rstrip("/")
    raise CatalogConfigError(
        "catalog URL missing: set TG_CATALOG_API_URL or [catalog] url in tools/sprite-gen/config.toml"
    )


class RegistryClient:
    def __init__(self, base_url: str, timeout: int = 5) -> None:
        self._base = base_url.rstrip("/")
        self._timeout = timeout
        self._session = requests.Session()

    def create_asset(self, payload: dict[str, Any]) -> dict[str, Any]:
        url = f"{self._base}/api/catalog/assets"
        try:
            r = self._session.post(url, json=payload, timeout=self._timeout)
        except requests.RequestException as exc:
            raise RegistryConnectionError(str(exc)) from exc
        if r.status_code in (200, 201):
            return r.json() if r.content else {}
        if r.status_code == 409:
            body: Any
            try:
                body = r.json()
            except Exception:
                body = r.text
            raise ConflictError("duplicate slug", body)
        if r.status_code == 422:
            try:
                err_body = r.json()
            except Exception:
                err_body = r.text
            raise ValidationError("validation failed", err_body)
        raise RegistryClientError(f"HTTP {r.status_code}: {r.text[:200]}")

    def patch_asset(self, asset_id: int, payload: dict[str, Any], updated_at: str) -> dict[str, Any]:
        url = f"{self._base}/api/catalog/assets/{asset_id}"
        body = {**payload, "updated_at": updated_at}
        try:
            r = self._session.patch(url, json=body, timeout=self._timeout)
        except requests.RequestException as exc:
            raise RegistryConnectionError(str(exc)) from exc
        if r.status_code in (200, 201):
            return r.json() if r.content else {}
        if r.status_code == 409:
            raise ConflictError("patch conflict", r.text)
        if r.status_code == 422:
            raise ValidationError("validation failed", r.text)
        raise RegistryClientError(f"HTTP {r.status_code}: {r.text[:200]}")

    def get_asset_by_slug(self, slug: str) -> Optional[dict[str, Any]]:
        url = f"{self._base}/api/catalog/assets"
        try:
            r = self._session.get(url, params={"limit": 500}, timeout=self._timeout)
        except requests.RequestException as exc:
            raise RegistryConnectionError(str(exc)) from exc
        if r.status_code != 200:
            return None
        data = r.json()
        assets = data.get("assets") or []
        for a in assets:
            if isinstance(a, dict) and a.get("slug") == slug:
                return a
        return None


__all__ = [
    "CatalogConfigError",
    "ConflictError",
    "RegistryClient",
    "RegistryClientError",
    "RegistryConnectionError",
    "ValidationError",
    "resolve_catalog_url",
]

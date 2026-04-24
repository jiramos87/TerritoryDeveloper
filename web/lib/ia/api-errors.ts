import { NextResponse } from "next/server";
import type { IaErrorBody, IaErrorCode } from "@/types/api/ia-api";

export function iaJsonError(
  status: 400 | 404 | 500,
  code: IaErrorCode,
  message: string,
  details?: unknown,
) {
  console.error(`[ia-api]`, status, code, message, details ?? "");
  const body: IaErrorBody = { error: message, code };
  if (details !== undefined) body.details = details;
  return NextResponse.json(body, { status });
}

export function isDbConfigError(e: unknown): boolean {
  return e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.";
}

export function postgresErrorResponse(e: unknown, context: string) {
  console.error(`[ia-api] postgres error`, context, e);
  return iaJsonError(500, "internal", `${context} failed`);
}

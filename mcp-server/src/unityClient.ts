import { randomUUID } from "node:crypto";

const DEFAULT_PORT = Number(process.env.MONSTERBATTLER_MCP_PORT ?? 17984);
const DEFAULT_HOST = process.env.MONSTERBATTLER_MCP_HOST ?? "127.0.0.1";

export interface UnityResponse {
  id?: string;
  ok: boolean;
  result?: unknown;
  error?: { message: string; type?: string; stack?: string };
}

/**
 * Sends one JSON command to the Unity Editor HTTP bridge and returns its result.
 * Throws on transport errors or `ok: false` responses.
 */
export async function unityCall(
  command: string,
  params: Record<string, unknown> = {},
  opts: { timeoutMs?: number; host?: string; port?: number } = {}
): Promise<unknown> {
  const id = randomUUID();
  const url = `http://${opts.host ?? DEFAULT_HOST}:${opts.port ?? DEFAULT_PORT}/`;
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), opts.timeoutMs ?? 60_000);

  let res: Response;
  try {
    res = await fetch(url, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ id, command, params }),
      signal: controller.signal,
    });
  } catch (e: any) {
    clearTimeout(timeout);
    if (e?.name === "AbortError") {
      throw new Error(`Unity bridge timeout after ${opts.timeoutMs ?? 60_000}ms (command="${command}")`);
    }
    throw new Error(
      `Unity bridge unreachable at ${url}. Is the Unity Editor open with the MonsterBattler MCP bridge running? (${e?.message ?? e})`
    );
  }
  clearTimeout(timeout);

  if (!res.ok) {
    throw new Error(`Unity bridge HTTP ${res.status} ${res.statusText}`);
  }

  const body = (await res.json()) as UnityResponse;
  if (!body.ok) {
    const err = body.error ?? { message: "Unknown error" };
    const msg = err.type ? `${err.type}: ${err.message}` : err.message;
    throw new Error(`Unity command "${command}" failed: ${msg}`);
  }
  return body.result;
}

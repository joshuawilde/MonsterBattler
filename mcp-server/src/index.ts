#!/usr/bin/env node
import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";
import { unityCall } from "./unityClient.js";

const server = new Server(
  { name: "monsterbattler-mcp", version: "0.1.0" },
  { capabilities: { tools: {} } }
);

interface ToolDef {
  name: string;
  description: string;
  inputSchema: Record<string, unknown>;
  /** Returns the Unity command name and params to forward. */
  handler: (args: any) => Promise<unknown>;
}

/**
 * One generic `unity_call` is the escape hatch for everything the Unity bridge
 * supports. The first-class wrappers below give Claude better discoverability
 * for the hot paths.
 */
const tools: ToolDef[] = [
  {
    name: "unity_call",
    description:
      "Generic escape hatch: invoke any registered Unity bridge command by name. Use unity_list_commands to discover commands. " +
      "Each command returns JSON. Use this for anything not covered by the first-class wrappers.",
    inputSchema: {
      type: "object",
      required: ["command"],
      properties: {
        command: { type: "string", description: "Unity bridge command name, e.g. 'scene.get_hierarchy'." },
        params: { type: "object", description: "Parameters object passed to the command." },
      },
    },
    handler: async ({ command, params }) => unityCall(command, params ?? {}),
  },
  {
    name: "unity_list_commands",
    description: "List every command registered on the Unity bridge.",
    inputSchema: { type: "object", properties: {} },
    handler: async () => unityCall("meta.list_commands"),
  },
  {
    name: "unity_ping",
    description: "Check that the Unity Editor bridge is reachable. Returns pong + timestamp.",
    inputSchema: { type: "object", properties: {} },
    handler: async () => unityCall("meta.ping"),
  },
  {
    name: "unity_batch",
    description:
      "Run multiple Unity commands in one round-trip. Each op is { command, params }. " +
      "Set stopOnError to halt on first failure (default true). Prefer this over many unity_call calls when doing multi-step edits.",
    inputSchema: {
      type: "object",
      required: ["ops"],
      properties: {
        ops: {
          type: "array",
          items: {
            type: "object",
            required: ["command"],
            properties: {
              command: { type: "string" },
              params: { type: "object" },
            },
          },
        },
        stopOnError: { type: "boolean", default: true },
      },
    },
    handler: async ({ ops, stopOnError = true }) =>
      unityCall("meta.batch", { ops, stopOnError }),
  },
  {
    name: "scene_get_hierarchy",
    description: "Return the active scene's GameObject hierarchy as a tree.",
    inputSchema: {
      type: "object",
      properties: {
        maxDepth: { type: "integer", description: "Limit recursion (default 64).", default: 64 },
        includeComponents: { type: "boolean", default: true },
      },
    },
    handler: async (args) => unityCall("scene.get_hierarchy", args ?? {}),
  },
  {
    name: "scene_get_object",
    description: "Get full details of one GameObject (transform + component list). Identify by path or id.",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string", description: "Scene path like 'Stage/Slot1/Sphere'." },
        id: { type: "integer", description: "Unity instanceID (alternative to path)." },
      },
    },
    handler: async (args) => unityCall("scene.get_object", args ?? {}),
  },
  {
    name: "gameobject_create",
    description:
      "Create a new GameObject in the active scene. Optionally a primitive (Cube/Sphere/Capsule/Cylinder/Plane/Quad) and a parent.",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", default: "GameObject" },
        primitive: { type: "string", enum: ["Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad"] },
        parent: {
          type: "object",
          properties: { path: { type: "string" }, id: { type: "integer" } },
        },
      },
    },
    handler: async (args) => unityCall("gameobject.create", args ?? {}),
  },
  {
    name: "gameobject_set_transform",
    description: "Set transform fields on a GameObject. Vectors are [x,y,z]. Omitted fields are left unchanged.",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string" },
        id: { type: "integer" },
        position: { type: "array", items: { type: "number" }, minItems: 3, maxItems: 3 },
        localPosition: { type: "array", items: { type: "number" }, minItems: 3, maxItems: 3 },
        eulerAngles: { type: "array", items: { type: "number" }, minItems: 3, maxItems: 3 },
        localEulerAngles: { type: "array", items: { type: "number" }, minItems: 3, maxItems: 3 },
        localScale: { type: "array", items: { type: "number" }, minItems: 3, maxItems: 3 },
      },
    },
    handler: async (args) => unityCall("gameobject.set_transform", args ?? {}),
  },
  {
    name: "component_add",
    description: "Add a component to a GameObject. Type is a short or fully-qualified type name.",
    inputSchema: {
      type: "object",
      required: ["type"],
      properties: {
        path: { type: "string" },
        id: { type: "integer" },
        type: { type: "string", description: "e.g. 'UnityEngine.UI.Image' or 'Rigidbody'." },
      },
    },
    handler: async (args) => unityCall("component.add", args ?? {}),
  },
  {
    name: "component_set_fields",
    description:
      "Set multiple serialized fields on a component in one call. " +
      "Fields are keyed by SerializedProperty path. Vectors as [x,y,z(,w)], colors as [r,g,b,a]. " +
      "Object references: { id } for scene/asset objects, or { assetPath, assetType } for assets.",
    inputSchema: {
      type: "object",
      required: ["fields"],
      properties: {
        path: { type: "string", description: "Owning GameObject path." },
        id: { type: "integer", description: "Owning GameObject instanceID." },
        type: { type: "string", description: "Component type (required unless componentId given)." },
        componentId: { type: "integer", description: "Component instanceID (skips path+type lookup)." },
        fields: { type: "object" },
      },
    },
    handler: async (args) => unityCall("component.set_fields", args ?? {}),
  },
  {
    name: "prefab_instantiate",
    description: "Instantiate a prefab asset into the active scene under an optional parent.",
    inputSchema: {
      type: "object",
      required: ["assetPath"],
      properties: {
        assetPath: { type: "string", description: "e.g. 'Assets/Prefabs/HPBar.prefab'." },
        parent: { type: "object", properties: { path: { type: "string" }, id: { type: "integer" } } },
        name: { type: "string" },
      },
    },
    handler: async (args) => unityCall("prefab.instantiate", args ?? {}),
  },
  {
    name: "prefab_save_as",
    description: "Save a scene GameObject as a prefab asset (and connect the instance by default).",
    inputSchema: {
      type: "object",
      required: ["assetPath"],
      properties: {
        path: { type: "string" },
        id: { type: "integer" },
        assetPath: { type: "string" },
        connectInstance: { type: "boolean", default: true },
      },
    },
    handler: async (args) => unityCall("prefab.save_as", args ?? {}),
  },
  {
    name: "ui_create_canvas",
    description: "Create a uGUI Canvas with CanvasScaler + GraphicRaycaster, plus an EventSystem if one doesn't exist.",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", default: "Canvas" },
        renderMode: { type: "string", enum: ["ScreenSpaceOverlay", "ScreenSpaceCamera", "WorldSpace"], default: "ScreenSpaceOverlay" },
      },
    },
    handler: async (args) => unityCall("ui.create_canvas", args ?? {}),
  },
  {
    name: "ui_set_rect",
    description:
      "Set RectTransform fields in one call. Vectors are [x,y]. Omitted fields are left unchanged. " +
      "Set anchorMin/anchorMax (e.g. both [0.5,0.5] for centered) + pivot + anchoredPosition + sizeDelta, " +
      "OR set offsetMin/offsetMax to stretch.",
    inputSchema: {
      type: "object",
      properties: {
        path: { type: "string" },
        id: { type: "integer" },
        anchorMin: { type: "array", items: { type: "number" }, minItems: 2, maxItems: 2 },
        anchorMax: { type: "array", items: { type: "number" }, minItems: 2, maxItems: 2 },
        pivot: { type: "array", items: { type: "number" }, minItems: 2, maxItems: 2 },
        anchoredPosition: { type: "array", items: { type: "number" }, minItems: 2, maxItems: 2 },
        sizeDelta: { type: "array", items: { type: "number" }, minItems: 2, maxItems: 2 },
        offsetMin: { type: "array", items: { type: "number" }, minItems: 2, maxItems: 2 },
        offsetMax: { type: "array", items: { type: "number" }, minItems: 2, maxItems: 2 },
      },
    },
    handler: async (args) => unityCall("ui.set_rect", args ?? {}),
  },
  {
    name: "assets_refresh",
    description: "Re-import the asset database. Pass save=true to also save dirty assets first.",
    inputSchema: {
      type: "object",
      properties: { save: { type: "boolean", default: false } },
    },
    handler: async (args) => unityCall("meta.refresh_assets", args ?? {}),
  },
  {
    name: "console_get_logs",
    description:
      "Read Unity console logs captured since the bridge started. Supports tail (last N), severity filter, " +
      "regex pattern, sinceSeq (only entries with seq > sinceSeq — use this to incrementally poll), and includeStack.",
    inputSchema: {
      type: "object",
      properties: {
        tail: { type: "integer", default: 100, description: "Return only the last N matching entries (0 disables)." },
        severity: {
          oneOf: [
            { type: "string", enum: ["Log", "Warning", "Error", "Assert", "Exception"] },
            { type: "array", items: { type: "string", enum: ["Log", "Warning", "Error", "Assert", "Exception"] } },
          ],
        },
        pattern: { type: "string", description: "Case-insensitive regex applied to the message." },
        sinceSeq: { type: "integer", description: "Only entries with seq strictly greater than this." },
        includeStack: { type: "boolean", default: false },
      },
    },
    handler: async (args) => unityCall("console.get_logs", args ?? {}),
  },
  {
    name: "console_count",
    description: "Count captured log entries per severity (and total).",
    inputSchema: { type: "object", properties: {} },
    handler: async () => unityCall("console.count"),
  },
  {
    name: "console_clear",
    description: "Clear the captured console buffer. Returns the number of entries dropped.",
    inputSchema: { type: "object", properties: {} },
    handler: async () => unityCall("console.clear"),
  },
  {
    name: "console_last_error",
    description: "Return the most recent Error/Exception/Assert entry, or null if there is none.",
    inputSchema: {
      type: "object",
      properties: { includeStack: { type: "boolean", default: true } },
    },
    handler: async (args) => unityCall("console.last_error", args ?? {}),
  },
  {
    name: "playmode_state",
    description: "Snapshot of play-mode state: isPlaying, isPaused, isCompiling, isUpdating, isPlayingOrWillChange.",
    inputSchema: { type: "object", properties: {} },
    handler: async () => unityCall("playmode.state"),
  },
  {
    name: "playmode_enter",
    description:
      "Enter play mode. Returns immediately; the transition is asynchronous. Poll playmode_state to confirm.",
    inputSchema: { type: "object", properties: {} },
    handler: async () => unityCall("playmode.enter"),
  },
  {
    name: "playmode_exit",
    description: "Exit play mode. Returns immediately; transition is asynchronous.",
    inputSchema: { type: "object", properties: {} },
    handler: async () => unityCall("playmode.exit"),
  },
  {
    name: "playmode_pause",
    description: "Pause play mode (must already be playing).",
    inputSchema: { type: "object", properties: {} },
    handler: async () => unityCall("playmode.pause"),
  },
  {
    name: "playmode_unpause",
    description: "Resume play mode from pause.",
    inputSchema: { type: "object", properties: {} },
    handler: async () => unityCall("playmode.unpause"),
  },
  {
    name: "playmode_step",
    description: "Advance one frame while paused in play mode.",
    inputSchema: { type: "object", properties: {} },
    handler: async () => unityCall("playmode.step"),
  },
];

server.setRequestHandler(ListToolsRequestSchema, async () => ({
  tools: tools.map(({ handler, ...rest }) => rest),
}));

server.setRequestHandler(CallToolRequestSchema, async (req) => {
  const tool = tools.find((t) => t.name === req.params.name);
  if (!tool) {
    return {
      isError: true,
      content: [{ type: "text", text: `Unknown tool: ${req.params.name}` }],
    };
  }
  try {
    const result = await tool.handler(req.params.arguments ?? {});
    return {
      content: [{ type: "text", text: JSON.stringify(result, null, 2) }],
    };
  } catch (e: any) {
    return {
      isError: true,
      content: [{ type: "text", text: e?.message ?? String(e) }],
    };
  }
});

const transport = new StdioServerTransport();
await server.connect(transport);

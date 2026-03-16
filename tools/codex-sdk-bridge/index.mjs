import { mkdir, readFile, rename, rm, writeFile } from "node:fs/promises";
import path from "node:path";
import process from "node:process";
import { Codex } from "@openai/codex-sdk";

const requestPath = getArgumentValue("--request");

if (!requestPath) {
  emit({ type: "failed", message: "Missing --request argument." });
  process.exit(1);
}

let request;

try {
  request = JSON.parse(await readFile(requestPath, "utf8"));
} catch (error) {
  emit({ type: "failed", message: `Unable to read request payload: ${error.message}` });
  process.exit(1);
}

const storePath = resolveStorePath();
const threadKey = request.chatSessionId || request.runId;
const threadStore = await loadThreadStore(storePath);
const savedThreadId = threadStore[threadKey]?.threadId ?? null;

const codex = new Codex();
const threadOptions = {
  workingDirectory: request.workingDirectory,
  skipGitRepoCheck: true,
  approvalPolicy: "never",
  sandboxMode: "danger-full-access",
  networkAccessEnabled: true,
};

const thread = savedThreadId
  ? codex.resumeThread(savedThreadId, threadOptions)
  : codex.startThread(threadOptions);

emit({ type: "started", threadId: savedThreadId });

try {
  const streamedTurn = await thread.runStreamed(request.prompt);
  let finalResponse = "";
  let lastAssistantStream = "";

  for await (const event of streamedTurn.events) {
    switch (event.type) {
      case "turn.started":
        emit({ type: "progress", message: "Thinking through the request..." });
        break;
      case "item.started":
      case "item.updated":
      case "item.completed":
        if (event.item?.type === "agent_message" && typeof event.item.text === "string") {
          finalResponse = event.item.text;
          if (event.item.text.trim() && event.item.text !== lastAssistantStream) {
            lastAssistantStream = event.item.text;
            emit({ type: "assistant_stream", message: event.item.text });
          }
        }
        emitProgressForItem(event.item);
        break;
      case "turn.completed":
        break;
      case "turn.failed":
        emit({
          type: "failed",
          message: event.error?.message ?? "Codex turn failed.",
        });
        process.exit(1);
        break;
      case "error":
        emit({
          type: "failed",
          message: event.message,
        });
        process.exit(1);
        break;
    }
  }

  const resolvedThreadId = thread.id;

  if (resolvedThreadId) {
    threadStore[threadKey] = {
      threadId: resolvedThreadId,
      updatedAt: new Date().toISOString(),
      workingDirectory: request.workingDirectory,
      workspaceId: request.workspaceId ?? null,
    };
    await saveThreadStore(storePath, threadStore);
  }

  emit({
    type: "completed",
    threadId: resolvedThreadId,
    finalResponse,
  });
} catch (error) {
  emit({
    type: "failed",
    message: error instanceof Error ? error.message : String(error),
  });
  process.exit(1);
}

function getArgumentValue(flag) {
  const index = process.argv.indexOf(flag);
  if (index === -1 || index === process.argv.length - 1) {
    return null;
  }

  return process.argv[index + 1];
}

function resolveStorePath() {
  const root = process.env.LOCALAPPDATA
    ? path.join(process.env.LOCALAPPDATA, "TakomiCode", "codex-sdk-bridge")
    : path.join(process.cwd(), ".takomi", "codex-sdk-bridge");

  return path.join(root, "thread-store.json");
}

async function loadThreadStore(storeFilePath) {
  try {
    const content = await readFile(storeFilePath, "utf8");
    return JSON.parse(content);
  } catch {
    return {};
  }
}

async function saveThreadStore(storeFilePath, store) {
  const directory = path.dirname(storeFilePath);
  await mkdir(directory, { recursive: true });

  const tempFilePath = `${storeFilePath}.${process.pid}.tmp`;
  await writeFile(tempFilePath, JSON.stringify(store, null, 2), "utf8");
  await rename(tempFilePath, storeFilePath);

  await rm(tempFilePath, { force: true }).catch(() => {});
}

function emit(payload) {
  process.stdout.write(`${JSON.stringify(payload)}\n`);
}

function emitProgressForItem(item) {
  if (!item || typeof item !== "object") {
    return;
  }

  switch (item.type) {
    case "reasoning":
      if (item.text && item.text.length <= 120) {
        emit({ type: "progress", message: item.text });
      }
      break;
    case "command_execution":
      if (item.command) {
        const summary = summarizeCommand(item.command, item.status);
        if (summary) {
          emit({ type: "progress", message: summary });
        }
      }
      break;
    case "file_change":
      if (Array.isArray(item.changes) && item.changes.length > 0) {
        const summary = item.changes
          .slice(0, 4)
          .map((change) => normalizePathForDisplay(change.path))
          .join(", ");
        emit({ type: "progress", message: `Changed files: ${summary}` });
      }
      break;
    case "todo_list":
      if (Array.isArray(item.items) && item.items.length > 0) {
        const nextTodo = item.items.find((entry) => !entry.completed)?.text ?? item.items[0]?.text;
        if (nextTodo) {
          emit({ type: "progress", message: `Plan update: ${nextTodo}` });
        }
      }
      break;
  }
}

function summarizeCommand(command, status) {
  const lower = command.toLowerCase();
  const suffix = status === "completed" ? "" : "...";

  if (lower.includes("skill.md")) {
    return "Checking project guidance" + suffix;
  }

  const getContentMatch = command.match(/Get-Content(?:\s+-Path)?\s+'([^']+)'/i);
  if (getContentMatch) {
    return `Reading \`${shortenDisplayPath(getContentMatch[1])}\`${suffix}`;
  }

  const getChildItemMatch = command.match(/Get-ChildItem(?:\s+-Path)?\s+'([^']+)'/i);
  if (getChildItemMatch) {
    return `Inspecting \`${shortenDisplayPath(getChildItemMatch[1])}\`${suffix}`;
  }

  if (lower.includes("apply_patch")) {
    return "Applying changes" + suffix;
  }

  if (lower.includes("dotnet build") || lower.includes("msbuild")) {
    return "Building the project" + suffix;
  }

  if (lower.includes("pnpm") || lower.includes("npm ")) {
    return "Running package command" + suffix;
  }

  return null;
}

function normalizePathForDisplay(value) {
  return value.replace(/\\/g, "/");
}

function shortenDisplayPath(value) {
  const normalized = normalizePathForDisplay(value);
  const segments = normalized.split("/").filter(Boolean);

  if (segments.length <= 3) {
    return normalized;
  }

  return `.../${segments.slice(-2).join("/")}`;
}

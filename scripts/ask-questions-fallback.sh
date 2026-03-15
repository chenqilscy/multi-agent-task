#!/usr/bin/env bash
# ask-questions-fallback.sh
#
# Fallback script for the AskQuestions tool defined in copilot.instructions.md.
# Priority order: AskQuestions tool → equivalent platform API → THIS SCRIPT
#
# Usage:
#   ./scripts/ask-questions-fallback.sh [--task-id <id>] [--question <text>]
#
# The script presents a multi-select menu, records the selection, writes a
# structured JSON result to stdout, and appends the session record to
#   memories/session/session-<timestamp>.json
#
# Exit codes:
#   0  – user completed selection (result written to stdout)
#   1  – user aborted (Ctrl-C or empty input after retries)
#   2  – script invocation error

set -euo pipefail

###############################################################################
# Helpers
###############################################################################
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
SESSION_DIR="${REPO_ROOT}/memories/session"
mkdir -p "${SESSION_DIR}"

TIMESTAMP="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
TASK_ID="${TASK_ID:-unset}"
QUESTION="${QUESTION:-下一步要我继续做什么任务？可多选，也可以直接输入新的具体任务。}"

# Parse CLI flags
while [[ $# -gt 0 ]]; do
  case "$1" in
    --task-id)  TASK_ID="$2";  shift 2 ;;
    --question) QUESTION="$2"; shift 2 ;;
    *) echo "Unknown option: $1" >&2; exit 2 ;;
  esac
done

###############################################################################
# Preset options  (must contain at least 3; fixed option is ALWAYS last)
###############################################################################
OPTIONS=(
  "继续执行 TODO.md 中的下一个未完成任务"
  "对当前代码进行单元测试补充"
  "对当前实现进行代码审查并修复问题"
  "必须按照文档执行 #file:copilot.instructions.md"
)

###############################################################################
# Display menu
###############################################################################
echo "" >&2
echo "╔══════════════════════════════════════════════════════════════════════╗" >&2
echo "║              EOT=ASK_NEXT_TASK  (copilot.instructions.md)           ║" >&2
echo "╚══════════════════════════════════════════════════════════════════════╝" >&2
echo "" >&2
echo "  ${QUESTION}" >&2
echo "" >&2
echo "  预设选项（输入编号可多选，用逗号或空格分隔，例如 1,3）：" >&2
echo "" >&2
for i in "${!OPTIONS[@]}"; do
  printf "    [%d] %s\n" "$((i+1))" "${OPTIONS[$i]}" >&2
done
echo "    [0] 自定义输入" >&2
echo "" >&2

###############################################################################
# Collect input (up to 2 retries on empty)
###############################################################################
SELECTED_INDICES=()
FREE_INPUT=""
MAX_RETRIES=2
attempt=0

while true; do
  read -r -p "  请输入选项编号或自定义任务描述：" RAW_INPUT || { echo "" >&2; exit 1; }

  if [[ -z "${RAW_INPUT}" ]]; then
    attempt=$((attempt + 1))
    if [[ ${attempt} -gt ${MAX_RETRIES} ]]; then
      echo "未收到有效输入，脚本退出。" >&2
      exit 1
    fi
    echo "  输入不能为空，请重试（剩余 $((MAX_RETRIES - attempt + 1)) 次）。" >&2
    continue
  fi

  # Separate numeric tokens from free text
  for token in ${RAW_INPUT//,/ }; do
    if [[ "${token}" =~ ^[0-9]+$ ]]; then
      SELECTED_INDICES+=("${token}")
    else
      FREE_INPUT="${FREE_INPUT} ${token}"
    fi
  done
  FREE_INPUT="${FREE_INPUT# }"  # trim leading space
  break
done

###############################################################################
# Build selections array
###############################################################################
SELECTED_OPTIONS=()
for idx in "${SELECTED_INDICES[@]}"; do
  if [[ "${idx}" -eq 0 ]]; then
    # user will supply free text
    :
  elif [[ "${idx}" -ge 1 && "${idx}" -le "${#OPTIONS[@]}" ]]; then
    SELECTED_OPTIONS+=("${OPTIONS[$((idx-1))]}")
  else
    echo "  警告：忽略无效编号 ${idx}" >&2
  fi
done

###############################################################################
# Build JSON result
###############################################################################
# Escape a string for JSON using python3 if available, otherwise use sed fallback
json_escape() {
  if command -v python3 &>/dev/null; then
    printf '%s' "$1" | python3 -c 'import json,sys; print(json.dumps(sys.stdin.read()))'
  else
    # Basic fallback: wrap in quotes and escape backslash, double-quote, and common controls
    local s="$1"
    s="${s//\\/\\\\}"
    s="${s//\"/\\\"}"
    s="${s//$'\n'/\\n}"
    s="${s//$'\r'/\\r}"
    s="${s//$'\t'/\\t}"
    printf '"%s"' "${s}"
  fi
}

build_json_array() {
  local out="["
  local first=1
  for item in "${@+"$@"}"; do
    [[ ${first} -eq 0 ]] && out+=","
    out+="$(json_escape "${item}")"
    first=0
  done
  out+="]"
  echo "${out}"
}

SELECTED_JSON="$(build_json_array "${SELECTED_OPTIONS[@]+"${SELECTED_OPTIONS[@]}"}")"
FREE_JSON="$(json_escape "${FREE_INPUT}")"
TASK_ID_JSON="$(json_escape "${TASK_ID}")"
QUESTION_JSON="$(json_escape "${QUESTION}")"
RESULT_JSON="$(cat <<EOF
{
  "timestamp": "${TIMESTAMP}",
  "task_id": ${TASK_ID_JSON},
  "question": ${QUESTION_JSON},
  "selected_options": ${SELECTED_JSON},
  "free_input": ${FREE_JSON}
}
EOF
)"

###############################################################################
# Persist session log
###############################################################################
# Strip colons and hyphens for a clean filename: 2026-03-15T09:59:40Z → 20260315T095940Z
SAFE_TIMESTAMP="${TIMESTAMP//[-:]/}"
SESSION_FILE="${SESSION_DIR}/session-${SAFE_TIMESTAMP}.json"
echo "${RESULT_JSON}" > "${SESSION_FILE}"
echo ""
echo "  ✔ 选择已记录：${SESSION_FILE}" >&2

###############################################################################
# Output structured result to stdout for the calling agent
###############################################################################
echo "${RESULT_JSON}"

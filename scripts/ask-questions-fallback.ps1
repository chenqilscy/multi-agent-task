# ask-questions-fallback.ps1
#
# Fallback script for the AskQuestions tool defined in copilot.instructions.md.
# Priority order: AskQuestions tool → equivalent platform API → THIS SCRIPT
#
# Usage:
#   .\scripts\ask-questions-fallback.ps1 [-TaskId <id>] [-Question <text>]
#
# The script presents a multi-select menu, records the selection, writes a
# structured JSON result to stdout, and appends the session record to
#   memories/session/session-<timestamp>.json
#
# Exit codes:
#   0  – user completed selection (result written to stdout)
#   1  – user aborted or max retries exceeded
#   2  – script invocation error

param(
    [string]$TaskId   = "unset",
    [string]$Question = "下一步要我继续做什么任务？可多选，也可以直接输入新的具体任务。"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

###############################################################################
# Paths
###############################################################################
$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot   = Split-Path -Parent $ScriptDir
$SessionDir = Join-Path $RepoRoot "memories\session"
New-Item -ItemType Directory -Force -Path $SessionDir | Out-Null

$Timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")

###############################################################################
# Preset options  (at least 3; fixed option is ALWAYS last)
###############################################################################
$Options = @(
    "继续执行 TODO.md 中的下一个未完成任务",
    "对当前代码进行单元测试补充",
    "对当前实现进行代码审查并修复问题",
    "必须按照文档执行 #file:copilot.instructions.md"
)

###############################################################################
# Display menu
###############################################################################
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════════════╗"
Write-Host "║              EOT=ASK_NEXT_TASK  (copilot.instructions.md)           ║"
Write-Host "╚══════════════════════════════════════════════════════════════════════╝"
Write-Host ""
Write-Host "  $Question"
Write-Host ""
Write-Host "  预设选项（输入编号可多选，用逗号或空格分隔，例如 1,3）："
Write-Host ""
for ($i = 0; $i -lt $Options.Length; $i++) {
    Write-Host ("    [{0}] {1}" -f ($i + 1), $Options[$i])
}
Write-Host "    [0] 自定义输入"
Write-Host ""

###############################################################################
# Collect input (up to 2 retries on empty)
###############################################################################
$SelectedIndices = @()
$FreeInput       = ""
$MaxRetries      = 2
$attempt         = 0

while ($true) {
    $RawInput = Read-Host "  请输入选项编号或自定义任务描述"

    if ([string]::IsNullOrWhiteSpace($RawInput)) {
        $attempt++
        if ($attempt -gt $MaxRetries) {
            Write-Warning "未收到有效输入，脚本退出。"
            exit 1
        }
        Write-Warning "输入不能为空，请重试（剩余 $($MaxRetries - $attempt + 1) 次）。"
        continue
    }

    # Split on commas and spaces
    $Tokens = $RawInput -split '[,\s]+' | Where-Object { $_ -ne "" }
    foreach ($token in $Tokens) {
        if ($token -match '^\d+$') {
            $SelectedIndices += [int]$token
        } else {
            $FreeInput += " $token"
        }
    }
    $FreeInput = $FreeInput.Trim()
    break
}

###############################################################################
# Build selections list
###############################################################################
$SelectedOptions = [System.Collections.Generic.List[string]]::new()
foreach ($idx in $SelectedIndices) {
    if ($idx -eq 0) {
        # free text will be captured separately
    } elseif ($idx -ge 1 -and $idx -le $Options.Length) {
        $SelectedOptions.Add($Options[$idx - 1])
    } else {
        Write-Warning "  警告：忽略无效编号 $idx"
    }
}

###############################################################################
# Build JSON result
###############################################################################
$ResultObj = [ordered]@{
    timestamp        = $Timestamp
    task_id          = $TaskId
    question         = $Question
    selected_options = $SelectedOptions.ToArray()
    free_input       = $FreeInput
}

$ResultJson = $ResultObj | ConvertTo-Json -Depth 5 -Compress:$false

###############################################################################
# Persist session log
###############################################################################
$SafeTimestamp = $Timestamp -replace '[:\-]', ''
$SessionFile   = Join-Path $SessionDir "session-${SafeTimestamp}.json"
$ResultJson | Out-File -FilePath $SessionFile -Encoding UTF8
Write-Host ""
Write-Host "  ✔ 选择已记录：$SessionFile" -ForegroundColor Green

###############################################################################
# Output structured result to stdout for the calling agent
###############################################################################
Write-Output $ResultJson

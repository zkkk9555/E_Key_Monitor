# Remove Legacy Workflow and Adopt Superpowers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the project-local Codex Self-Cycling Workflow artifacts from `E_Key_Monitor` while preserving the monitor application and making Superpowers the active development process.

**Architecture:** Keep the existing .NET 8 WinForms application, runtime configuration, launch scripts, icons, user documentation, and project safety rules. Delete only workflow state, workflow reports, workflow-specific project configuration, and empty workflow directories. Superpowers remains supplied by the Codex plugin environment; no replacement workflow configuration is installed into the project.

**Tech Stack:** C#, .NET 8, WinForms, Windows x64, PowerShell 5.1-compatible scripts, Superpowers plugin skills.

---

### Task 1: Confirm the removal boundary

**Files:**
- Read: `C:\Projects\Codex_Self-Cycling_Workflow\AGENTS.md`, `README.md`, `PROJECT_GUIDE.md`, `WORKFLOW_STANDARD.md`, and `MAINTENANCE.md`
- Read: current project file inventory and workflow-reference search

- [x] **Step 1: Identify legacy workflow artifacts**

  Remove exactly these current-project files:

  ```text
  ACCEPTANCE.md
  GOAL.md
  LOOP.md
  STATE.md
  WORKFLOW_STANDARD.md
  loop-run-log.md
  workflow-adoption-report.md
  .codex/config.toml
  .codex/agents/workflow-explorer.toml
  .codex/agents/workflow-maker.toml
  .codex/agents/workflow-verifier.toml
  ```

- [x] **Step 2: Confirm preserved application files**

  Preserve `AGENTS.md`, `README.md`, `E_Key_Monitor.csproj`, `Program.cs`, `MainForm.cs`, `config.json`, `Package_E_Key_Monitor.ps1`, all launch/shortcut scripts, the icon assets, and generated build directories.

- [x] **Step 3: Confirm the source workflow repository is read-only**

  Do not write under `C:\Projects\Codex_Self-Cycling_Workflow`. Its workflow files are reference material only.

**Scope note:** The old source paths in `AGENTS.md`/`README.md`, the ordinary-mode documentation contradictions, and the stale four-PS1 count in `AGENTS.md` were independent documentation facts that existed before cleanup. `README.md` already documents `Package_E_Key_Monitor.ps1`. This request only removes the legacy workflow, so `AGENTS.md` and `README.md` remain unchanged. Track the documentation corrections and any related ordinary-mode behavior decision as separate follow-up product/documentation tasks.

### Task 2: Delete only project-local legacy workflow content

**Files:**
- Delete: the eleven legacy files listed in Task 1
- Delete if empty: `.codex`, `.codex/agents`, and `.agents`

- [x] **Step 1: Delete the exact files with `apply_patch`**

  Apply file deletions only to the listed workflow artifacts. Do not edit application source, scripts, configuration templates, or user documentation.

- [x] **Step 2: Remove empty workflow directories**

  After file deletion, remove only empty `.codex`, `.codex/agents`, and `.agents` directories. Leave `.git` and all build/tool directories untouched.

- [x] **Step 3: Confirm no legacy project config remains**

  Verify that the listed files no longer exist and that no current-project `.codex` agent definitions remain.

### Task 3: Verify application integrity after cleanup

**Files:**
- Read: preserved project files and generated outputs
- Test: build and static checks; no new test project is required for metadata-only deletion

- [x] **Step 1: Verify the preserved file inventory**

  Run:

  ```powershell
  rg --files -uu -g '!bin/**' -g '!obj/**' -g '!.appdata/**' -g '!.dotnet-home/**' -g '!.nuget/**'
  ```

  Expected: application source, configuration, scripts, assets, `AGENTS.md`, `README.md`, and the Superpowers plan remain; legacy workflow files are absent.

- [x] **Step 2: Build the application**

  Run:

  ```powershell
  dotnet build -c Release --no-restore
  ```

  Expected: exit code 0, 0 warnings, 0 errors.

- [x] **Step 3: Re-run preserved script and safety checks**

  Run this from the project root in Windows PowerShell 5.1. It enumerates every root `*.ps1` and `*.bat`, checks the PS1 BOM and parser errors, checks BAT bytes above ASCII, parses `config.json`, and searches all preserved C#/PS1/BAT files while excluding generated directories:

  ```powershell
  $ErrorActionPreference = 'Stop'
  $root = (Get-Location).Path
  $failed = $false

  if (-not (Get-Command rg -ErrorAction SilentlyContinue)) {
      throw 'rg is required for this verification.'
  }

  $ps1Files = @(Get-ChildItem -LiteralPath $root -Force -File -Filter '*.ps1')
  $ps1BomFailures = @()
  $ps1ParseErrorCount = 0
  foreach ($file in $ps1Files) {
      $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
      if ($bytes.Length -lt 3 -or $bytes[0] -ne 0xEF -or $bytes[1] -ne 0xBB -or $bytes[2] -ne 0xBF) {
          $ps1BomFailures += $file.FullName
      }

      $tokens = $null
      $parseErrors = $null
      [System.Management.Automation.Language.Parser]::ParseFile(
          $file.FullName,
          [ref]$tokens,
          [ref]$parseErrors
      ) | Out-Null
      $ps1ParseErrorCount += @($parseErrors).Count
  }

  $batFiles = @(Get-ChildItem -LiteralPath $root -Force -File -Filter '*.bat')
  $nonAsciiBatFiles = @()
  foreach ($file in $batFiles) {
      $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
      $highByteCount = @($bytes | Where-Object { $_ -gt 127 }).Count
      if ($highByteCount -gt 0) {
          $nonAsciiBatFiles += '{0} ({1} byte(s) > 127)' -f $file.FullName, $highByteCount
      }
  }

  $config = Get-Content -Encoding UTF8 -Raw -LiteralPath 'config.json' | ConvertFrom-Json
  if ($null -eq $config) {
      throw 'config.json parsed to null.'
  }

  $prohibitedPattern = '(?i)(SendInput|keybd_event|mouse_event|SetCursorPos|SendKeys|PostMessage|WH_MOUSE_LL|ReadProcessMemory|WriteProcessMemory|OpenProcess|VirtualAllocEx|VirtualProtectEx|CreateRemoteThread|Nt(Read|Write)VirtualMemory|LoadLibrary(A|W)?|CreateToolhelp32Snapshot|Process\.Kill|TerminateProcess|Stop-Process|Start-Process.*NarakaBladepoint|Process\.Start.*NarakaBladepoint|NarakaBladepoint\.exe.*(Start|Launch))'
  $prohibitedMatches = @(
      & rg --no-heading --line-number --color never -uu `
          -g '*.cs' -g '*.ps1' -g '*.bat' `
          -g '!bin/**' -g '!obj/**' -g '!.appdata/**' -g '!.dotnet-home/**' -g '!.nuget/**' `
          $prohibitedPattern $root
  )
  $rgExitCode = $LASTEXITCODE
  if ($rgExitCode -gt 1) {
      throw "rg failed with exit code $rgExitCode."
  }

  Write-Output ('Root PS1 files: {0}' -f $ps1Files.Count)
  Write-Output ('PS1 BOM failures: {0}' -f $ps1BomFailures.Count)
  Write-Output ('PS1 parse errors: {0}' -f $ps1ParseErrorCount)
  Write-Output ('Root BAT files: {0}' -f $batFiles.Count)
  Write-Output ('BAT files containing bytes > 127: {0}' -f $nonAsciiBatFiles.Count)
  Write-Output 'config.json: parsed'
  Write-Output ('Prohibited API matches: {0}' -f $prohibitedMatches.Count)

  if ($ps1BomFailures.Count -gt 0) {
      $failed = $true
      $ps1BomFailures | ForEach-Object { Write-Output "BOM failure: $_" }
  }
  if ($ps1ParseErrorCount -ne 0) {
      $failed = $true
  }
  if ($nonAsciiBatFiles.Count -gt 0) {
      $failed = $true
      $nonAsciiBatFiles | ForEach-Object { Write-Output "Non-ASCII BAT: $_" }
  }
  if ($prohibitedMatches.Count -gt 0) {
      $failed = $true
      $prohibitedMatches | ForEach-Object { Write-Output "Prohibited API match: $_" }
  }

  if ($failed) {
      exit 1
  }
  ```

  Expected: all parser/encoding/JSON checks pass and no prohibited API matches are found.

- [x] **Step 4: Confirm the source workflow repository was not modified**

  Run this read-only command to re-list timestamps and compute SHA-256 hashes for the reference files from Task 1. Do not redirect output or write anything under the reference repository:

  ```powershell
  $ErrorActionPreference = 'Stop'
  $referenceRoot = 'C:\Projects\Codex_Self-Cycling_Workflow'
  $referenceFiles = @(
      'AGENTS.md'
      'README.md'
      'PROJECT_GUIDE.md'
      'WORKFLOW_STANDARD.md'
      'MAINTENANCE.md'
  ) | ForEach-Object {
      Join-Path -Path $referenceRoot -ChildPath $_
  }

  Get-Item -LiteralPath $referenceFiles -Force |
      Select-Object FullName, LastWriteTimeUtc, Length
  Get-FileHash -Algorithm SHA256 -LiteralPath $referenceFiles |
      Select-Object Algorithm, Hash, Path
  ```

  Expected: record this read-only snapshot and confirm this task did not write to the reference directory; do not mislabel any pre-existing uncommitted state in the source repository as `clean`.

### Task 4: Continue with Superpowers

**Files:**
- Keep: `docs/superpowers/plans/2026-07-20-remove-legacy-workflow-and-adopt-superpowers.md`
- Do not create: project-local `GOAL.md`, `STATE.md`, `LOOP.md`, `ACCEPTANCE.md`, `.codex` workflow config, or heartbeat configuration

- [x] **Step 1: Use Superpowers skills for future work**

  This management task was executed using `using-superpowers`, `writing-plans`, `subagent-driven-development`, `receiving-code-review`, and `verification-before-completion`. No project-local legacy workflow configuration was created.

- [x] **Step 2: Stop at the feature boundary**

  No concrete new feature requirement was provided, so the task stops at the feature boundary. The ordinary-mode behavior and documentation questions remain separate follow-up product/documentation tasks.

## Subsequent Documentation Audit (2026-07-20)

The scope note above is a historical record of the workflow-cleanup task. A later read-only audit found that the source directory is now `C:\Projects\E_Key_Monitor`, `MainForm.OnLoad` requires a valid `gameDirectory` in both startup modes, the project has five root PS1 files including `Package_E_Key_Monitor.ps1`, and the current desktop shortcuts still target the old source path. `README.md` and `AGENTS.md` were updated to record those facts, the fresh build/static-check results, and the unverified or partial runtime checks. No source code, configuration, dependency, build/deployment script, or generated source was edited, and no new status file was created; verification commands may refresh generated `bin/` and `obj/` outputs.

### Task 5: Final review

- [x] **Step 1: Review the removal diff**

  Confirm the only intentional source-tree changes are legacy workflow deletions and the Superpowers plan. The workspace has an empty `.git` directory, so no commit can be created; report that limitation instead of fabricating VCS evidence.

- [x] **Step 2: Report evidence and remaining risks**

  **Actual results:** Initial spec review: APPROVE. The first quality review identified incomplete hidden-file and static-check plan commands; those were corrected. The second quality review: APPROVE. An overall review then found that the empty `.agents` directory had reappeared after an earlier check; the directory was removed again with an explicit empty-directory guard and elevated permission, and the final local integrity check is recorded after that removal. Build: `dotnet build -c Release --no-restore` exited 0 with 0 warnings and 0 errors. Integrity: 17 preserved files and 0 remaining legacy workflow files/directories after the final cleanup. Scripts and safety: 5 root PS1 files, 0 BOM failures, 0 parser errors; 3 root BAT files, 0 bytes above 127; `config.json` parsed; 0 prohibited API matches. The read-only reference snapshot recorded these SHA-256 hashes: `AGENTS.md` `5D358C0F121F18FACF6C61907F93934F9E8514962643EAEAACEB061AB6DCFC8E`, `README.md` `86E8D90131A0238DB8CDF8C1F931996F4E028A104C3241A74D92BB47E05254A5`, `PROJECT_GUIDE.md` `DE82B38D71604034C6540E398A3C558A59EB4A455C98B8A6E07D39AD49183B47`, `WORKFLOW_STANDARD.md` `145BD918D15BB4543ADC4581A65D0EEE9DBF946F4FE2B912D74A3641755E827C`, and `MAINTENANCE.md` `91B9990B7195C23FD3D777FFCE3E502F24261F251009B440DD5EBA6EB59110CD`; their timestamps predated this task and this task did not write to the reference directory. The `.git` directory is empty, so no diff or commit can be provided. The game, launch scripts, shortcut scripts, and GUI were not run.

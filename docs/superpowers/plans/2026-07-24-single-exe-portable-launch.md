# 单 EXE 便携启动与双快捷方式 Implementation Plan

> **状态（2026-10-22 核对）：** 这份计划描述的工作**已经实现并发布**（单文件 self-contained EXE + 两个直接指向 EXE 的桌面快捷方式）。下面的复选框是当时的执行记录、没有回填，**不要照着重做一遍**。当前事实以仓库根目录 `AGENTS.md` 为准。

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将日常使用收敛为一个 `E_Key_Monitor.exe` 和两个直接指向 EXE 的桌面快捷方式，首次选择并保存游戏目录，便携包不暴露 BAT 或 PowerShell 入口。

**Architecture:** 保留现有监控窗口和 `--follow-game` 逻辑，在同一个 `E_Key_Monitor.exe` 增加 `--launch-game` 模式。该模式先完成现有目录校验，再以已验证目录为工作目录启动原始 `StartGame.exe`，同时启用现有游戏进程跟随。托盘菜单负责用当前 EXE 创建两个快捷方式；打包脚本只复制单文件 EXE、空路径配置和中文说明书。

**Tech Stack:** C#, .NET 8, WinForms, Windows x64, Windows PowerShell 5.1 packaging script, WScript.Shell shortcut COM API.

---

### Task 1: 将两个启动模式合并到同一个 EXE

**Files:**
- Modify: `C:\Projects\E_Key_Monitor\Program.cs`
- Modify: `C:\Projects\E_Key_Monitor\MainForm.cs`

- [ ] **Step 1: Extend command-line mode parsing**

在 `Program.Main` 中识别 `--launch-game`，令该参数同时启用 follow-game；无参数保持独立监控，已有 `--follow-game` 保持只跟随不启动游戏：

```csharp
bool launchGame = args.Any(
    arg => string.Equals(arg, "--launch-game", StringComparison.OrdinalIgnoreCase));
bool followGame = launchGame || args.Any(
    arg => string.Equals(arg, "--follow-game", StringComparison.OrdinalIgnoreCase));

Application.Run(new MainForm(followGame, launchGame));
```

- [ ] **Step 2: Add launch mode state and start the original launcher**

将构造函数改为 `MainForm(bool followGame, bool launchGame)`，保存 `_launchGame`。在 `OnLoad` 中沿用 `EnsureGameDirectoryConfigured()`，成功后调用 `StartConfiguredGameLauncher()`，再安装键盘 Hook。启动方法只使用：

```csharp
ProcessStartInfo startInfo = new()
{
    FileName = Path.Combine(_settings.GameDirectory!, "StartGame.exe"),
    WorkingDirectory = _settings.GameDirectory!,
    UseShellExecute = true
};
Process.Start(startInfo)?.Dispose();
```

若启动器不存在、启动失败或目录为空，显示中文错误并请求安全退出；绝不直接启动 `NarakaBladepoint.exe`。

- [ ] **Step 3: Translate user-visible menu and directory messages**

将菜单项替换为“设置游戏目录...”，新增“生成桌面快捷方式”，并把窗口标题、托盘提示、目录选择说明、目录校验错误、保存失败错误和游戏等待超时提示改为中文；保留真实 EXE 名称和进程名不翻译。

- [ ] **Step 4: Add tray shortcut creation**

在 `MainForm` 内增加创建桌面快捷方式的方法：

```text
桌面/E键监控工具.lnk
  TargetPath = 当前 E_Key_Monitor.exe
  Arguments = 空

桌面/启动StartGame和监控.lnk
  TargetPath = 当前 E_Key_Monitor.exe
  Arguments = --launch-game
```

两个快捷方式使用当前 EXE 的 `IconLocation`、当前 EXE 所在目录作为工作目录，并在 COM 创建失败时显示中文错误。快捷方式文件名是用户入口，BAT/PowerShell 不参与运行。

### Task 2: 恢复空游戏路径并整理便携打包

**Files:**
- Modify: `C:\Projects\E_Key_Monitor\config.json`
- Modify: `C:\Projects\E_Key_Monitor\Package_E_Key_Monitor.ps1`

- [ ] **Step 1: Set portable default configuration**

将根目录 `config.json` 的 `gameDirectory` 改回 `null`，保持 UTF-8 with BOM；构建和发布时该空值复制到 EXE 旁边。其他圆点、穿透和位置默认值不变。

- [ ] **Step 2: Change the package contract**

继续使用单文件 self-contained `dotnet publish`，但把发布目录验收从两个文件改为三个文件：`E_Key_Monitor.exe`、`config.json`、`E键监控工具中文使用说明书.md`。复制说明书到便携目录，并让 ZIP 同时包含这三个文件。

- [ ] **Step 3: Keep packaging outside the source tree**

保留现有输出安全检查：输出目录必须是源码目录外的 `E_Key_Monitor-portable`，清理范围只允许该输出目录、同名 ZIP 和临时发布目录；不把 BAT、PS1、源码或旧快捷方式复制进包内。

### Task 3: 对齐用户说明和维护文档

**Files:**
- Modify: `C:\Projects\E_Key_Monitor\README.md`
- Modify: `C:\Projects\E_Key_Monitor\AGENTS.md`
- Modify: `C:\Projects\E_Key_Monitor\E键监控工具中文使用说明书.md`
- Modify: `C:\Projects\E_Key_Monitor\docs\superpowers\specs\2026-07-24-single-exe-portable-launch-design.md`

- [ ] **Step 1: Document the two EXE-backed shortcuts**

删除日常使用必须双击 BAT 的表述，统一说明两个快捷方式、`--launch-game` 的作用、快捷方式生成位置和移动便携文件夹后重新生成的规则。

- [ ] **Step 2: Document first-run path persistence**

明确 `gameDirectory` 默认为空；首次选择成功保存到 EXE 旁边的 `config.json`；有效路径不重复询问；失效路径再次询问。记录 `StartGame.exe` 和 `NarakaBladepoint.exe` 必须在同一目录。

- [ ] **Step 3: Separate source maintenance files from the portable package**

说明源码目录可以保留开发和兼容脚本，但最终便携包不包含它们；现有旧 BAT/PS1 不作为用户入口，不删除历史文件。

### Task 4: Build and verify

**Files:**
- Read-only verification of all modified files and generated package output.

- [ ] **Step 1: Run code and packaging checks**

执行：

```powershell
dotnet build -c Release --no-restore
dotnet publish -c Release -r win-x64 --self-contained true --no-restore
dotnet format E_Key_Monitor.csproj --verify-no-changes --no-restore
dotnet test -c Release --no-restore --verbosity normal
```

预期：构建和发布退出码为 0；格式检查退出码为 0；测试命令退出码为 0，但若项目仍没有测试项目，报告为 PARTIAL 而不是自动化测试通过。

- [ ] **Step 2: Verify package contents and shortcut target logic**

静态检查 PowerShell 5.1 解析、脚本 BOM、配置 JSON、中文菜单文本、游戏 EXE 存在性；运行打包脚本到源码外临时输出，确认目录和 ZIP 只含一个 EXE、配置和中文说明书，不启动游戏、不修改桌面。

- [ ] **Step 3: Verify source boundary and runtime limits**

检查 Git 状态，确认没有测试、依赖或锁文件改动；不声称已完成 GUI、键盘输入、实际游戏启动或游戏退出清理验证，除非本次确实执行并读取到结果。

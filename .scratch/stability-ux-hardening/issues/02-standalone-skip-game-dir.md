# 02: Standalone skip game directory

**Type:** task
**What to build:** 纯独立模式允许跳过游戏目录选择；跟随/联合模式仍强制。
**Anchors:** `MainForm.cs :: EnsureGameDirectoryConfigured`, `MainForm.cs :: OnLoad`
**Blocked by:** None
**Status:** resolved

- [x] 无参数启动 + 目录无效时弹 Yes/No/Cancel：选择目录 / 跳过运行 / 退出
- [x] 跳过后 `GameDirectory` 保持 null，托盘“设置游戏目录...”仍可后补
- [x] `--follow-game` / `--launch-game` 保持强制选择，取消即退出

## Answer

实现：`EnsureGameDirectoryConfigured` 按 `_followGame||_launchGame` 区分强制/可选；纯独立模式取消选择后弹 Yes/No/Cancel（跳过运行/重选/退出），跳过记诊断日志。verify：`dotnet build -c Release --no-restore` 0 警告 0 错误。

# 03: Shutdown watchdog

**Type:** task
**What to build:** 优雅关闭超时后强制退出，杜绝僵尸进程。
**Anchors:** `MainForm.cs :: RequestShutdown`, `MainForm.cs :: CleanupResources`
**Blocked by:** None
**Status:** resolved

- [x] `RequestShutdown` 启动 8s 一次性看门狗；正常关闭在 `CleanupResources` 解除
- [x] 超时未退出则写诊断条目后强制结束进程
- [x] 正常路径行为不变（无额外弹窗、无提前退出）

## Answer

实现：`RequestShutdown` 启动 8s 一次性 `System.Threading.Timer` 看门狗，超时写诊断后 `Environment.Exit(1)`；`CleanupResources` 首行 `Interlocked.Exchange` 取出并 Dispose 解除。verify：`dotnet build -c Release --no-restore` 0 警告 0 错误。

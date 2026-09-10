# 01: Render containment + self-heal

**Type:** task
**What to build:** 越界窗口钳制后继续绘制 + 瞬时绘制失败按 1/s 重试自愈，不再首次绘制失败即退出。
**Anchors:** `MainForm.cs :: RenderLayeredWindow`, `MainForm.cs :: UpdateActivationState`, `MainForm.cs :: InitializeVisibleWindow`
**Blocked by:** None
**Status:** resolved

- [x] 窗口部分越界时钳制进工作区并继续绘制，不记失败、不退出
- [x] 瞬时失败置 `_renderDirty`，`UpdateActivationState` 每 tick 最多 1/s 重试，成功即恢复
- [x] 重试节流避免日志刷屏（持续失败不高于约 1 条/秒）
- [x] 初次绘制兜底退出路径保留（仅走投无路时）

## Answer

实现：`RenderLayeredWindow` 越界先 `ClampLocationToWorkingArea` 钳制再判；新增 `_renderDirty`/`_lastRenderRetryTick` + `RenderRetryIntervalMs=1000`，全部绘制调用经 `MarkRenderDirtyOnFailure` 置位，`UpdateActivationState` 每 tick 最多 1/s 重试。verify：`dotnet build -c Release --no-restore` 0 警告 0 错误。

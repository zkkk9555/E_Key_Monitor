# 04: Custom fallback consistent

**Type:** task
**What to build:** 自定义坐标失效回退 `MiddleLeft`，与默认位置一致。
**Anchors:** `MainForm.cs :: GetCustomOrFallbackLocation`
**Blocked by:** None
**Status:** resolved

- [x] `GetCustomOrFallbackLocation` 回退 `MiddleLeft`（现为 `MiddleRight`）
- [x] 其余放置逻辑不动

## Answer

实现：`GetCustomOrFallbackLocation` 回退 `MiddleRight`→`MiddleLeft`，与默认位置一致。verify：`dotnet build -c Release --no-restore` 0 警告 0 错误（与 05 同批构建）。

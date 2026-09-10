## Destination

E_Key_Monitor 在真实游玩场景下敢用：启动不闪退、无僵尸进程、独立模式不强绑游戏目录、位置行为一致。

## Notes

- 源码：`MainForm.cs`（1877 行，Hook/绘制/跟随/菜单/快捷方式全集中）+ `Program.cs`（27 行）。
- 现场：工作树干净（`M AGENTS.md` + `?? docs/agents/` 为上轮 setup 落盘，未提交）；本机有 .NET runtime、无 SDK（`dotnet --list-sdks` 空）；游戏双 EXE 均在 `E:\Youxi\网吧极速版\program`。
- Gate 0：`Watchdog|跳过|Contains(windowBounds)` 在源码零命中（上条命令实测），属 MISS，无可 hardening 的现成机制。
- Shield：无 publish/发版/credential/花钱面，纯本地行为修正，不 STOP。

## Decisions so far

- [01-render-containment-self-heal](issues/01-render-containment-self-heal.md): 越界窗口钳制后继续绘制，不再首次绘制失败即退出。
- [02-standalone-skip-game-dir](issues/02-standalone-skip-game-dir.md): 纯独立模式允许跳过游戏目录选择，跟随/联合模式仍强制。
- [03-shutdown-watchdog](issues/03-shutdown-watchdog.md): 优雅关闭超时后强制退出，杜绝僵尸进程。
- [04-custom-fallback-consistent](issues/04-custom-fallback-consistent.md): 自定义坐标失效回退 `MiddleLeft`，与默认一致。

## Not yet specified

- 全屏独占下圆点不可见属系统限制，仅文档提示，不做代码改动。
- Hook 存活自检本轮不做（无证据表明钩子会静默死亡）。

## Out of scope

- 改技术栈、触安全边界（输入模拟/内存/反作弊）、`dotnet run` 下快捷方式回退（纯开发态）。
- 发布/对外分发动作。

## Review (code-review 双轴，fixed point db40320，2026-09-10)

### Standards（文档标准 + Fowler 气味基线）

- 结论：无 hard violation，1 处已修，其余为 judgement call 不修。
- 已修：`MainForm.cs:243` 曾出现 `private const int VkE = 0x45;    private const int WmKeyDown` 同行合并（编辑副作用），已拆回两行，重构建 0 警告 0 错误（commit 2c549c1）。
- judgement call（不修）：`MarkRenderDirtyOnFailure` 在 SizeChanged/SetDotSize/SetDockPosition/UpdateActivationState 四处重复调用形状 → Duplicated Code 气味，但每次调用点语义不同（状态切换/设置变更/尺寸变化），抽取反而掩盖调用原因，保留；`TryParseObserveKey` 返回 `(bool, vk, name)` 三元组 → Data Clumps 气味，但仅此一处生产两处消费，不值得新建类型；`RequestShutdown` 内联 watchdog lambda → Middle Man 反方向（内联优于多一层方法），保留。
- 工具已覆盖：命名/缩进/CRLF 由 `dotnet build` 守门（0 警告）。

### Spec（5 票逐项对账）

- 01：越界钳制后继续绘制 ✓；`_renderDirty` + 1/s 重试 ✓；节流（重试即日志节流，持续失败 ≈1 条/秒）✓；初次兜底退出保留 ✓。无 scope creep。
- 02：纯独立模式 Yes/No/Cancel ✓；跟随/联合仍强制 ✓；跳过后托盘可后补（`SelectGameDirectory` 未动）✓。
- 03：8s 一次性看门狗 ✓；`CleanupResources` 首行 `Interlocked.Exchange` 解除 ✓；正常路径无新增弹窗 ✓。
- 04：回退 `MiddleRight`→`MiddleLeft` ✓，其余放置逻辑未动 ✓。
- 05：纯函数解析 A-Z/0-9 不分大小写 ✓；非法回退 E ✓；Hook 比 `_observeVkCode` ✓；托盘标题显示当前键 ✓；安全边界不变（只观察不断言输入）✓；README/AGENTS 同步 ✓（commit 2c549c1）。
- Spec 缺口：零。Scope creep：零。

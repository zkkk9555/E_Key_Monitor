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

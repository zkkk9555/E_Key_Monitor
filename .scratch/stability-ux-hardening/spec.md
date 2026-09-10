# spec: stability-ux-hardening

分支：`stability-ux-hardening`（`NEEDS-HUMAN.md` 指针：无，本轮零 HITL）

## Problem

用户在玩《永劫无间》时不敢用：已知 30s 僵尸进程（AGENTS PARTIAL），越界窗口首次绘制失败即退出，独立模式强绑游戏目录，自定义坐标回退与默认不一致，`config.json` 的 `key` 字段实际不生效。

## Solution

五票：01 绘制包容+自愈，02 独立模式可跳过目录，03 关闭看门狗，04 回退一致，05 可配置观察键。全部纯本地行为修正，安全边界不变（仍只被动观察，不产生/拦截输入）。

## User-Stories

- 启动后圆点始终可见，瞬时失败自愈，不闪退。
- 只想看 E 键时不被强制选游戏目录。
- 游戏退出后监控不残留进程。
- 拖动/预设位置行为一致可预期。
- 想观察别的键时改配置即生效。

## Implementation Decisions

- 01 `MainForm.cs :: RenderLayeredWindow` 越界先钳制；瞬时失败置 `_renderDirty`，由 `UpdateActivationState` tick 1/s 重试。
- 02 `EnsureGameDirectoryConfigured` 纯独立模式弹 Yes/No/Cancel；`_followGame||_launchGame` 强制。
- 03 `RequestShutdown` 启 8s 看门狗，`CleanupResources` 解除，超时强制结束。
- 04 `GetCustomOrFallbackLocation` 回退 `MiddleLeft`。
- 05 纯函数解析 key→VK（A-Z/0-9，不分大小写，非法回退 E），Hook 回调用配置键。

## Testing Decisions

- tdd n/a（WinForms 无测试线；纯逆向/治理类 n/a 既定解释）。门禁 = `dotnet build -c Release` 全绿 + 静态门（8 BAT ASCII / 5 PS1 BOM / config.json 可解析 / 安全 grep 零命中）+ 逻辑走查。
- 冒烟：发布 EXE 独立启动可见圆点；托盘菜单可操作；ASCII/BOM 复查。

## Out of Scope

- 改技术栈、触安全边界、全屏独占可见性、Hook 存活自检、对外发布。

## Glossary

- 包容绘制：越界钳制后继续绘制。
- 自愈：瞬时失败 tick 重试恢复。
- 看门狗：关闭超时强制结束。

## Notes

- AFK 自答：默认位置 `MiddleLeft`（AGENTS/配置一致）；看门狗 8s（宽于 5s 跟随宽限，窄于用户可察觉的残留）；`key` 仅 A-Z/0-9（VK 简单映射，无 IME/修饰键）。Skills called: ask-matt.
- Gate 0: `Watchdog|watchdog|退出看门狗` 零命中 = MISS。Shield: 命中全是 `ReleaseComObject/ReleaseDC`，无外部性。

Checks: i18n N/A（无新增面向用户英文串之外的本地化机制）/ save Pass（`key/position/customX/customY` 经 `SaveSettingsSafely`，证据：票 02/04/05 验收）/ BALANCE N/A（无数值系统）

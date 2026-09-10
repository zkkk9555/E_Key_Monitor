# spec: singlefile-portable

## Problem

根目录 13 个脚本（8 BAT + 5 PS1）用户记不清用途；`dotnet publish` 默认产物是多文件目录（`E_Key_Monitor.dll` + runtimeconfig + 几十个依赖 DLL），用户想要真·单文件绿色便携版：一个 EXE，双击即用。

## Solution

csproj 开 `PublishSingleFile`（win-x64 self-contained），`Package_E_Key_Monitor.ps1` 改为单文件契约校验（断言目录里只有 1 个 EXE 且 >1MB，拒绝多文件形态），根目录脚本用途写入中文说明书附录，正文入口只剩“解压→双击 EXE”。

## User-Stories

- 用户解压 ZIP 后只看到一个 EXE（+config+说明），双击即运行。
- 用户不再需要点任何 BAT/PS1。
- 根目录脚本还在（维护用），但文档明确说日常不用。

## Implementation Decisions

- `E_Key_Monitor.csproj :: PropertyGroup` 加 `PublishSingleFile=true` + `IncludeNativeLibrariesForSelfExtract=true` + `PublishTrimmed=false` + 自包含 win-x64（与打包脚本已有参数对齐，落盘为默认行为）。
- `Package_E_Key_Monitor.ps1 :: publishArguments` 减掉重复参数（csproj 已带），单文件契约：staging 必须恰好 `E_Key_Monitor.exe` + `config.json` 两个文件（config 由 `PreserveNewest` 复制），EXE >1MB。
- BAT/PS1 零改动（维护入口保留），用途表进中文说明书附录。

## Testing Decisions

- tdd n/a（发布形态任务，无可吞产品 seam；门禁 = publish 全绿 + 单文件契约 + 独立启动冒烟 + 静态门）。
- Slices（spec 内嵌 checklist）：
  - [x] slice1 单文件发布：csproj 开关 + publish 全绿 + staging 契约（仅 EXE+config）+ 便携包 3 文件。
  - [x] slice2 独立冒烟：新目录解压（或复制 3 文件到空目录）→ 双击 EXE → 圆点可见 → 托盘菜单可用 → 退出无残留。
  - [x] slice3 文档同步：中文说明书附录 13 脚本用途表 + README/AGENTS 入口说明更新 + 静态门复查。

## Out of Scope

- 删 BAT/PS1（维护用保留）；改业务代码；对外发布。

## Glossary

- 单文件发布：`PublishSingleFile` 自解压形态，磁盘上仍可能有 config 并列，但业务 EXE 只有一个。
- 便携包：`E_Key_Monitor.exe` + `config.json` + 中文说明书。

## Notes

- AFK 自答：`PublishTrimmed=false`（WinForms 反射多，裁剪风险高于体积收益）；config 不内嵌（用户要能改键/目录）。Skills called: ask-matt.
- Gate 0: `PublishSingleFile` 在 csproj 零命中 = MISS（打包脚本传参有但非默认，新机制）。Shield: `credential|spend|upload|submit|上架|支付` 在 ps1/bat 零命中，无外部性。

Checks: i18n N/A（无新本地化串）/ save Pass（config 默认值契约由打包脚本断言）/ BALANCE N/A（无数值系统）

# 便携包用单文件 self-contained 发布

Status: accepted

发布产物是**一个** self-contained 的 `E_Key_Monitor.exe`（`PublishSingleFile` + `IncludeNativeLibrariesForSelfExtract`，显式 `PublishTrimmed=false`），随包再放 `config.json` 和中文说明书。

用户拿到的是「双击就能跑」的文件夹，拷到任何一台装了《永劫无间》的电脑都不需要再装 .NET Runtime，也不需要记一串运行库文件。

## Considered Options

- 依赖框架的普通发布（几十个 DLL）——被否：用户要手动保持整个文件夹完整，少拷一个文件就起不来。
- 开 `PublishTrimmed` 减小体积——被否：WinForms 反射裁剪容易在运行时炸；体积不是这个工具的瓶颈。
- 保留 PDB 一起发——被否：只对维护者有用。打包脚本用 `-p:DebugType=None -p:DebugSymbols=false` 去掉它，并断言发布目录只有 EXE 和 `config.json` 两件。

## Consequences

- 便携 EXE 约 154 MB，这是「目标机器不用装 .NET Runtime」换来的体积。
- 裸 `dotnet publish` 会在 EXE 旁边多产出一个 `.pdb`；只有打包脚本会去掉它，用户看到的目录永远是三个文件。
- 改发布形态要动 csproj 的三个开关，`tools/verify-conventions.ps1` 的 `portable-contract` 会在改动后立刻报红。

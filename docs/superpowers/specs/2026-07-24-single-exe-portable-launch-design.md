# 单 EXE 便携启动与双快捷方式设计

> **状态（2026-10-22 核对）：** 本设计**已经落地**（`PublishSingleFile=true`、打包脚本产出三件套便携包、EXE 托盘菜单生成两个指向 EXE 的桌面快捷方式）。保留此文件作为历史设计记录；当前事实以仓库根目录 `AGENTS.md` 和 `docs/adr/0002-single-file-self-contained-portable.md` 为准。

## 目标

将日常使用收敛为一个可复制、可压缩、可解压运行的文件夹。用户只需要使用两个桌面快捷方式：单独启动监控，或启动 `StartGame.exe` 与监控工具。最终便携包不暴露 BAT、PowerShell、开发脚本或旧快捷方式生成脚本。

## 用户入口

便携包只包含一个业务 EXE：

```text
E_Key_Monitor.exe
```

程序通过命令行参数区分两个运行模式：

- 无参数：只启动监控工具。
- `--launch-game`：先启动监控工具的游戏跟随模式，再启动配置目录中的原始 `StartGame.exe`。

程序运行后通过托盘菜单“生成桌面快捷方式”创建两个快捷方式：

```text
E键监控工具.lnk
启动StartGame和监控.lnk
```

两个快捷方式都直接指向当前的 `E_Key_Monitor.exe`，图标使用 EXE 自身的图标。联合快捷方式只额外传入 `--launch-game`，不指向 BAT 或 PowerShell，也不直接启动 `NarakaBladepoint.exe`。

## 配置和首次启动

默认 `config.json` 的 `gameDirectory` 为 `null`。普通模式和联合模式都由 `MainForm` 在启动时检查目录：

1. 配置为空、目录不存在或缺少两个游戏 EXE 时，显示中文目录选择框。
2. 用户选择同时包含 `StartGame.exe` 和 `NarakaBladepoint.exe` 的目录。
3. 验证成功后，将规范化绝对路径保存到 EXE 旁边的 `config.json`。
4. 后续启动如果目录仍然有效，不再弹出选择框；目录失效时重新选择。

联合模式启动 `StartGame.exe` 时，工作目录设置为已验证的游戏目录。监控工具继续只观察 `NarakaBladepoint` 进程并在游戏退出后按既有宽限逻辑关闭。

## 中文界面

将托盘和圆点菜单中的 `Set game directory...` 改为“设置游戏目录...”，同时将目录选择说明、路径校验错误、保存失败提示、窗口标题和托盘提示改为中文。保留 `StartGame.exe`、`NarakaBladepoint.exe` 和 `NarakaBladepoint` 这些真实文件名及进程名。

## 便携包内容

`Package_E_Key_Monitor.ps1` 继续在源码目录外生成便携目录和 ZIP，但便携目录内容调整为：

```text
E_Key_Monitor-portable/
├── E_Key_Monitor.exe
├── config.json
└── E键监控工具中文使用说明书.md
```

压缩包可以解压到任意可写目录。EXE 旁边的 `config.json` 保存路径和其他用户设置；不使用注册表、不依赖源码目录、不依赖单独安装的 .NET Runtime。桌面快捷方式保存当前解压路径，因此移动整个便携文件夹后需要从托盘重新生成快捷方式。

## 错误处理和安全边界

- 配置读取失败时沿用现有安全默认值，并在启动时要求选择有效游戏目录。
- `StartGame.exe` 不存在或无法启动时显示中文错误并退出，不尝试启动游戏本体。
- 快捷方式创建失败时显示中文错误，不影响监控进程清理。
- 不模拟、拦截或修改输入，不读取游戏内存，不注入进程，不修改网络，不绕过反作弊。

## 验收标准

- C# Release 构建无警告、无错误。
- 无参数启动仍是独立监控模式。
- `--launch-game` 启动已验证目录中的原始 `StartGame.exe`，并启用游戏跟随。
- 托盘菜单显示中文“设置游戏目录...”，目录选择和错误提示为中文。
- 默认配置的 `gameDirectory` 为 `null`；首次选择后保存，路径有效时不重复选择，路径失效时重新选择。
- 便携包只包含一个 EXE、配置文件和中文说明书；不包含 BAT 或 PowerShell 文件。
- 两个快捷方式直接指向 EXE，均使用 EXE 图标，联合快捷方式只增加 `--launch-game` 参数。

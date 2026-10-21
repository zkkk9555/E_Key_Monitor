# AGENTS.md

## 先读这里

本文件描述 `E_Key_Monitor` 当前真实实现，是后续 AI 阅读、诊断和修改项目时的项目级事实来源。

- 唯一有效的源码目录：`C:\Project\E_Key_Monitor`（以本机实际路径为准；换机器后先确认真实位置再改文档）。
- 不要把其他位置的 `E_Key_Monitor` 副本当作源码。
- 当前项目目录是可用的 Git 工作树；当前分支和提交信息必须以实际 `git status`、分支和日志命令为准，**不要把提交号写进本文件**。不要覆盖或清理现有未提交修改。
- `bin/`、`obj/`、`.appdata/`、`.dotnet-home/`、`.nuget/`、`.agents/` 和 `.codex/` 是构建或工具生成内容，不是源代码。
- `docs/superpowers/` 是**历史存档**：里面的路径和结论属于当时，不代表现在的事实，改动前先核对当前代码。
- 本文件写的是约定，不是环境快照。环境快照集中在「当前核查状态」一节，带日期，可复算。

automatt: on

## 项目用途

`E_Key_Monitor` 是一个 Windows 桌面悬浮提示工具，用于显示全局 `E` 键最近是否被触发。

用户玩《永劫无间》时，会使用鼠标宏高频触发 `E` 键拾取物品。如果进入战斗后忘记关闭宏，持续的 `E` 输入会影响正常操作。这个工具通过屏幕上的小圆点直观提示宏是否还在触发 `E`：

- 未触发：偏白色圆点。
- 最近触发过：高亮青绿色圆点。
- 高频持续触发：青绿色持续亮起，不按每次按键闪烁。
- 停止触发约 `200ms`：恢复偏白色。

工具只负责观察和提示，不负责产生、拦截或修改输入。

## 安全边界

项目允许：

- 使用 `WH_KEYBOARD_LL` 全局键盘 Hook 被动观察按键。
- 根据最近一次触发时间更新悬浮圆点。
- 判断指定进程名是否存在，以便随游戏退出。
- 启动用户原有的游戏启动器 `StartGame.exe`。

项目禁止：

- 模拟或发送键盘、鼠标输入。
- 拦截、屏蔽或修改被观察的按键。
- 安装全局鼠标 Hook。
- 读取游戏内存、注入游戏进程或修改游戏文件。
- 修改网络封包或绕过反作弊。
- 直接控制、终止或自动化操作 `NarakaBladepoint.exe`。
- 使用某一次运行产生的固定 PID。

这些边界里，**能被静态检查兜底**的部分由 `tools/verify-conventions.ps1` 的 `safety-boundary` 覆盖：输入模拟 API、鼠标 Hook、进程注入、固定 PID（`GetProcessById`）、终止进程（`Kill`/`TerminateProcess`）、网络客户端、拼出游戏本体路径、根脚本硬编码绝对路径。**静态检查覆盖不到的**（拦截/屏蔽输入的行为语义、封包与反作弊）只能靠改代码时的自觉，不要以为有门在管。

## 技术栈

- C#
- .NET 8（`net8.0-windows`）
- WinForms
- Windows x64
- 不使用第三方 NuGet 包

不要改成 WPF、Avalonia、Electron、Python 或其他技术栈。保持项目小型、直接、适合阅读。

## 当前文件职责

```text
E_Key_Monitor/
├── .gitignore                            # 构建产物、本地状态和敏感文件排除规则
├── .scratch/                             # 工作流留痕：spec / issues / NOTES，随仓库提交
├── AGENTS.md                             # 面向 AI 和维护者的当前项目说明
├── Create_Naraka_Monitor_Shortcut.bat    # 维护者入口：创建指向中文 BAT 的两个桌面快捷方式
├── Create_Naraka_Monitor_Shortcut.ps1    # 维护者入口：创建指向中文 BAT 的两个桌面快捷方式
├── docs/adr/                             # 决策记录：一条难回退的决策一个文件
├── docs/agents/                          # agent 侧约定：领域、issue tracker、triage 标签
├── docs/superpowers/                     # 历史计划与设计记录（只读存档）
├── E_Key_Monitor-icon.png                # 图标设计预览，不参与运行
├── E_Key_Monitor.csproj                  # .NET 8 WinForms 项目定义（含单文件发布开关）
├── E_Key_Monitor.ico                     # EXE、托盘和独立快捷方式的多尺寸图标
├── E键监控工具中文使用说明书.md           # 面向便携包使用者的中文说明书
├── MainForm.cs                           # 配置、Hook、绘制、托盘、拖动、联合启动、快捷方式、游戏跟随和清理
├── Package_E_Key_Monitor.ps1             # 生成便携版单文件包
├── Program.cs                            # 解析 --follow-game / --launch-game 并启动 MainForm
├── README.md                             # 面向用户的使用说明
├── Start_E_Key_Monitor.ps1               # 独立启动正式监控工具
├── Start_Naraka_With_Monitor.bat         # 正式启动的 ASCII 包装器
├── Start_Naraka_With_Monitor.ps1         # 正式联合启动逻辑
├── Start_Naraka_With_Monitor_Dev.bat     # 开发启动的 ASCII 包装器
├── Start_Naraka_With_Monitor_Dev.ps1     # dotnet run 联合启动逻辑
├── config.json                           # 默认配置模板，构建时复制到输出目录
├── tools/verify-conventions.ps1          # 约定校验器：一条命令核对本文件写的约定
├── 制作便携版监控工具.bat                 # 中文便携版打包入口
├── 启动E键监控工具.bat                   # 中文独立监控入口，调用正式 PS1
├── 启动永劫无间E键提示.bat                # 中文正式联合启动入口
├── 开发启动永劫无间E键提示.bat            # 中文开发联合启动入口
└── 生成中文桌面入口.bat                   # 中文桌面快捷方式生成入口
```

`.lnk` 是生成产物，包含绝对路径，不能作为可移植源码或路径事实。源码目录没有 `.lnk` 文件；项目移动后应重新生成快捷方式。

最终便携包只包含：

```text
E_Key_Monitor.exe
config.json
E键监控工具中文使用说明书.md
```

根目录的 BAT、PS1 和 `docs/`、`tools/`、`README.md`、`AGENTS.md` 是源码维护文件，不进入最终便携包，也不是普通用户的日常入口。

## 应用图标

项目使用自定义多尺寸 `E_Key_Monitor.ico`，设计为深色圆角底、偏白色 `E`、高亮青绿色描边与状态点。图标必须在 16px 托盘尺寸下仍可辨识。

- `E_Key_Monitor.csproj` 通过 `ApplicationIcon` 将 ICO 嵌入 Windows EXE。
- `MainForm` 从当前可执行文件提取图标供 `NotifyIcon` 使用，失败时才回退到系统默认图标。
- 提取出的 `Icon` 由 `CleanupResources` 释放。
- `E键监控工具` 快捷方式使用监控 EXE 的新图标。
- `启动StartGame和监控` 联合快捷方式使用当前监控 EXE 图标，便于保证便携包移动后仍可直接使用。
- `E_Key_Monitor-icon.png` 仅作为文档和设计预览，不参与运行。

## 程序入口和运行模式

`Program.cs` 读取命令行参数：

- 不带参数：普通独立模式。
- 带 `--follow-game`：游戏跟随模式。
- 带 `--launch-game`：游戏跟随模式，并在目录验证后启动原始 `StartGame.exe`。

所有模式都会创建同一个 `MainForm`。三种模式的差别只在**是否要求游戏目录**：

- 独立模式（无参数）：目录**可选**。没有有效目录时弹出「设置游戏目录...」，取消后还会追问一次：选「是」跳过并只跑独立监控，选「否」重新选目录，选「取消」退出程序。
- `--follow-game`：**必须**有有效目录，否则取消即退出。
- `--launch-game`：**必须**有有效目录，否则取消即退出。

独立模式不观察游戏进程，也不会因游戏退出自动关闭，需要用户从托盘菜单退出。目录有效后，即使游戏进程不存在也可以运行。

## 全局键盘监听

当前实现位于 `MainForm.cs`：

- Hook 类型：`SetWindowsHookEx(WH_KEYBOARD_LL)`，只装键盘 Hook，不装鼠标 Hook。
- 目标虚拟键：来自配置 `key`（单个 A-Z/0-9，不区分大小写），默认 `E`（`0x45`）。
- 观察消息：`WM_KEYDOWN` 和 `WM_SYSKEYDOWN`。
- Hook 回调只记录 `Environment.TickCount64`，保持轻量。
- 无论是否检测到目标键，都必须调用 `CallNextHookEx`。
- `System.Windows.Forms.Timer` 每 `50ms` 检查一次状态。
- 最近触发时间不超过 `activeTime` 时为激活状态，默认 `200ms`。
- 只有状态发生变化时才重新绘制圆点。
- 退出时必须调用 `UnhookWindowsHookEx`。

不要在 Hook 回调中增加磁盘访问、复杂计算、网络访问或 UI 阻塞操作。

## 悬浮窗和绘制

窗口是无边框、置顶、不显示任务栏按钮、不激活输入焦点的 WinForms 工具窗口。

当前实现不是普通 `OnPaint` 绘制，而是：

- 使用 `WS_EX_LAYERED`、`WS_EX_NOACTIVATE` 和 `WS_EX_TOOLWINDOW`。
- 使用 32 位预乘 Alpha DIB。
- 使用 `Graphics` 的抗锯齿和高质量像素偏移绘制圆形。
- 使用 `UpdateLayeredWindow` 提交逐像素透明窗口。
- 正常背景完全透明，只显示圆点。
- 绘制前会把目标位置钳制到工作区，**越界不放弃绘制**（包容绘制）。
- 瞬时绘制失败不立刻退出：置脏标记，由 50ms tick 最多每 `1s` 重试一次（自愈）；只有首次可见绘制彻底失败才写入诊断日志并安全退出。
- GDI 的 DC、Bitmap 和选入对象必须按现有顺序释放。

默认视觉配置：

- 窗口：`40 x 40`
- 圆点：中号，`26px`
- 未激活偏白色：`ARGB(255, 235, 235, 235)`
- 激活高亮青绿色：`ARGB(255, 65, 255, 145)`
- 全局透明度：`0.8`
- 默认位置：左中

圆点大小：小 `18px`、中 `26px`、大 `34px`。

## 鼠标穿透、菜单和拖动

当前真实需求是默认启用鼠标穿透，`config.json` 中为：

```json
"clickThrough": true
```

默认状态下，圆点的左键和右键都会穿透到后方窗口，避免影响游戏操作。用户通过系统托盘菜单关闭“鼠标穿透”后：

- 左键按住圆点可以拖动。
- 右键圆点可以打开与托盘相同的设置菜单。
- 拖动后的坐标保存为 `Custom` 位置。
- 重新启用穿透前会取消未完成的拖动。

托盘菜单和圆点菜单需要保持勾选状态同步。位置预设依次为右上、右中、右下、左下、左中、左上。

不要把默认行为改回“不穿透”。如果需要移动圆点，应先从托盘关闭穿透，移动完成后再开启。

## 配置读取和保存

根目录 `config.json` 是构建时复制到输出目录的默认模板。程序实际读取和保存的是：

```text
AppContext.BaseDirectory\config.json
```

也就是当前正在运行的 EXE 或 DLL 旁边的配置，而不一定是项目根目录的文件。开发脚本走 `dotnet run` 时，EXE 在 `bin\Debug\net8.0-windows\`，配置也读写那个目录下的 `config.json`；发布版和便携包则读写各自 EXE 旁边的 `config.json`。

当前配置包含中文游戏路径，根目录模板及构建输出应保持 UTF-8 with BOM，以兼容 Windows PowerShell 5.1 的默认读取行为；程序写回配置时同样带 BOM。

当前字段：

```json
{
  "key": "E",
  "activeTime": 200,
  "windowSize": 40,
  "opacity": 0.8,
  "dotSize": "Medium",
  "position": "MiddleLeft",
  "customX": null,
  "customY": null,
  "clickThrough": true,
  "gameDirectory": null
}
```

说明：

- `key` 为观察键（单个 A-Z/0-9，不区分大小写，默认 `E`），`MonitorSettings.TryParseObserveKey` 解析归一化，非法值回退 `E`；`HookCallback` 按解析后的 VK 比较，托盘标题显示当前键。
- `gameDirectory` 保存经验证的游戏目录绝对路径；根目录模板默认是 `null`，首次启动选择成功后保存，路径失效时重新选择。
- 无配置、JSON 损坏或字段无效时回退到安全默认值。
- 保存配置失败只能记录诊断，不能让程序崩溃。

## 游戏跟随状态机

`--follow-game` 只观察进程名：

```text
NarakaBladepoint
```

准确流程：

1. 监控工具启动后，每约 `1` 秒调用 `Process.GetProcessesByName`。
2. 第一次发现 `NarakaBladepoint` 前，最多等待约 `10` 分钟。
3. 等待超时会弹一个托盘气泡，然后按退出流程关闭。
4. 发现游戏后记录 `_gameSeen = true`。
5. `StartGame.exe` 是否继续存在不影响监控工具。
6. 游戏进程消失后开始计算宽限时间。
7. 连续约 `5` 秒未发现游戏进程后调用 `RequestShutdown`。
8. 每次枚举出的 `Process` 对象都必须 `Dispose`。

不要改成观察 `StartGame`，不要使用固定 PID，也不要直接启动 `NarakaBladepoint.exe`。

## 关闭流程

- `RequestShutdown` 置关闭标志、隐藏托盘图标、启动 `8s` 看门狗定时器，然后 `Close()`。
- `CleanupResources` 幂等：撤销拖动、解除看门狗、停并释放两个 Timer、卸载 Hook、释放 `NotifyIcon`、`Icon`、两个 `ContextMenuStrip`。
- 看门狗到时仍未正常退出就写一条诊断并 `Environment.Exit(1)`，避免残留僵尸进程。

## 联合启动逻辑

用户部署机上的游戏路径（启动脚本不硬编码此路径）：

```text
游戏目录：E:\Youxi\网吧极速版\program
启动器：  E:\Youxi\网吧极速版\program\StartGame.exe
游戏本体：E:\Youxi\网吧极速版\program\NarakaBladepoint.exe
进程名：  NarakaBladepoint
```

这段路径是**部署机示例**，不是仓库事实：克隆到别的机器时它可能不存在。真实目录以运行目录旁 `config.json` 的 `gameDirectory` 为准，选不到就重新选。

正式联合启动流程：

1. 桌面快捷方式直接启动当前 `E_Key_Monitor.exe --launch-game`。
2. EXE 验证旁边的 `config.json`、游戏目录和两个游戏文件。
3. EXE 以游戏目录为工作目录启动原始 `StartGame.exe`。
4. EXE 同时启用 `NarakaBladepoint` 游戏跟随模式。
5. 不直接启动 `NarakaBladepoint.exe`。
6. 游戏结束后的清理由监控工具负责。

Windows 不会因为用户直接双击原始 `StartGame.exe` 而自动启动监控工具。用户应使用程序生成的“启动StartGame和监控”快捷方式。

源码目录还保留以下脚本作为开发和兼容入口；它们不属于最终便携包，也不是普通用户的日常入口：

- 旧中文 BAT 和英文 PS1：保留用于维护，不参与便携包运行。
- `E_Key_Monitor.exe`：最终便携包中的唯一业务 EXE。

## 桌面快捷方式

有**两套**创建路径，名字和目标都不一样，不要混为一谈。

**A. 运行中的 EXE —— 托盘菜单“生成桌面快捷方式”（用户日常入口）**

```text
E键监控工具.lnk          -> 当前 EXE，无参数
启动StartGame和监控.lnk  -> 当前 EXE，参数 --launch-game
```

- 两个都用当前 EXE 的图标，工作目录为 EXE 所在目录。
- 便携文件夹移动后，必须从当前 EXE 的托盘菜单重新生成快捷方式。

**B. 源码目录的 `Create_Naraka_Monitor_Shortcut.ps1`（维护者入口）**

```text
E键监控工具.lnk            -> 启动E键监控工具.bat（中文 BAT 入口）
永劫无间（E键提示）.lnk    -> 启动永劫无间E键提示.bat（中文 BAT 入口）
```

- 这一套指向源码目录里的中文 BAT：`E键监控工具.lnk` 用监控 EXE 的图标，`永劫无间（E键提示）.lnk` 用游戏启动器的图标；第二个快捷方式的名字也和 A 不同。
- **两条路径都会写 `E键监控工具.lnk`**，所以跑过 B 之后，A 那个就被覆盖成指向 BAT 的版本了。
- 日常使用只走 A；B 只是维护入口。改桌面快捷方式前先想清楚改的是哪一套。

## 脚本编码规则

根目录全部 8 个 BAT 必须始终保持纯 ASCII：

- `Start_Naraka_With_Monitor.bat`
- `Start_Naraka_With_Monitor_Dev.bat`
- `Create_Naraka_Monitor_Shortcut.bat`
- `启动E键监控工具.bat`
- `启动永劫无间E键提示.bat`
- `开发启动永劫无间E键提示.bat`
- `生成中文桌面入口.bat`
- `制作便携版监控工具.bat`

BAT 只负责调用同目录 PS1，不属于最终便携包；不要把 BAT 当作用户桌面入口。

仓库内所有 `.ps1`（根目录 5 个 + `tools/` 下的校验器）必须保存为 UTF-8 with BOM，文件头为 `EF BB BF`，以兼容 Windows PowerShell 5.1：

- `Start_E_Key_Monitor.ps1`
- `Start_Naraka_With_Monitor.ps1`
- `Start_Naraka_With_Monitor_Dev.ps1`
- `Create_Naraka_Monitor_Shortcut.ps1`
- `Package_E_Key_Monitor.ps1`
- `tools/verify-conventions.ps1`

根目录入口脚本不得硬编码任何绝对路径（游戏目录、源码目录一律靠 `config.json` 与 `$PSScriptRoot` 解析）。路径存在性检查使用 `Test-Path -LiteralPath`。脚本需要兼容 Windows PowerShell 5.1。

## 构建和发布

常用命令：

```powershell
dotnet build -c Release
dotnet run
dotnet run -- --follow-game
dotnet run -- --launch-game
dotnet publish -c Release -r win-x64 --self-contained true
.\Package_E_Key_Monitor.ps1
```

发布相关事实：

- `E_Key_Monitor.csproj` 默认开启 `PublishSingleFile`、`IncludeNativeLibrariesForSelfExtract`，并显式关闭 `PublishTrimmed`。产物是**单文件** self-contained EXE，目标机器不需要另装 .NET Runtime。
- 裸 `dotnet publish` 会在 EXE 旁边额外产出 `E_Key_Monitor.pdb`；打包脚本额外传 `-p:DebugType=None -p:DebugSymbols=false`，并断言发布目录**只有** `E_Key_Monitor.exe` 和 `config.json` 两件。
- `Package_E_Key_Monitor.ps1` 默认输出到源码目录之外的 `<源码父目录>\artifacts\E_Key_Monitor-portable`，并生成同名 ZIP；输出目录必须叫 `E_Key_Monitor-portable` 且必须在源码树之外，否则脚本拒绝执行。
- 打包脚本还会断言便携包**只有**三件（EXE、`config.json`、中文说明书），并校验 `config.json` 保留便携默认值（`position=MiddleLeft`、`activeTime=200`、`clickThrough=true`、`gameDirectory=null`）。
- `Start_E_Key_Monitor.ps1` 和 `Start_Naraka_With_Monitor.ps1` 先找 publish 目录里的单文件 EXE，找不到就回退到 `bin\Release\net8.0-windows\win-x64\E_Key_Monitor.exe`——那是**多文件**自包含形态（旁边有几十个运行时 DLL），不是便携包形态。`Start_Naraka_With_Monitor_Dev.ps1` 不走这条路：它用 `dotnet run` 跑开发版。
- 本机 `dotnet` 可能不在当前 shell 的 PATH 上；执行前先确认 `dotnet --version` 可用。

常规自动检查可以执行 Release 构建。除非用户明确要求实际联调，否则不要自动启动游戏、BAT、PS1 或桌面快捷方式创建脚本。

不要直接编辑 `bin/` 或 `obj/`。源代码修改后由 `dotnet build` 或 `dotnet publish` 重新生成输出。

## 约定自检

改动本文件、`README.md`、中文说明书、脚本或配置后，先跑这条命令：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify-conventions.ps1
```

退出码 0 = 全部通过；非 0 = 至少一条 `FAIL`，行尾给出证据。它检查：

- 根目录 8 个 BAT 纯 ASCII、仓库内所有 `.ps1` 带 UTF-8 BOM。
- 根目录 `config.json` 字段齐、`gameDirectory` 为 null。
- 本文件「当前文件职责」树与真实仓库双向一致（树上写的都存在，仓里的都被写到）。
- README 的「项目结构」树与本文件的树条目一致（两份树不许各说各话）。
- 三份规范文档没有钉住提交号（提交号一变就过期）。
- 产品文件里没有工作流 skill 的旧名称，且本文件有且只有一行显式凭据。
- 安全边界静态零命中：输入模拟 API、鼠标 Hook、进程注入、固定 PID、终止进程、网络客户端、直启游戏本体、根脚本硬编码绝对路径。
- 单文件发布开关与便携包两件/三件契约未被改掉。
- `docs/adr/` 存在，且 `docs/agents/domain.md` 列出的每条 ADR 都真实存在。

新增约定就往校验器里加一条检查，并同步写进本文件。

## 当前核查状态（2026-10-22）

以下结论都可用同名命令复算；**不写提交号**，当前提交以 `git log -1` 为准。

- VERIFIED：Windows x64，.NET SDK `8.0.426`。
- VERIFIED：`dotnet build -c Release` 退出码 0，0 个警告、0 个错误。
- VERIFIED：`dotnet publish -c Release -r win-x64 --self-contained true` 退出码 0；产物为单文件 EXE（约 154 MB）加 `config.json`，裸 publish 另带 `E_Key_Monitor.pdb`。
- VERIFIED：`dotnet format E_Key_Monitor.csproj --verify-no-changes --no-restore` 退出码 0。
- VERIFIED：8 个根目录 BAT 均为纯 ASCII；仓库内 6 个 `.ps1` 均为 UTF-8 with BOM；`Test-Path` 一律 `-LiteralPath`；根目录 `config.json` 可解析、10 个字段齐、`gameDirectory` 为 null、带 BOM。（复算：`tools\verify-conventions.ps1` 的 `bat-ascii` / `ps1-bom` / `config-fields`）
- VERIFIED：安全边界静态搜索零命中——无输入模拟 API、无鼠标 Hook、无进程注入；`NarakaBladepoint.exe` 只出现在目录校验与提示文案里，不作为启动目标；根目录入口脚本无硬编码绝对路径。
- VERIFIED：便携包契约（发布 2 件、便携包 3 件）由打包脚本自带断言。
- VERIFIED：桌面当前没有本项目生成的 `.lnk`；源码目录没有 `.lnk`。快捷方式只能由运行中的 EXE 生成。（复算：`Get-ChildItem -Path "$env:USERPROFILE\Desktop" -Filter *.lnk` 与 `git ls-files -- '*.lnk'`）
- ENVIRONMENT：本机没有游戏安装（`E:\` 不存在），因此「联合启动逻辑」里的 E 盘路径只是部署机示例；本机无法验证游戏相关流程。
- PARTIAL：项目没有测试工程，`dotnet test` 退出码 0 不代表有自动化测试通过；本轮的门是上面的约定校验器加构建三命令。
- UNKNOWN（未执行，不声称通过）：模拟 `E` 输入、验证圆点颜色切换、实际启动游戏、验证游戏退出后的 5 秒跟随清理、托盘菜单操作、从托盘创建桌面快捷方式、便携包双击运行。

## 修改原则

- 先读现有实现，再做最小范围修改。
- 不因旧提示词重建项目或替换技术栈。
- 不破坏已经可用的圆点绘制、键盘 Hook、鼠标穿透、托盘菜单、拖动、配置和游戏跟随逻辑。
- 资源释放必须保持幂等，Hook、Timer、NotifyIcon、ContextMenuStrip 和 GDI 资源都要正常释放。
- 修改脚本后必须重新检查 BAT 的 ASCII 和 PS1 的 BOM。
- 修改代码后至少执行 `dotnet build -c Release`，并报告真实结果。
- 修改任何规范文档后必须执行 `tools/verify-conventions.ps1`，并把真实输出贴出来。
- 不要声称未实际执行的 GUI、键盘或游戏联动测试已经通过。
- 文档、源码和脚本中的路径说明必须区分「当前源码目录」和「部署机示例」，不要把部署机路径说成当前机器事实。

## Agent skills

本仓库使用 `automatt` 工作流 skill 驱动 AI 协作。

### 启用凭据

本文件里那行独占的 `automatt: on` 就是启用凭据：它让**普通的工程请求**也走自动驱动流程（自己理解、自己拆、自己实现、自己验证）。要关掉自动驱动，把它改成 `automatt: off`。

### 开工前

若 `.scratch/WORKFLOW-ACTIVE.md` 存在 → 先调 `automatt` 再继续；新任务开局拿不准走哪条路时 → 也先调它分流。

### Issue tracker

本地 markdown under `.scratch/`（one directory per effort）。See `docs/agents/issue-tracker.md`。

### Triage labels

Five canonical roles with identical label strings. See `docs/agents/triage-labels.md`。

### Domain docs

Single-context: `AGENTS.md` + `docs/agents/` + `docs/adr/`. See `docs/agents/domain.md`。

### 决策落点

难回退、有真实取舍、且没有上下文会让人困惑的决定，写成 `docs/adr/NNNN-<slug>.md`，一条决策一个文件。正文记「决定了什么、为什么」；可以带一两个实现锚点（API 名、开关名）方便定位，但不要复述实现。

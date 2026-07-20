# E_Key_Monitor

`E_Key_Monitor` 是一个 Windows 置顶悬浮提示工具，用来直观看出全局 `E` 键是否仍在被触发。

使用场景是《永劫无间》中的鼠标宏：宏会高频触发 `E` 拾取物品，但进入战斗后如果忘记关闭，持续输入会影响操作。工具通过一个低干扰的小圆点提醒用户宏是否还在工作。

## 当前项目位置

唯一有效的源码目录：

```text
C:\Projects\E_Key_Monitor
```

其他位置的 `E_Key_Monitor` 副本不是当前源码，尤其不要使用或修改 E 盘保存的项目备份。

E 盘上的以下目录是实际游戏安装位置，不是项目源码：

```text
E:\Youxi\网吧极速版\program
```

## 实际效果

- 没有检测到 `E`：显示偏白色圆点。
- 最近 `200ms` 内触发过 `E`：显示高亮青绿色圆点。
- 鼠标宏连续高频触发：圆点持续保持青绿色，不会疯狂闪烁。
- 宏停止约 `200ms`：圆点恢复偏白色。
- 圆点无边框、置顶、背景逐像素透明，不显示任务栏按钮。
- 默认启用鼠标穿透，不影响游戏中的鼠标点击。
- 支持托盘菜单、大小切换、六个位置预设和自定义拖动位置。
- 支持独立运行和随《永劫无间》退出的游戏跟随模式。
- EXE、托盘和独立监控快捷方式使用专用 E 键状态图标。
- 当前实现启动时会校验 `gameDirectory`；配置缺失或无效时会要求选择同时包含两个游戏 EXE 的目录，取消选择会退出。

当前颜色：

```text
激活高亮青绿色：ARGB(255, 65, 255, 145)
未激活偏白色：ARGB(255, 235, 235, 235)
```

## 应用图标

监控工具使用自定义图标：深色圆角底、偏白色 `E`，配合高亮青绿色描边和状态点。它同时用于正式 EXE、系统托盘和桌面的“E键监控工具”快捷方式。

![E_Key_Monitor 图标](E_Key_Monitor-icon.png)

“永劫无间（E键提示）”联合启动快捷方式继续使用游戏原始 `StartGame.exe` 图标，方便和单独监控入口区分。

## 安全边界

本工具只被动监听键盘事件并显示状态。它不会：

- 发送或模拟键盘、鼠标输入。
- 拦截、屏蔽或修改 `E` 键。
- 安装全局鼠标 Hook。
- 读取游戏内存。
- 注入或修改游戏。
- 修改网络封包。
- 绕过反作弊。
- 直接操作 `NarakaBladepoint.exe`。

## 鼠标穿透和移动圆点

程序默认启用“鼠标穿透”。此时鼠标左键和右键都会穿过圆点，作用到后方游戏窗口。

需要移动圆点时：

1. 在系统托盘找到 `E Key Monitor`。
2. 右键托盘图标，关闭“鼠标穿透”。
3. 左键按住圆点拖动。
4. 移动完成后，从托盘重新开启“鼠标穿透”。

穿透关闭时，右键圆点也可以打开设置菜单。托盘菜单与圆点菜单的状态保持同步。

位置预设：

```text
右上
右中
右下
左下
左中
左上
```

## 普通独立运行

普通模式只运行提示工具，不观察游戏进程，也不会因游戏退出自动关闭；但当前实现仍会在启动时校验 `gameDirectory`。第一次运行或配置无效时会弹出目录选择框，必须选择同时包含 `StartGame.exe` 和 `NarakaBladepoint.exe` 的目录，取消选择会退出。目录配置有效后，即使游戏进程没有运行，提示工具也可以继续运行，需要从托盘菜单手动退出。

```powershell
cd "C:\Projects\E_Key_Monitor"
dotnet run
```

## 游戏跟随开发运行

```powershell
cd "C:\Projects\E_Key_Monitor"
dotnet run -- --follow-game
```

`--follow-game` 模式只观察进程名：

```text
NarakaBladepoint
```

它不会观察 `StartGame.exe`，不会使用固定 PID，也不会启动、终止或修改游戏本体。

行为如下：

1. 启动后等待 `NarakaBladepoint` 出现，最长约 10 分钟。
2. 游戏出现后继续监控进程是否存在。
3. `StartGame.exe` 提前退出不影响工具。
4. 游戏本体退出并连续消失约 5 秒后，工具自动关闭。

## 构建和正式发布

Release 构建：

```powershell
cd "C:\Projects\E_Key_Monitor"
dotnet build -c Release
```

发布 Windows x64 自包含版本：

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

正式发布 EXE：

```text
bin\Release\net8.0-windows\win-x64\publish\E_Key_Monitor.exe
```

这是 self-contained 发布目录，运行目标电脑不需要另外安装 .NET Runtime，但必须保留 publish 目录中的配套文件，不能只随意抽走一个 EXE。

### 便携版打包

在项目目录运行：

```powershell
.\Package_E_Key_Monitor.ps1
```

脚本会在项目目录外生成 `..\artifacts\E_Key_Monitor-portable` 和旁边的 `E_Key_Monitor-portable.zip`。便携包是 Windows x64 自包含单文件 `E_Key_Monitor.exe` 加上随附且可写的配置文件 `config.json`，不依赖源码目录或单独安装 .NET Runtime。首次启动时选择并验证同时包含 `StartGame.exe` 和 `NarakaBladepoint.exe` 的游戏目录；移动到另一台电脑后，如果原绝对路径不可用，需要重新选择该目录。

## 一键启动游戏和监控工具

当前验证环境的游戏路径（启动脚本不硬编码此路径）：

```text
游戏目录：E:\Youxi\网吧极速版\program
启动器：  E:\Youxi\网吧极速版\program\StartGame.exe
游戏本体：E:\Youxi\网吧极速版\program\NarakaBladepoint.exe
进程名：  NarakaBladepoint
```

根目录 `config.json` 的 `gameDirectory` 当前默认是 `null`。正式联合启动前，必须让正在使用的监控 EXE 旁边的 `config.json` 保存一个有效游戏目录；可以启动监控工具后从菜单选择“Set game directory...”，程序会验证该目录中同时存在 `StartGame.exe` 和 `NarakaBladepoint.exe`。

正式联合启动入口：

```text
Start_Naraka_With_Monitor.bat
```

BAT 只负责调用同目录的 `Start_Naraka_With_Monitor.ps1`。PS1 的实际流程是：

1. 优先找到 publish 目录中的 `E_Key_Monitor.exe`；不存在时回退到正式 win-x64 运行目录中的 EXE。
2. 读取并验证监控 EXE 旁边的 `config.json` 中的 `gameDirectory` 和两个游戏文件。
3. 启动 `E_Key_Monitor.exe --follow-game`。
4. 启动原始 `StartGame.exe`。
5. 由 `StartGame.exe` 正常启动 `NarakaBladepoint.exe`。
6. 游戏退出约 5 秒后，监控工具自动退出。

脚本完全不直接启动 `NarakaBladepoint.exe`。原始游戏启动流程没有被替换或修改。

直接双击原始 `StartGame.exe` 不会自动运行监控工具，因为 Windows 不知道项目中的监控程序。需要使用联合启动 BAT 或下文生成的桌面快捷方式。

## 生成两个桌面快捷方式

当前源码目录内没有 `.lnk` 文件。当前用户桌面虽然存在两个快捷方式，但本次核查确认它们仍指向旧源码路径：

```text
C:\Users\24310\Projects\E_Key_Monitor
```

这些快捷方式不能作为当前源码目录的可靠入口使用。应通过项目脚本为当前用户重新生成；本次核查没有运行快捷方式创建脚本。

第一次设置或项目移动后，请先发布、配置游戏目录，再重新生成快捷方式：

```powershell
cd "C:\Projects\E_Key_Monitor"
dotnet publish -c Release -r win-x64 --self-contained true
.\Start_E_Key_Monitor.ps1
.\Create_Naraka_Monitor_Shortcut.bat
```

运行 `Start_E_Key_Monitor.ps1` 后，在监控工具菜单中选择游戏目录并退出；快捷方式创建脚本读取的是 publish 目录旁的 `config.json`，如果其中 `gameDirectory` 仍为空会创建失败。

桌面将同时生成：

```text
E键监控工具
永劫无间（E键提示）
```

- `E键监控工具`：通过 `Start_E_Key_Monitor.ps1` 只启动正式监控软件，不启动游戏。关闭时从托盘选择“退出”。
- `永劫无间（E键提示）`：一键启动监控软件和原始 `StartGame.exe`，并在游戏本体退出约 5 秒后自动关闭监控工具。

以后需要单独查看 `E` 键状态时，双击“E键监控工具”；需要玩游戏时，双击“永劫无间（E键提示）”。

不要手工修改旧 `.lnk`，也不要把项目目录中的旧快捷方式复制到桌面；项目路径变化后重新运行创建脚本即可。

## 配置文件

根目录 [config.json](config.json) 是默认配置模板：

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

程序实际读取和保存的是当前运行目录中、位于 EXE 或 DLL 旁边的 `config.json`。例如正式版会使用 publish 目录里的配置。

字段说明：

| 字段 | 含义 |
| --- | --- |
| `key` | 预留字段；当前代码固定监听 `E` |
| `activeTime` | 触发后保持高亮青绿色的毫秒数 |
| `windowSize` | 悬浮窗口尺寸 |
| `opacity` | 圆点整体透明度，范围 0.1 到 1.0 |
| `dotSize` | `Small`、`Medium` 或 `Large` |
| `position` | 位置预设或 `Custom` |
| `customX` / `customY` | 拖动后保存的自定义屏幕坐标 |
| `clickThrough` | 是否启用鼠标穿透，默认 `true` |
| `gameDirectory` | 经验证的游戏目录绝对路径；根目录模板默认 `null`，启动时会要求配置 |

配置不存在、格式错误或字段无效时，程序会回退到安全默认值，不会因此崩溃。

## 项目结构

```text
E_Key_Monitor/
├── E_Key_Monitor.csproj
├── E_Key_Monitor.ico
├── E_Key_Monitor-icon.png
├── Program.cs
├── MainForm.cs
├── config.json
├── Package_E_Key_Monitor.ps1
├── Start_E_Key_Monitor.ps1
├── Start_Naraka_With_Monitor.bat
├── Start_Naraka_With_Monitor.ps1
├── Start_Naraka_With_Monitor_Dev.bat
├── Start_Naraka_With_Monitor_Dev.ps1
├── Create_Naraka_Monitor_Shortcut.bat
├── Create_Naraka_Monitor_Shortcut.ps1
├── README.md
└── AGENTS.md
```

主要逻辑集中在 `MainForm.cs`：

- 全局键盘 Hook。
- 200ms 激活状态计算。
- 分层透明圆点绘制。
- 鼠标穿透和拖动。
- 托盘与菜单。
- 配置保存和游戏目录选择。
- 游戏进程跟随。
- Hook、Timer、GDI 和托盘资源清理。

## 脚本编码要求

以下 BAT 必须保持纯 ASCII：

```text
Start_Naraka_With_Monitor.bat
Start_Naraka_With_Monitor_Dev.bat
Create_Naraka_Monitor_Shortcut.bat
```

当前根目录共有 5 个 PS1。以下启动和快捷方式脚本包含中文路径或中文错误信息，必须保持 UTF-8 with BOM，以兼容 Windows PowerShell 5.1；打包脚本当前也保持相同编码：

```text
Start_E_Key_Monitor.ps1
Start_Naraka_With_Monitor.ps1
Start_Naraka_With_Monitor_Dev.ps1
Create_Naraka_Monitor_Shortcut.ps1
Package_E_Key_Monitor.ps1
```

游戏路径不在启动脚本中硬编码；路径变化时应在正在使用的 `config.json` 中重新选择或填写 `gameDirectory`，不要把中文路径放进 BAT。

## 常见问题

### 游戏里看不到圆点

- 将游戏改为无边框窗口模式。
- 如果游戏以管理员权限运行，监控工具或联合启动入口也可能需要以管理员身份运行。
- 检查托盘是否已有 `E Key Monitor` 图标。
- 检查运行目录中的 `E_Key_Monitor.log`，或 `%LOCALAPPDATA%\E_Key_Monitor\E_Key_Monitor.log`。

### 无法移动或右键圆点

默认鼠标穿透正在生效。从托盘菜单关闭“鼠标穿透”后再拖动或右键。

### 联合启动失败

在项目目录手动执行 PS1，以保留错误窗口：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Start_Naraka_With_Monitor.ps1"
```

确认以下文件仍存在：

```text
E:\Youxi\网吧极速版\program\StartGame.exe
E:\Youxi\网吧极速版\program\NarakaBladepoint.exe
bin\Release\net8.0-windows\win-x64\publish\E_Key_Monitor.exe
```

同时确认 publish 目录中的 `config.json` 已设置有效的 `gameDirectory`；脚本不会从根目录模板或旧的 Debug 配置自动推断游戏目录。

## 当前核查状态

截至 2026-07-20，本次只读核查结果如下：

- VERIFIED：Windows x64 环境安装 .NET SDK `8.0.422`。
- VERIFIED：`dotnet build -c Release --no-restore` 退出码为 0，0 个警告、0 个错误。
- VERIFIED：`dotnet format E_Key_Monitor.csproj --verify-no-changes --no-restore` 退出码为 0。
- VERIFIED：5 个根目录 PS1 均为 UTF-8 with BOM，PowerShell 5.1 静态解析无错误；3 个 BAT 均为纯 ASCII；根目录 `config.json` 可解析；安全边界静态搜索未发现输入注入、鼠标 Hook、进程注入或直接启动/终止游戏的命中。
- PARTIAL：`dotnet test -c Release --no-restore --verbosity normal` 退出码为 0，但项目没有测试项目或测试用例，因此不能据此声称有自动化测试通过。
- VERIFIED：当前验证环境中的 `StartGame.exe`、`NarakaBladepoint.exe` 均存在，`NarakaBladepoint` 进程核查时未运行。
- PARTIAL：已配置运行目录中的监控 EXE 能启动并保持响应，`--follow-game` 在没有游戏进程时能保持等待；通过外部关闭窗口请求后，进程在 30 秒内未退出，本次测试进程需要强制清理。根因未定位，托盘菜单“退出”路径未单独验证。
- STALE：当前桌面两个快捷方式仍指向 `C:\Users\24310\Projects\E_Key_Monitor`，不是本源码目录；项目目录内没有可用 `.lnk` 文件。需要重新运行创建脚本。
- UNKNOWN：本次没有模拟 `E` 键、验证圆点颜色切换、启动游戏、验证游戏退出后的 5 秒跟随清理、执行联合启动脚本、执行快捷方式创建脚本或执行便携打包脚本。

项目没有可用的 Git 版本历史：`.git` 目录存在但 `git status`、分支和提交查询均报告不是 Git 仓库。`bin/`、`obj/` 和本地 .NET 工具目录属于生成内容，不是源代码。

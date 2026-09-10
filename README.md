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

“E键监控工具”和“启动StartGame和监控”两个快捷方式都直接使用监控 EXE 的图标，不依赖 BAT 图标。

## 用户入口

最终便携包只需要一个 `E_Key_Monitor.exe`。程序运行后，从托盘菜单选择“生成桌面快捷方式”，会在桌面创建两个直接指向 EXE 的快捷方式：

| 快捷方式 | 作用 |
| --- | --- |
| `E键监控工具` | 只启动 E 键监控 |
| `启动StartGame和监控` | 启动监控，并启动原始 `StartGame.exe` |

快捷方式使用 EXE 自身图标，不依赖 BAT 或 PowerShell。源码目录中保留的旧脚本只用于开发和兼容，不是便携包入口。完整说明见 [E键监控工具中文使用说明书](E键监控工具中文使用说明书.md)。

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

1. 在系统托盘找到“E键监控工具”。
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

这是 self-contained 单文件发布（`PublishSingleFile`），运行目标电脑不需要另外安装 .NET Runtime。业务 EXE 只有一个：`E_Key_Monitor.exe`，双击即用。

### 便携版打包

在项目目录运行：

```powershell
.\Package_E_Key_Monitor.ps1
```

脚本会在项目目录外生成 `..\artifacts\E_Key_Monitor-portable` 和旁边的 `E_Key_Monitor-portable.zip`。便携包只包含 Windows x64 自包含的 `E_Key_Monitor.exe`、可写的 `config.json` 和中文说明书，不依赖源码目录或单独安装 .NET Runtime。解压后直接启动 EXE；第一次启动选择游戏目录，之后沿用保存的目录，目录失效时才重新选择。

## 两种 EXE 运行模式

当前验证环境的游戏路径（启动脚本不硬编码此路径）：

```text
游戏目录：E:\Youxi\网吧极速版\program
启动器：  E:\Youxi\网吧极速版\program\StartGame.exe
游戏本体：E:\Youxi\网吧极速版\program\NarakaBladepoint.exe
进程名：  NarakaBladepoint
```

根目录 `config.json` 的 `gameDirectory` 默认是 `null`。第一次启动时，程序会显示“设置游戏目录...”选择框；选择成功后保存到 EXE 旁边的 `config.json`。以后目录有效就不再询问，目录不存在或缺少游戏文件时才重新选择。

独立监控模式：

```text
E_Key_Monitor.exe
```

联合启动模式由桌面快捷方式传入参数：

```text
E_Key_Monitor.exe --launch-game
```

这个模式先验证配置目录，再以该目录为工作目录启动原始 `StartGame.exe`，同时启用现有的 `NarakaBladepoint` 进程跟随。游戏退出约 5 秒后，监控工具按既有逻辑退出。

程序不会直接启动 `NarakaBladepoint.exe`，也不会修改原始游戏启动流程。

## 生成桌面快捷方式

当前源码目录内没有 `.lnk` 文件。快捷方式由正在运行的 EXE 直接生成，最终便携包不携带固定的 `.lnk`。

便携文件夹移动后，打开当前文件夹中的 `E_Key_Monitor.exe`，右键托盘图标并选择“生成桌面快捷方式”。

桌面将同时生成：

```text
E键监控工具
启动StartGame和监控
```

- 在任意模式启动 EXE 后，右键托盘图标，选择“生成桌面快捷方式”。
- `E键监控工具` 直接指向当前 `E_Key_Monitor.exe`，不带参数。
- `启动StartGame和监控` 直接指向同一个 EXE，参数为 `--launch-game`。
- 两个快捷方式都使用 EXE 图标；如果移动整个便携文件夹，需要重新生成快捷方式。

以后只需要双击桌面的“E键监控工具”或“启动StartGame和监控”，不需要双击 BAT、PS1 或直接双击原始 `StartGame.exe`。

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

由于游戏目录包含中文，根目录和构建输出中的 `config.json` 保持 UTF-8 with BOM，以兼容 Windows PowerShell 5.1 对配置的读取。不要用不保留 BOM 的工具覆盖这个文件。

字段说明：

| 字段 | 含义 |
| --- | --- |
| `key` | 观察键（单个 A-Z/0-9，不区分大小写，默认 `E`，非法值回退 `E`）；托盘标题显示当前键 |
| `activeTime` | 触发后保持高亮青绿色的毫秒数 |
| `windowSize` | 悬浮窗口尺寸 |
| `opacity` | 圆点整体透明度，范围 0.1 到 1.0 |
| `dotSize` | `Small`、`Medium` 或 `Large` |
| `position` | 位置预设或 `Custom` |
| `customX` / `customY` | 拖动后保存的自定义屏幕坐标 |
| `clickThrough` | 是否启用鼠标穿透，默认 `true` |
| `gameDirectory` | 经验证的游戏目录绝对路径；默认 `null`，首次启动选择后保存，路径失效时重新选择 |

配置不存在、格式错误或字段无效时，程序会回退到安全默认值，不会因此崩溃。

## 项目结构

```text
E_Key_Monitor/
├── .gitignore
├── E_Key_Monitor.csproj
├── E_Key_Monitor.ico
├── E_Key_Monitor-icon.png
├── Program.cs
├── MainForm.cs
├── config.json
├── 启动E键监控工具.bat
├── 启动永劫无间E键提示.bat
├── 开发启动永劫无间E键提示.bat
├── 生成中文桌面入口.bat
├── 制作便携版监控工具.bat
├── Package_E_Key_Monitor.ps1
├── Start_E_Key_Monitor.ps1
├── Start_Naraka_With_Monitor.bat
├── Start_Naraka_With_Monitor.ps1
├── Start_Naraka_With_Monitor_Dev.bat
├── Start_Naraka_With_Monitor_Dev.ps1
├── Create_Naraka_Monitor_Shortcut.bat
├── Create_Naraka_Monitor_Shortcut.ps1
├── E键监控工具中文使用说明书.md
├── docs/superpowers/                  # 计划和设计记录
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
- `--launch-game` 联合启动和桌面快捷方式生成。
- 游戏进程跟随。
- Hook、Timer、GDI 和托盘资源清理。

## 脚本编码要求

以下 BAT 必须保持纯 ASCII（文件名可以是中文，文件内容不能写入中文路径或中文提示）：

```text
Start_Naraka_With_Monitor.bat
Start_Naraka_With_Monitor_Dev.bat
Create_Naraka_Monitor_Shortcut.bat
启动E键监控工具.bat
启动永劫无间E键提示.bat
开发启动永劫无间E键提示.bat
生成中文桌面入口.bat
制作便携版监控工具.bat
```

当前根目录共有 5 个 PS1。以下启动和快捷方式脚本包含中文路径或中文错误信息，必须保持 UTF-8 with BOM，以兼容 Windows PowerShell 5.1；打包脚本当前也保持相同编码：

```text
Start_E_Key_Monitor.ps1
Start_Naraka_With_Monitor.ps1
Start_Naraka_With_Monitor_Dev.ps1
Create_Naraka_Monitor_Shortcut.ps1
Package_E_Key_Monitor.ps1
```

游戏路径不在 BAT 或 PS1 中硬编码；根目录模板默认是 `null`。首次启动选择后保存到 EXE 旁边的 `config.json`，路径变化或失效时重新选择。

## 常见问题

### 游戏里看不到圆点

- 将游戏改为无边框窗口模式。
- 如果游戏以管理员权限运行，监控工具或联合启动入口也可能需要以管理员身份运行。
- 检查托盘是否已有“E键监控工具”图标。
- 检查运行目录中的 `E_Key_Monitor.log`，或 `%LOCALAPPDATA%\E_Key_Monitor\E_Key_Monitor.log`。

### 无法移动或右键圆点

默认鼠标穿透正在生效。从托盘菜单关闭“鼠标穿透”后再拖动或右键。

### 联合启动失败

打开当前便携文件夹中的 `E_Key_Monitor.exe`，右键托盘图标并选择“设置游戏目录...”重新选择。若仍然失败，再检查以下文件：

```text
E:\Youxi\网吧极速版\program\StartGame.exe
E:\Youxi\网吧极速版\program\NarakaBladepoint.exe
bin\Release\net8.0-windows\win-x64\publish\E_Key_Monitor.exe
```

同时确认 `E_Key_Monitor.exe` 和 `config.json` 在便携文件夹中。不要把 EXE 单独从文件夹中拿出来运行。

## 当前核查状态

截至 2026-07-24，本次只读核查结果如下：

- VERIFIED：Windows x64 环境安装 .NET SDK `8.0.422`。
- VERIFIED：`dotnet build -c Release --no-restore` 退出码为 0，0 个警告、0 个错误。
- VERIFIED：`dotnet publish -c Release -r win-x64 --self-contained true --no-restore` 退出码为 0；最终便携目录和 ZIP 已在 `C:\tmp` 生成。
- VERIFIED：`dotnet format E_Key_Monitor.csproj --verify-no-changes --no-restore` 退出码为 0。
- VERIFIED：5 个根目录 PS1 均为 UTF-8 with BOM，PowerShell 5.1 静态解析无错误；8 个 BAT 均为纯 ASCII；根目录 `config.json` 可解析且 `gameDirectory` 默认为空；安全边界静态搜索未发现输入注入、鼠标 Hook、进程注入或直接启动/终止游戏的命中。
- PARTIAL：`dotnet test -c Release --no-restore --verbosity normal` 退出码为 0，但项目没有测试项目或测试用例，因此不能据此声称有自动化测试通过。
- VERIFIED：当前验证环境中的 `StartGame.exe`、`NarakaBladepoint.exe` 均存在，`NarakaBladepoint` 进程核查时未运行。
- PARTIAL：已配置运行目录中的监控 EXE 能启动并保持响应，`--follow-game` 在没有游戏进程时能保持等待；通过外部关闭窗口请求后，进程在 30 秒内未退出，本次测试进程需要强制清理。根因未定位，托盘菜单“退出”路径未单独验证。
- STALE：当前桌面两个快捷方式仍指向 `C:\Users\24310\Projects\E_Key_Monitor`，不是本源码目录；项目目录内没有可用 `.lnk` 文件。需要重新运行创建脚本。
- VERIFIED：最终便携包实际只包含一个 EXE、`config.json` 和中文说明书；ZIP 中没有 BAT 或 PS1。
- UNKNOWN：本次没有模拟 `E` 键、验证圆点颜色切换、实际启动游戏、验证游戏退出后的 5 秒跟随清理、从托盘创建桌面快捷方式或实际双击便携包 EXE。
- VERIFIED：本次修改集中在 `Program.cs`、`MainForm.cs`、配置、打包脚本和文档；没有修改测试、依赖或锁文件。

当前目录是可用的 Git 工作树；核查到当前分支为 `main`，最近提交为 `838e43e Initial commit`。本次保留所有未提交修改，没有执行提交、重置、切换或清理。`bin/`、`obj/` 和本地 .NET 工具目录属于生成内容，不是源代码。

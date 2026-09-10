# E 键监控工具中文使用说明书

更新时间：2026-07-24

## 一、便携文件夹里有什么

最终便携文件夹只需要下面三个文件：

```text
E_Key_Monitor-portable/
├── E_Key_Monitor.exe
├── config.json
└── E键监控工具中文使用说明书.md
```

日常不需要双击 BAT 或 PowerShell。程序本体就是 `E_Key_Monitor.exe`。

## 二、两个桌面入口

启动 `E_Key_Monitor.exe` 后，右键系统托盘中的“E键监控工具”，选择“生成桌面快捷方式”。程序会创建两个直接指向 EXE 的快捷方式：

| 快捷方式 | 作用 |
| --- | --- |
| `E键监控工具.lnk` | 只启动监控工具 |
| `启动StartGame和监控.lnk` | 启动监控工具，并启动原始 `StartGame.exe` |

两个快捷方式都使用 EXE 的正常软件图标，不会显示 BAT 图标。

以后：

- 只想监测 E 键：双击“E键监控工具”。
- 要玩游戏：双击“启动StartGame和监控”。

如果移动了整个便携文件夹，快捷方式中的绝对路径会失效。重新打开 EXE，再选择“生成桌面快捷方式”即可更新。

## 三、第一次启动和游戏目录

默认配置没有游戏路径：

```json
"gameDirectory": null
```

第一次启动任意模式时，程序会显示“设置游戏目录...”窗口。请选择同时包含下面两个文件的目录：

```text
StartGame.exe
NarakaBladepoint.exe
```

在当前电脑上，目录是：

```text
E:\Youxi\网吧极速版\program
```

选择成功后，路径会保存到 `E_Key_Monitor.exe` 旁边的 `config.json`。之后只要路径仍然有效，就不会再次询问；如果目录被移动、删除或缺少游戏文件，下一次启动时会重新要求选择。

真正用于启动游戏的是 `StartGame.exe`。监控工具不会直接启动或控制 `NarakaBladepoint.exe`，只观察它对应的进程是否存在。

## 四、托盘菜单

右键托盘图标可以看到：

- 鼠标穿透：默认开启，避免影响游戏点击。
- 设置游戏目录：重新选择游戏目录。
- 生成桌面快捷方式：创建或覆盖两个桌面快捷方式。
- 切换大小：小、中、大。
- 切换位置：右上、右中、右下、左下、左中、左上。
- 退出：关闭监控工具。

需要移动圆点时，先关闭“鼠标穿透”，拖动完成后再重新开启。

## 五、圆点状态

- 偏白色：最近没有检测到 `E`。
- 青绿色：最近约 `200ms` 内检测到过 `E`。
- 高频连续触发时会持续亮起，不会按每次按键疯狂闪烁。

工具只观察输入，不发送、模拟、拦截或修改键盘鼠标输入。

## 六、两个模式实际做什么

### E键监控工具

快捷方式不带参数，等同于：

```text
E_Key_Monitor.exe
```

它只显示 E 键状态，不会因为游戏退出自动关闭。退出请使用托盘菜单中的“退出”。

### 启动StartGame和监控

快捷方式指向同一个 EXE，但带有参数：

```text
E_Key_Monitor.exe --launch-game
```

程序会先验证游戏目录，再以该目录为工作目录启动原始 `StartGame.exe`，同时进入游戏跟随模式。游戏进程退出并持续消失约 5 秒后，监控工具按现有逻辑退出。

程序不会直接启动 `NarakaBladepoint.exe`。

## 七、配置文件位置

程序真正使用的是 EXE 旁边的配置文件：

```text
E_Key_Monitor.exe
config.json
```

便携文件夹可以复制到其他目录或其他电脑。配置中的游戏目录是绝对路径；如果另一台电脑的游戏位置不同，第一次启动时重新选择即可。

## 八、制作便携包（维护者使用）

源码目录中的 `Package_E_Key_Monitor.ps1` 不是日常用户入口。维护者可以在项目目录执行：

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
.\Package_E_Key_Monitor.ps1
```

脚本会在源码目录外生成便携文件夹和 ZIP。最终包不包含 BAT、PS1、源码、开发文件或旧快捷方式。

## 九、常见问题

### 每次启动都要求选择路径

确认 `config.json` 和 `E_Key_Monitor.exe` 在同一个文件夹，并确认选择的目录中同时存在 `StartGame.exe` 与 `NarakaBladepoint.exe`。不要把 EXE 单独从便携文件夹中拿出来运行。

### 桌面快捷方式打不开

打开当前便携文件夹里的 `E_Key_Monitor.exe`，右键托盘图标，重新选择“生成桌面快捷方式”。移动便携文件夹后必须重新生成。

### 游戏里看不到圆点

确认托盘中有监控图标，并检查游戏和监控工具是否需要使用相同的管理员权限。默认鼠标穿透不会影响游戏点击。

## 十、当前验证边界

项目已完成 Release 构建和静态检查；本次没有把未执行的实际游戏启动、E 键模拟、圆点颜色切换或游戏退出清理验证写成已通过。工具仍然只观察输入和游戏进程，不执行输入模拟、内存注入或反作弊绕过。

## 附录、源码目录脚本一览（维护者使用，日常不用）

日常使用只需要便携包里的 `E_Key_Monitor.exe`。下面 13 个脚本都在源码目录里，解压后的便携包不包含它们：

| 文件 | 用途 |
| --- | --- |
| `启动E键监控工具.bat` | 独立启动正式监控（调 `Start_E_Key_Monitor.ps1`） |
| `启动永劫无间E键提示.bat` | 正式联合启动（调 `Start_Naraka_With_Monitor.ps1`） |
| `开发启动永劫无间E键提示.bat` | 开发联合启动（调 `Start_Naraka_With_Monitor_Dev.ps1`） |
| `生成中文桌面入口.bat` | 创建桌面快捷方式（调 `Create_Naraka_Monitor_Shortcut.ps1`） |
| `制作便携版监控工具.bat` | 打便携包（调 `Package_E_Key_Monitor.ps1`） |
| `Start_Naraka_With_Monitor.bat` | 正式启动的 ASCII 包装器 |
| `Start_Naraka_With_Monitor_Dev.bat` | 开发启动的 ASCII 包装器 |
| `Create_Naraka_Monitor_Shortcut.bat` | 快捷方式创建入口 |
| `Start_E_Key_Monitor.ps1` | 独立启动正式监控逻辑 |
| `Start_Naraka_With_Monitor.ps1` | 正式联合启动逻辑 |
| `Start_Naraka_With_Monitor_Dev.ps1` | `dotnet run` 开发联合启动逻辑 |
| `Create_Naraka_Monitor_Shortcut.ps1` | 创建两个桌面快捷方式 |
| `Package_E_Key_Monitor.ps1` | 生成单文件便携包（含单文件契约校验） |

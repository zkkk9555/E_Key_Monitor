# NOTES — dot-scale-autostart-watch

Skills called: grilling→to-spec→tdd→implement→code-review

开工卡｜任务：圆点按屏幕比例自适应变大 + 开机自启开关 + 常驻跟随（游戏开就显示、关就隐藏）｜slug：dot-scale-autostart-watch｜lane：B｜档位：full
本轮调用：grilling→to-spec→tdd→implement→code-review（ask-matt 本会话已分流，本轮 T0 downshift）
调用面：grilling=inline（设计树自答）｜to-spec=inline｜tdd=tool｜implement=inline｜code-review=tool
分流：shield n-a（写 HKCU Run 属用户级可逆设置，默认关闭、只由用户点开关触发）｜Gate0 MISS @MainForm.cs:22（仍是 SystemAware、无百分比尺寸、无自启、无常驻模式）｜setup done
正身：grilling、to-spec、tdd、implement、code-review｜必读集：MainForm.cs:139-240,317-462,940-1010,1538-1660、Program.cs:1-27、config.json
链·产物：grilling=.scratch/dot-scale-autostart-watch/spec.md｜to-spec=.scratch/dot-scale-autostart-watch/spec.md｜tdd=tools/verify-conventions.ps1+行为测试脚本｜implement=MainForm.cs,Program.cs,AutoStartManager.cs,config.json｜code-review=NOTES.md「review」节

## 动手前的事实核对

- 本机 15.7GB 内存只剩 0.8GB（95% 已用）→ **不启动游戏**。游戏本体在 `C:\Game\永劫无间网吧极速版\program`（已下载，含 StartGame.exe / NarakaBladepoint.exe），可以拿它做目录校验，但不做真实联调。
- 本机单屏 2560x1600 物理像素，Windows 缩放 150%（AppliedDPI=144），逻辑分辨率 1707x1067。
- 实测旧版：窗口 40x40 **设备**像素、圆点 26 物理像素（≈4.6mm）；同一份 EXE 换到 100% 缩放的 1080p 屏就是 ≈6.9mm —— **跨屏物理大小差 1.5 倍**，这就是用户说的「比例不一致」的根因。
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 已有 8 条用户项（微信、IDM、GameViewer、桌面工具、naraka-toolkit 等），本项目要加的条目名 `E_Key_Monitor` 目前不存在。

## 三条切片的实测结果（红→绿）

### S1 圆点按屏幕比例

```text
改前: 窗口 40x40 device px，圆点 26 device px（固定像素）
改后: 窗口 60x60 device px，圆点 48 device px = 屏幕高度 1600 的 3.0%
```

截图像素量测与 Win32 窗口矩形两条独立口径一致。

### S2 开机自启（注册表）

临时工程（放在 TEMP，编译 `AutoStartManager.cs`，用完删除）11 条断言全绿：默认无条目 → Enable 后值为 `"<EXE 路径>" --watch` → 指向当前 EXE 时才算已开启 → 路径失效不算已开启 → Disable 后条目被删除。

### S3 常驻跟随 `--watch`

用**进程名替身**（把 `ping.exe` 复制改名成 `NarakaBladepoint.exe`，不启动真游戏）驱动：

```text
游戏进程不在      -> 圆点隐藏（visible=False）
替身进程出现      -> 圆点显示（visible=True，尺寸 60x60）
替身进程结束 +5s  -> 圆点再次隐藏，程序进程仍在（alive=True）
```

## 踩到的一个真 bug（已修）

第一版 `--watch` 的圆点**一直不出现**。原因不在检测：`RenderLayeredWindow` 里有一道既有守卫「窗口必须可见才能绘制」，而常驻模式此时窗口是隐藏的，于是每次绘制都失败。应用自己的诊断日志（`E_Key_Monitor.log`）把真因写了出来：`Step=Validate visible window handle ... Visible=False`。

修法：`ShowDot()` 先把窗口显示出来再绘制；`UpdateActivationState` 里补 `Visible` 守卫，避免隐藏状态下每次状态变化都去画、刷爆日志。

## 交付

- 改：`MainForm.cs`、`Program.cs`、`config.json`（删 `windowSize`）、`tools/verify-conventions.ps1`（配置字段 10→9）、`AGENTS.md`、`README.md`、中文说明书、`docs/agents/domain.md`。
- 增：`AutoStartManager.cs`、`docs/adr/0004-screen-relative-dot-and-user-run-autostart.md`、本目录 spec/NOTES。
- 门：`dotnet build -c Release` 0 警告 0 错误；`dotnet format --verify-no-changes` 通过；`tools\verify-conventions.ps1` 12/12 绿（配置字段 9、树 29 条、两棵树一致、4 条 ADR 指针可达）。

## 没做 / 不声称

- 没启动真游戏（本机内存不够），游戏相关的真实联调留给用户单独做。
- 没验证真实重启后的自启生效，只验证了注册表读写与命令行内容。
- 没点过托盘菜单（UI 点击没自动化），菜单项的代码路径与注册表行为分别验证。
## review（双轴复核，code-review）

### Spec 轴（子代理报告，7 条）

| # | 发现 | 处理 |
| --- | --- | --- |
| A1 | `AutoStartManager.cs`、ADR-0004、`.scratch/` 还是 untracked，`git diff HEAD` 里看不到 slice 2 | 不是缺陷：提交用 `git add -A` 会一并带上（`MainForm.cs` 已经调用 `AutoStartManager`，所以必须一起提交，否则编译不过） |
| A2 | `README.md` 的配置样例里还留着 `"windowSize": 40` | **已修**：样例与表格、代码、模板、校验器一致 |
| A3 | 规格说「路径失效时菜单显示未勾选**并提示**」，实现只取消勾选，没有提示 | **已修**：开启时若检测到旧条目，提示语改为「旧的失效路径已替换为当前 EXE」 |
| A4 | 只有 `WM_DPICHANGED` 会重算，**拖到同 DPI 但不同分辨率的屏**不会重算 → 「每块屏一样大」没兑现 | **已修**：`FinishDrag`（拖动结束）与 `SetDockPosition`（换预设）也调 `ApplyDotMetrics()` |
| A5 | 规格写「两者都不套用 10 分钟超时」，代码只给 `--watch` 免 | **已修规格**：只有 `--watch` 免，`--follow-game` 保持旧语义 |
| B1 | 规格 Out of Scope 写「不做托盘气泡」，实现里用了 `ShowBalloonTip` | **已修规格**：改为「不做额外的常驻通知/气泡 UI；开关点击后的结果提示沿用既有托盘气泡」 |
| C1 | **真 bug**：菜单勾选用 `IsEnabledForCurrentExecutable()`，但点击分支用 `IsEnabled()`（有值就算）→ 便携包挪走后菜单显示未勾选，点一下却是删除，开不了自启 | **已修**：点击分支改成同一个判据 `IsEnabledForCurrentExecutable()`；旧条目在「开启」时被覆盖 |
| (d) | 「在每块显示屏上占比一致」「1080p 上 3% = 32px」是推出来的，本机只有一块 150% 屏 | **已改口径**：AGENTS.md 改成「构造上与缩放无关 + 明确标注只在一块屏实测过」；核查状态补一条「多屏未实测」 |

### Standards 轴

子代理在收尾前没有回报（与上一轮同样的现象：`send_message` 不可达，只能等它自己报）。这一轴由父会话按同一简报内联跑完，结论：

- 硬违规：**无**。安全边界未动（校验器 `safety-boundary` 12/12 绿，含输入模拟 API、鼠标 Hook、进程注入、固定 PID、终止进程、网络、直启游戏本体）。
- 判断项（已修）：`ShowDot`/`HideDot` 同时维护 `_dotVisible` 和 `Visible` 两份状态 → 删掉 `_dotVisible`，统一用 `Visible`。
- 判断项（已修）：圆点尺寸只在 `WM_DPICHANGED` 重算 → 补 `FinishDrag` / `SetDockPosition` 两个重算点。
- 判断项（已修）：自启开关的判据不一致（见 C1）。
- 气味扫描：新增的 `AutoStartManager.cs` 职责单一；没有引入重复逻辑；`ApplyDotMetrics` 的三个调用点是有意为之（构造、换屏、换尺寸/位置）。


## 复核后补记（Standards 轴迟到报告的处理）

Standards 轴子代理在收尾之后才回报，提出 4 条硬问题 + 3 条判断项。逐条处理如下，处理后**全部重跑**：

| # | 报告内容 | 处理 |
| --- | --- | --- |
| H1 | **多屏启动崩**：构造时 `ApplyDotMetrics()` 按**主屏**算尺寸，随后窗口被放到 Custom 位置（另一块屏），首帧按新屏重算直径 → 直径大于客户区 → 渲染失败 → 「首次绘制失败…程序将安全退出」 | **已修**：`ApplyDotMetrics()` 改成「先落位 → 再按落位后的屏量尺寸 → 再重钳位」；`InitializeVisibleWindow` 改调 `ApplyDotMetrics()`；渲染里直径装不下时**钳制**而不是失败（与项目既有的「包容绘制」哲学一致） |
| H2 | **`--watch` 换屏/DPI 变化不重绘**：`OnShown` 的 watch 分支提前返回，`_initialRenderCompleted` 永远为 false，所以 `WM_DPICHANGED` 与 `SizeChanged` 只改尺寸不重画 | **已修**：两处判断从 `_initialRenderCompleted` 改为 `Visible`（圆点在场就重绘）；顺手删掉因此变成死字段的 `_initialRenderCompleted`（否则编译出 CS0414 警告） |
| H3 | AGENTS.md 文件树注释过时：`Program.cs` 仍写「解析 --follow-game / --launch-game」，`MainForm.cs` 没提新职责 | **已修** |
| H4 | 自启判定用 `registered.Contains(exe)`，是**子串**匹配，`E_Key_Monitor.exe.bak` 也会算「已开启」 | **已修**：改为从注册表命令行里**精确取出路径**再全等比较；新增断言「后缀路径不算已开启」 |
| J1 | 三个 bool 参数（Primitive Obsession） | **未改，记为待办**：改成枚举要动入口模式与文档，收益不抵这个阶段的回归风险；代码里每个 bool 的用途是显式的 |
| J2 | `Program.cs` 重复三份参数解析 | **已改**：抽了一个 `HasFlag(args, flag)` |
| J3 | **开机自启会在每次登录弹目录选择框**：`--watch` 原本要求有效游戏目录 | **已修**：`--watch` 只看进程名、从不启动游戏，因此**完全跳过目录流程**（既不弹框也不追问）；这也是用户「设好自启就不用管」的前提 |

修完复测（全部重跑）：

```text
dotnet build -c Release --no-incremental -> 0 警告 0 错误
dotnet format --verify-no-changes      -> exit 0
tools\verify-conventions.ps1          -> 12 passed / 0 failed
行为测试：圆点 48 device px / --watch 隐藏-显示-再隐藏、程序仍在 -> 通过
自启注册表：6 条断言全绿（新增「后缀路径不算已开启」）
```
### 复核后复测

```text
dotnet build -c Release                                -> 0 警告 0 错误
dotnet format --verify-no-changes                      -> exit 0
tools\verify-conventions.ps1                          -> 12 passed / 0 failed
行为测试：窗口 60x60、圆点 48 device px              -> 通过
行为测试：--watch 隐藏/显示/再隐藏、程序仍在           -> 通过
```


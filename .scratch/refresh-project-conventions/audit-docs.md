# Audit: project documentation

> 审计对象：§C:\Project\E_Key_Monitor§（分支 §main§，工作树干净）
> 审计日期：2026-10-22
> 审计范围：只读。未修改任何项目文件；本文件是唯一新建产物。
> HEAD：§986cf84c0f785fdb853aafa23242bb348d74c0cb§ — "docs: update source path to C:\Project\E_Key_Monitor"（2026-10-22T06:47:47+08:00）

## Summary table

| Area | Verdict | Evidence |
| --- | --- | --- |
| A. 文件树完整性 | **FAIL** — AGENTS.md 的树漏掉 11 个真实受版本控制的路径（整个 §.scratch/§ 和 §docs/agents/§）；树里列出的 24 个路径全部真实存在，没有"幽灵条目"；6 条角色描述过时或不完整 | 树在 §AGENTS.md:58-84§；§git ls-files§ = 39 个文件；见 A.1 / A.2 / A.3 |
| B. 事实过时 | **FAIL** — 12 项 VERIFIED/PARTIAL 声明中有 7 项今天已不成立；Git SHA、SDK 版本、游戏路径、桌面快捷方式、便携包输出位置全部过时；§E:§ 盘在本机根本不存在 | §AGENTS.md:320-335§；§git log§；§Get-PSDrive§；§Test-Path§ |
| C. 跨文档矛盾 | **FAIL** — 12 处互相矛盾，最严重的是"独立模式是否强制选游戏目录"（三份用户文档全部与代码相反）和"桌面快捷方式由谁生成、叫什么"（EXE 与根目录 PS1 生成同名但不同目标的快捷方式） | 见 C 节逐条引用 |
| D. 缺失约定 | **FAIL** — 7 项缺失：§docs/adr/§、术语表、构建 vs 校验命令区、§.md§/§.cs§ 编码规则、可复用的校验命令、§autopilot§ 指针（仓库内仅 1 处命中，且技能名已改为 automatt） | §AGENTS.md:368,370§；§docs/agents/domain.md:9,21§；全仓库 grep |
| E. docs/agents 质量 | **PARTIAL** — 三份文件都真实存在且被 AGENTS.md 引用，内容与 AGENTS.md 大体一致；但 §docs/adr/§ 指向空目录、§Status:§ 行格式描述与实际不符、五类 triage 标签在仓库内一次都没被使用 | §AGENTS.md:360,364,368§；§docs/agents/*.md§；§.scratch/**/issues/*.md§ |

---

## A. File-tree completeness

参照来源：
- 文档树：§AGENTS.md:58-84§（code fence 内的文本块，24 个条目）
- 真实树：§git -c core.quotepath=false ls-files§ = **39 个受版本控制的文件**（另有 §bin/§、§obj/§ 生成物，已按要求排除）

### A.1 真实存在但树里缺失的路径（11 个）

树完全没有表示 §.scratch/§ 和 §docs/agents/§ 两个目录。这两个目录都被 AGENTS.md 自己在 "Agent skills" 一节引用（§AGENTS.md:360§、§:364§、§:368§），所以 **AGENTS.md 内部自相矛盾**。

| # | 真实路径 | 该文件实际作用 |
| --- | --- | --- |
| 1 | §docs/agents/domain.md§ | 域文档/ADR/术语的读取约定，被 §AGENTS.md:368§ 引用 |
| 2 | §docs/agents/issue-tracker.md§ | 本地 markdown issue tracker 约定，被 §AGENTS.md:360§ 引用 |
| 3 | §docs/agents/triage-labels.md§ | 五类 triage 角色映射，被 §AGENTS.md:364§ 引用 |
| 4 | §.scratch/singlefile-portable/spec.md§ | 单文件便携版 effort 的 spec（§PublishSingleFile§ 决策来源，§:19§） |
| 5 | §.scratch/stability-ux-hardening/spec.md§ | 稳定化/UX effort 的 spec（5 票来源） |
| 6 | §.scratch/stability-ux-hardening/map.md§ | 该 effort 的 map（Decisions-so-far / Not yet specified） |
| 7 | §.scratch/stability-ux-hardening/issues/01-render-containment-self-heal.md§ | 已 resolved 票 01 |
| 8 | §.scratch/stability-ux-hardening/issues/02-standalone-skip-game-dir.md§ | 已 resolved 票 02 |
| 9 | §.scratch/stability-ux-hardening/issues/03-shutdown-watchdog.md§ | 已 resolved 票 03 |
| 10 | §.scratch/stability-ux-hardening/issues/04-custom-fallback-consistent.md§ | 已 resolved 票 04 |
| 11 | §.scratch/stability-ux-hardening/issues/05-configurable-observe-key.md§ | 已 resolved 票 05 |

补充：§docs/superpowers/plans/§ 与 §docs/superpowers/specs/§ 下的 5 个文件由目录条目 §docs/superpowers/§（§AGENTS.md:81§）概括表示，但树里没有区分 §plans/§ 与 §specs/§ 两个子目录。

### A.2 树里列出但实际不存在的路径

**无。** §AGENTS.md:60-83§ 列出的 24 个路径逐一在 §git ls-files§ 中命中，全部真实存在（§bin/§、§obj/§ 除外，树里本来也没列）。

### A.3 角色描述错误或过时的条目

| 树位置 | 原文 | 问题 | 真实路径 / 修正 |
| --- | --- | --- | --- |
| §AGENTS.md:61§ | §E_Key_Monitor.csproj  # .NET 8 WinForms 项目定义§ | 不完整。csproj 现在还承担"默认发布形态"职责：§PublishSingleFile=true§、§IncludeNativeLibrariesForSelfExtract=true§、§PublishTrimmed=false§（§E_Key_Monitor.csproj:11-13§），以及 §config.json§ 的 §CopyToOutputDirectory="PreserveNewest"§（§:17§）。AGENTS.md 全文从未出现 §PublishSingleFile§（全仓库 grep 只在 §README.md:151§ 和 §.scratch/singlefile-portable/spec.md§ 命中）。 | §E_Key_Monitor.csproj§ |
| §AGENTS.md:64§ | §Program.cs  # 解析 --follow-game 并启动 MainForm§ | **过时且与本文档自身矛盾**。§Program.cs:11-16§ 同时解析 §--launch-game§ 和 §--follow-game§，§:22-24§ 还设置高 DPI；AGENTS.md 自己的"程序入口和运行模式"一节（§:111-117§）写对了。 | §Program.cs§ |
| §AGENTS.md:65§ | §MainForm.cs  # 配置、Hook、绘制、托盘、拖动、联合启动、快捷方式、游戏跟随和清理§ | 不完整，漏掉 2026-09-10 已落地的 4 项能力：可配置观察键（§MainForm.cs:112§、§:306§、§:891§）、越界钳制 + 1s 自愈重绘（§:264§、§:936§、§:1001§）、8s 关闭看门狗（§:265§、§:1808-1836§）、独立模式可跳过游戏目录（§:527-602§）。 | §MainForm.cs§ |
| §AGENTS.md:72§ | §Package_E_Key_Monitor.ps1  # 生成便携版单文件包§ | 描述正确但不完整：脚本默认输出目录是**源码目录之外**的 §<源码父目录>\artifacts\E_Key_Monitor-portable§（§Package_E_Key_Monitor.ps1:27-29§），并会同时生成同名 ZIP（§:45§、§:131§）。AGENTS.md 全文没有写这个输出位置。 | §Package_E_Key_Monitor.ps1§ |
| §AGENTS.md:79§ | §Create_Naraka_Monitor_Shortcut.ps1  # 创建独立监控与游戏联合启动快捷方式§ | **描述错误**。该脚本创建的第二个快捷方式叫 §永劫无间（E键提示）.lnk§、目标是中文 BAT（§Create_Naraka_Monitor_Shortcut.ps1:141-148§），而 AGENTS.md §:268-271§ 说的 §启动StartGame和监控§ 是 EXE 托盘菜单生成的（§MainForm.cs:722-724§）。两者都写桌面 §E键监控工具.lnk§（**同名冲突**）。 | §Create_Naraka_Monitor_Shortcut.ps1§ |
| §AGENTS.md:81§ | §docs/superpowers/  # 计划和设计记录§ | 正确但不完整：该目录下同时存在 §plans/§（3 个）与 §specs/§（2 个），且 §plans/2026-07-24-single-exe-portable-launch.md§ 的 13 个步骤至今全部是未勾选的 §- [ ]§（见 B.7）。 | §docs/superpowers/§ |
| §AGENTS.md:60§ | §.gitignore  # 构建产物、本地状态和敏感文件排除规则§ | 正确但漏了与 §AGENTS.md:12§ 的不一致：§.gitignore:9-10§ 还排除了 §.agents/§ 和 §.codex/§，而 AGENTS.md:12 的"生成内容"清单里没有这两个。 | §.gitignore§ |

---

## B. Factual staleness

### B.1 Git 分支 / 最新提交 SHA —— **FALSE**

§AGENTS.md:335§：
> 当前 Git 工作树核查到分支为 §main§，最近提交为 §838e43e Initial commit§；本次保留所有未提交修改，没有执行提交、重置、切换或清理。

§README.md:362§（同样过时）：
> 当前目录是可用的 Git 工作树；核查到当前分支为 §main§，最近提交为 §838e43e Initial commit§。

**真实状态**（§git log --oneline --decorate§）：

| 提交 | 日期 | 说明 |
| --- | --- | --- |
| §986cf84§ | 2026-10-22 | **HEAD** — docs: update source path to C:\Project\E_Key_Monitor |
| §1f49a09§ | 2026-09-10 | chore(review): record two-axis review for singlefile portable |
| §a34bd74§ | 2026-09-10 | docs(singlefile): smoke pass plus script appendix and entry wording |
| §eb3d605§ | 2026-09-10 | feat(singlefile): default PublishSingleFile portable EXE |
| §da23b5c§ | 2026-09-10 | chore(review): record two-axis review verdicts for hardening |
| §2c549c1§ | 2026-09-10 | docs(review): sync observe-key docs, fix const layout, close hardening review |
| §2d88777§ | 2026-09-10 | fix(04-05): consistent custom fallback plus configurable observe key |
| §040641b§ | 2026-09-10 | fix(03): shutdown watchdog forces exit after 8s |
| §6296fcd§ | 2026-09-10 | fix(02): standalone mode can skip game directory |
| §ed248f6§ | 2026-09-10 | fix(01): render containment plus 1s self-heal retry |
| §db40320§ | 2026-07-24 | feat: simplify portable launcher and desktop shortcuts |
| §838e43e§ | 2026-07-20 | Initial commit ← 文档里写的这个 |

分支 §main§ ✅ 正确；工作树干净 ✅ 正确（§git status --porcelain§ 为空）；但 **SHA 落后 11 个提交**，HEAD 是 §986cf84§。§main§ 与 §origin/main§ 同步，远端为 §https://github.com/zkkk9555/E_Key_Monitor.git§。

### B.2 "当前核查状态（2026-07-24）"逐条复核

§AGENTS.md:320-333§ 原文 → 今天是否仍然成立：

| 行 | 原文要点 | 今天 | 依据 |
| --- | --- | --- | --- |
| §:322§ | VERIFIED：安装 .NET SDK §8.0.422§ | **FALSE** | 实际 §8.0.426§（§dotnet --list-sdks§ → §8.0.426 [C:\Program Files\dotnet\sdk]§）。且 §dotnet§ **不在 PATH** 上，必须用 §C:\Program Files\dotnet\dotnet.exe§ |
| §:323§ | VERIFIED：§dotnet build -c Release --no-restore§ 退出码 0，0 警告 0 错误 | **UNKNOWN**（未重跑） | 本次审计为只读，未执行构建。§bin/Release/net8.0-windows/§ 下确实存在成功产物 |
| §:324§ | VERIFIED：publish 退出码 0；便携目录和 ZIP 已在 §C:\tmp§ 生成 | **FALSE** | §C:\tmp§ 存在但**没有** §E_Key_Monitor-portable§（列出内容全是无关文件）。脚本当前默认输出是 §C:\Project\artifacts\E_Key_Monitor-portable§（§Package_E_Key_Monitor.ps1:27-29§），该目录也不存在 |
| §:325§ | VERIFIED：§dotnet format --verify-no-changes§ 退出码 0 | **UNKNOWN**（未重跑） | 本次未执行 |
| §:326§ | VERIFIED：5 个根目录 PS1 均为 UTF-8 with BOM，PS5.1 静态解析无错误；8 个 BAT 均为纯 ASCII；§config.json§ 可解析且 §gameDirectory§ 默认为空 | **TRUE ✅** | 重跑实测：5/5 PS1 首字节 §EF BB BF§，解析错误 0；8/8 BAT 高位字节数 0；§config.json§ BOM=True、§gameDirectory§ 为空、§key=E§ |
| §:327§ | PARTIAL：§dotnet test§ 退出码 0，但项目没有测试项目 | **TRUE ✅** | §git ls-files§ 中无任何测试项目/测试文件 |
| §:328§ | VERIFIED：当前验证环境中的 §StartGame.exe§、§NarakaBladepoint.exe§ 均存在 | **FALSE** | §E:\§ 盘在本机**根本不存在**（§Get-PSDrive§ 只有 §C:\§；§Test-Path 'E:\'§ = False）。§E:\Youxi\网吧极速版\program§ 不存在 |
| §:329§ | PARTIAL：外部关闭窗口请求后进程 30 秒内未退出，根因未定位 | **FALSE / 已被修复** | 票 03（commit §040641b§）已加 8s 看门狗：§MainForm.cs:265§ §ShutdownWatchdogMs = 8000§、§:1808-1836§ 超时 §Environment.Exit(1)§。根因已定位并处理 |
| §:330§ | STALE：当前桌面两个快捷方式仍指向 §C:\Users\24310\Projects\E_Key_Monitor§ | **FALSE（描述对象已不存在）** | 桌面上**根本没有** E_Key_Monitor 相关快捷方式（只有 7 个无关的 §.lnk§）；§C:\Users\24310\Projects§ 目录不存在。结论（需要重新生成）仍成立，但诊断内容必须重写 |
| §:331§ | VERIFIED：便携打包验证通过，目录和 ZIP 内只包含一个 EXE、§config.json§ 和中文说明书；没有 BAT 或 PS1 | **TRUE ✅（作为脚本契约）** | §Package_E_Key_Monitor.ps1:117-123§ 断言恰好 3 个文件；§:95-100§ 断言 staging 恰好 §E_Key_Monitor.exe§ + §config.json§；§:131§ ZIP 打包这 3 个文件 |
| §:332§ | UNKNOWN：未模拟 E 输入、未验证圆点切换、未实际启动游戏、未验证 5 秒跟随清理、未从托盘创建快捷方式、未双击便携 EXE | **TRUE ✅（仍然未知）** | 本次只读审计未做任何 GUI/游戏联动验证 |
| §:333§ | VERIFIED：修改集中在 §Program.cs§、§MainForm.cs§、配置、打包脚本和文档；没有修改测试、依赖或锁文件 | **TRUE ✅** | §git ls-files§ 无测试/依赖/锁文件 |

§README.md:345-360§ 是同一份清单的副本，结论逐条相同（§:349§ SDK 版本过时、§:351§ §C:\tmp§ 过时、§:355§ 游戏 EXE 存在性过时、§:356§ 30 秒僵尸进程过时、§:357§ 桌面快捷方式过时、§:349/:351/:352/:353/:354/:358/:359/:360§ 仍成立）。

### B.3 文档里的绝对路径 —— 逐个实测

| 文档位置 | 路径 | 存在？ | 说明 |
| --- | --- | --- | --- |
| §AGENTS.md:7§, §README.md:12§, §README.md:104,111,135§ | §C:\Project\E_Key_Monitor§ | ✅ **存在** | 且确为 git 顶层（§git rev-parse --show-toplevel§ = §C:/Project/E_Key_Monitor§）。**"唯一有效的源码目录"这一条成立 ✅** |
| §AGENTS.md:9§, §README.md:15§ | "E 盘上的项目副本（备份）" | ❌ **不存在** | §E:\§ 盘不存在。"不要读取 E 盘副本"的警告今天无对象 |
| §AGENTS.md:240-243§, §README.md:20,168-171,338-339§, §E键监控工具中文使用说明书.md:54§ | §E:\Youxi\网吧极速版\program§（及两个 EXE） | ❌ **不存在** | 全部 4 个路径都失效。§README.md:338-339§ 甚至把这两个 EXE 写进"联合启动失败时检查以下文件"清单，用户照着查必然找不到 |
| §AGENTS.md:330§, §README.md:357§ | §C:\Users\24310\Projects\E_Key_Monitor§ | ❌ **不存在** | 目录本身都不存在 |
| §AGENTS.md:86§, §README.md:194§ | "当前源码目录没有 §.lnk§ 文件" | ✅ **成立** | §git ls-files§ 无 §.lnk§；§.gitignore:35§ 也排除 §*.lnk§ |
| §AGENTS.md:324§, §README.md:351§ | §C:\tmp§（便携包产物） | ⚠️ 目录存在，但**无**便携包产物 | 见 B.2 §:324§ |
| §AGENTS.md:12§ | §.appdata/§、§.dotnet-home/§、§.nuget/§ | ❌ 当前都不存在 | 无妨，§.gitignore:6-8§ 已排除 |
| §docs/superpowers/plans/2026-07-24-single-exe-portable-launch.md:16,71-72,89-92§ | §C:\Projects\E_Key_Monitor§ | ❌ **不存在** | 这是**旧源码目录**（注意是 §C:\Projects§ 复数）。现行目录是 §C:\Project§ 单数 |
| §docs/superpowers/plans/2026-07-20-...md:16,43,187§ | §C:\Projects\Codex_Self-Cycling_Workflow§ | ❌ **不存在** | 引用仓库已被删除 |
| §docs/superpowers/plans/2026-07-20-...md:222§ | "the source directory is now §C:\Projects\E_Key_Monitor§" | ❌ **过时** | 现在是 §C:\Project\E_Key_Monitor§ |
| §README.md:148,340§ | §bin\Release\net8.0-windows\win-x64\publish\E_Key_Monitor.exe§ | ✅ **存在** | 161,816,043 字节，是单文件产物（同目录只有 §config.json§ + 一个 pdb） |
| §README.md:161§ | §..\artifacts\E_Key_Monitor-portable§ | ⚠️ 路径规则正确，目录当前不存在 | 与 §Package_E_Key_Monitor.ps1:28§ 一致 |
| §Start_E_Key_Monitor.ps1:10§ 等 | §bin\Release\net8.0-windows\win-x64\E_Key_Monitor.exe§ | ✅ 存在，但**是多文件布局** | 该目录有 §E_Key_Monitor.exe§(9.6MB) + §coreclr.dll§ + 约 60 个 DLL + runtimeconfig。三个根目录 PS1 都把它当"兼容回退路径"（§Start_E_Key_Monitor.ps1:38-39§、§Start_Naraka_With_Monitor.ps1:80-81§、§Create_Naraka_Monitor_Shortcut.ps1:107-108§），**文档从未说明这条回退路径启动的是多文件旧形态** |

### B.4 是否仍在描述"旧的单文件之前"的可携带布局？

**结论：用户向文档没有。没有一份用户文档把"多文件便携包"说成当前产物。**

- §AGENTS.md:88-94§：最终便携包 = §E_Key_Monitor.exe§ + §config.json§ + §E键监控工具中文使用说明书.md§ ✅
- §README.md:151§："这是 self-contained 单文件发布（§PublishSingleFile§）" ✅
- §README.md:161§："便携包只包含 Windows x64 自包含的 §E_Key_Monitor.exe§、可写的 §config.json§ 和中文说明书" ✅
- §E键监控工具中文使用说明书.md:5-16§：同上 ✅
- §E_Key_Monitor.csproj:11-13§ 确实设了 §PublishSingleFile=true§ / §IncludeNativeLibrariesForSelfExtract=true§ / §PublishTrimmed=false§ ✅

**但仍有 3 处残留的旧布局描述 / 旧形态事实：**

1. §AGENTS.md§ 的"构建和发布"一节（§:303-318§）**只字未提** §PublishSingleFile§，也没写便携包输出目录。它给出的
   > §dotnet publish -c Release -r win-x64 --self-contained true§
   表面上与"多文件发布"无区别；当前形态之所以是单文件，完全来自 csproj 默认值，而这份"事实来源"文档没有记录它。

2. §docs/superpowers/plans/2026-07-24-single-exe-portable-launch.md§ 是**当前**的单 EXE 实施计划，但 **13 个步骤全部是未勾选的 §- [ ]§**（§:19,32,48,52,74,78,82,94,98,102,111,124,128§），而该计划描述的工作**已经完成并发布**（§db40320§ + §eb3d605§）。计划文件还停留在旧源码目录 §C:\Projects\E_Key_Monitor§（§:16§、§:71-72§、§:89-92§）。

3. §docs/superpowers/specs/2026-07-24-chinese-entry-points-design.md§ 与 §docs/superpowers/plans/2026-07-24-chinese-entry-points.md§ 描述的是**旧的"BAT 包装器 + 游戏路径写死在根目录 config.json + 快捷方式指向 BAT"**方案：
   > §2026-07-24-chinese-entry-points-design.md:16§：§永劫无间（E键提示）.lnk§：启动监控工具后启动原始 §StartGame.exe§。
   > §2026-07-24-chinese-entry-points.md:13§：将用户提供的游戏目录 §E:\Youxi\网吧极速版\program§ 写入根目录默认配置，并由现有构建流程复制到运行目录。

   两份文件第 3 行都有"历史方案"横幅 ✅，但 §docs/superpowers/specs/§ 与 §plans/§ 下**没有任何索引或 README 指向"哪份是当前、哪份是历史"**，读者只能自己猜。

### B.5 文件数量声明 —— **TRUE ✅**

| 声明 | 位置 | 实测 |
| --- | --- | --- |
| "根目录全部 8 个 BAT" | §AGENTS.md:280§；§README.md:295-306§ | ✅ 根目录正好 8 个 §.bat§ |
| "当前根目录共有 5 个 PS1" | §AGENTS.md:293§；§README.md:308§ | ✅ 根目录正好 5 个 §.ps1§ |
| "下面 13 个脚本都在源码目录里" | §E键监控工具中文使用说明书.md:148-164§ | ✅ 8+5=13，表格里 13 个文件名逐一存在，无遗漏无多余 |

### B.6 "唯一有效的源码目录"声明 —— **TRUE ✅**

§AGENTS.md:7§、§README.md:9-13§ 写的 §C:\Project\E_Key_Monitor§ 与真实 git 顶层完全一致 ✅。今天 06:47 的提交 §986cf84§ 刚刚把 §C:\Projects§（复数）改成 §C:\Project§（单数），方向正确。
唯一残留问题：§AGENTS.md:9§ 与 §README.md:15§ 仍在警告"E 盘上的项目副本"，而 §E:\§ 盘在本机不存在。

### B.7 其他过时陈述

| 位置 | 原文 | 问题 |
| --- | --- | --- |
| §AGENTS.md:117§ | "普通模式…启动时会校验 §gameDirectory§；配置缺失或无效时会显示中文目录选择框，**取消选择会退出**" | **与代码相反**。票 02（commit §6296fcd§）后，纯独立模式取消选择会弹 §YesNoCancel§（§MainForm.cs:558-576§）："是"=跳过并继续运行，"否"=重选，"取消"=退出。跟随/联合模式才是强制（§MainForm.cs:538§、§:553-556§） |
| §AGENTS.md:135-158§ "悬浮窗和绘制" | "初次绘制失败会写入诊断日志并安全退出" | **部分过时**。票 01（commit §ed248f6§）后：越界先钳制（§MainForm.cs:1001§、§:1634§），瞬时失败置 §_renderDirty§ 由 tick 1s 重试（§:264§、§:916§、§:936§），只有"初次兜底"才退出 |
| §AGENTS.md:230§ | "连续约 §5§ 秒未发现游戏进程后调用 §RequestShutdown§" | 仍成立 ✅（§MainForm.cs:268§ §GameExitGracePeriod = 5s§）。但没提 8s 看门狗 |
| §AGENTS.md:303-314§ 构建命令 | §dotnet build§ / §dotnet publish§ / §.\Package_E_Key_Monitor.ps1§ | 命令本身正确，但本机 §dotnet§ **不在 PATH**，照抄会 "无法将 dotnet 识别为 cmdlet"。应写明完整路径或先加 PATH |
| §AGENTS.md:352§ 修改原则 | "修改代码后至少执行 §dotnet build -c Release§" | 不够：§dotnet format --verify-no-changes§、PS1 BOM/PS5.1 解析、BAT ASCII、安全 grep 这些门禁只出现在"核查状态"清单里（§:325-326§），从没被写成"改动后必须跑"的命令 |
| §E键监控工具中文使用说明书.md:3§ | "更新时间：2026-07-24" | 已过时 2 个多月；文档正文已包含 2026-09-10 的单文件/观察键变化 |
| §E键监控工具中文使用说明书.md:144§ | "项目已完成 Release 构建和静态检查" | 无日期、无命令、无结果，不可复现；与 AGENTS.md 的 VERIFIED 清单口径不同 |
| §.scratch/stability-ux-hardening/map.md:8§ | "本机有 .NET runtime、无 SDK（§dotnet --list-sdks§ 空）" | **现在相反**：本机 SDK 为 §8.0.426§（scratch 记录，但会让未来的 agent 误判环境） |
| §.scratch/stability-ux-hardening/map.md:7§ | "§MainForm.cs§（1877 行…）" | 现在是 **2034 行** |
| §docs/superpowers/plans/2026-07-20-...md:232§ | "The workspace has an empty §.git§ directory, so no commit can be created" | **过时**：现在是完整 git 仓库，12 个提交，远端已配置 |
| §docs/superpowers/plans/2026-07-20-...md:232§ | "3 root BAT files" | 现在是 8 个 |
| §docs/superpowers/plans/2026-07-20-...md:222§ | "the source directory is now §C:\Projects\E_Key_Monitor§" | 过时，现在是 §C:\Project\E_Key_Monitor§ |


---

## C. Cross-doc contradictions

### C.1 独立模式是否强制选择游戏目录（三份文档全错）

| 侧 | 引用 |
| --- | --- |
| AGENTS.md | §AGENTS.md:117§："普通模式…启动时会校验 §gameDirectory§；配置缺失或无效时会显示中文目录选择框，取消选择会退出。" |
| README.md | §README.md:34§："当前实现启动时会校验 §gameDirectory§；配置缺失或无效时会要求选择同时包含两个游戏 EXE 的目录，取消选择会退出。" |
| README.md | §README.md:101§："第一次运行或配置无效时会弹出目录选择框，**必须**选择同时包含 §StartGame.exe§ 和 §NarakaBladepoint.exe§ 的目录，取消选择会退出。" |
| 中文说明书 | §E键监控工具中文使用说明书.md:44§："**第一次启动任意模式时**，程序会显示“设置游戏目录...”窗口。请选择同时包含下面两个文件的目录：" |
| 真实代码 | §MainForm.cs:538§ §bool directoryRequired = _followGame || _launchGame;§；§:558-576§ 独立模式取消后弹 §MessageBoxButtons.YesNoCancel§，"是"=跳过运行并写诊断日志（§:567§） |

**矛盾点**：独立模式现在**可以跳过**游戏目录，文档仍说"必须选、取消即退出"。票 02（§6296fcd§）已修代码，文档没跟上。

### C.2 桌面快捷方式由谁生成、叫什么、图标是什么

| 侧 | 引用 |
| --- | --- |
| AGENTS.md | §AGENTS.md:266-276§："§E键监控工具§" 和 "§启动StartGame和监控§"，"两个快捷方式都使用当前 EXE 图标，工作目录为 EXE 所在目录" |
| README.md | §README.md:53-58§、§:196-208§：同上 |
| 中文说明书 | §E键监控工具中文使用说明书.md:24-27§："§E键监控工具.lnk§"、"§启动StartGame和监控.lnk§"，"两个快捷方式都使用 EXE 的正常软件图标，不会显示 BAT 图标。" |
| 真实代码 A | §MainForm.cs:722-724§ → §E键监控工具.lnk§ + §启动StartGame和监控.lnk§，目标 = EXE，图标 = EXE（§MainForm.cs:768§ §IconLocation = $"{executablePath},0"§） |
| 真实代码 B | §Create_Naraka_Monitor_Shortcut.ps1:132-148§ → §E键监控工具.lnk§（目标 = §启动E键监控工具.bat§）+ §永劫无间（E键提示）.lnk§（目标 = §启动永劫无间E键提示.bat§，图标 = §"$GameLauncher,0"§） |

**矛盾点**：三份文档只描述了机制 A，完全没提机制 B 仍在仓库里（§AGENTS.md:78-79§ 还把它列为"两个桌面快捷方式的创建入口"）。后果有三：
1. **同名冲突**：两个机制都写桌面 §E键监控工具.lnk§，跑一次 PS1 就会覆盖掉 EXE 生成的、指向 EXE 的那个。
2. **名称不一致**：PS1 建的是 §永劫无间（E键提示）.lnk§，文档里的 §启动StartGame和监控§ 根本不会由脚本产生。
3. **图标不一致**：PS1 给联合快捷方式用的是**游戏启动器**图标（§:146§），中文说明书 §:27§ 明确说"不会显示 BAT 图标"、§AGENTS.md:106§ 说用"当前监控 EXE 图标"——对 PS1 路径都不成立。

### C.3 便携包产物位置

| 侧 | 引用 |
| --- | --- |
| AGENTS.md | §AGENTS.md:324§："最终便携目录和 ZIP 已在 §C:\tmp§ 生成" |
| README.md | §README.md:161§："脚本会在项目目录外生成 §..\artifacts\E_Key_Monitor-portable§ 和旁边的 §E_Key_Monitor-portable.zip§" |
| 真实脚本 | §Package_E_Key_Monitor.ps1:27-29§：默认 = §Split-Path(sourceDirectory -Parent)\artifacts\E_Key_Monitor-portable§ → 本机即 §C:\Project\artifacts\E_Key_Monitor-portable§；ZIP 在同级（§:45§） |

**矛盾点**：README 与脚本一致 ✅；AGENTS.md 的 §C:\tmp§ 与两者都不一致 ❌（§C:\tmp§ 只能是当时手工传 §-OutputDirectory§ 的产物，现在已不存在）。

### C.4 关闭行为：30 秒僵尸进程 vs 8s 看门狗

| 侧 | 引用 |
| --- | --- |
| AGENTS.md | §AGENTS.md:329§："通过外部关闭窗口请求后，进程在 30 秒内未退出…根因未定位" |
| README.md | §README.md:356§：同 |
| 真实代码 | §MainForm.cs:265§ §ShutdownWatchdogMs = 8000§；§:1820-1836§ 超时写诊断后 §Environment.Exit(1)§；§:1851-1853§ 正常关闭时解除 |
| effort 记录 | §.scratch/stability-ux-hardening/issues/03-shutdown-watchdog.md:7,9-11§ §Status: resolved§ |

**矛盾点**：AGENTS.md/README 仍在播报一个已被修复的问题，并把"根因未定位"写成现状。

### C.5 "当前核查状态"日期与内容

§AGENTS.md:320§ 的标题是"当前核查状态（2026-07-24）"，但仓库在 2026-09-10 有 6 个提交、2026-10-22 有 1 个提交；§README.md:347§ 又写"截至 2026-07-24"。两份文档用同一个旧日期描述一个已经变了三次的代码库。

### C.6 项目结构树：AGENTS.md 与 README.md 同样不完整

§AGENTS.md:58-84§ 与 §README.md:253-279§ 的两棵树**结构一致**，也都漏掉 §.scratch/§ 和 §docs/agents/§（共 11 个受控文件）。两份"事实/说明"文档对仓库结构给出同样的错误印象。

### C.7 术语不一致：§docs/agents/domain.md§ 允许缺失，§AGENTS.md§ 当作存在

| 侧 | 引用 |
| --- | --- |
| AGENTS.md | §AGENTS.md:368§："Single-context: §AGENTS.md§ + §docs/agents/§ + §docs/adr/§. See §docs/agents/domain.md§." |
| docs/agents | §docs/agents/domain.md:9§："§docs/adr/§: read ADRs that touch the area you're about to work in."；§:11§："If any of these files don't exist, **proceed silently**." |

**矛盾点**：§docs/adr/§ **不存在**（实测）。domain.md 明说"不存在就静默继续"，AGENTS.md 却把它写进"域文档集合"且没有任何"可能不存在"的限定。

### C.8 文件树角色 vs 真实入口：§Create_Naraka_Monitor_Shortcut.bat/.ps1§

§AGENTS.md:78§ "两个桌面快捷方式的创建入口" 与 §AGENTS.md:266-276§"两个快捷方式都是 EXE 指向" 不一致（详见 C.2）。

### C.9 便携包"3 文件"vs 发布 staging"2 文件"

§AGENTS.md:88-94§（便携包 = 3 文件）、§AGENTS.md:331§（"目录和 ZIP 内只包含一个 EXE、§config.json§ 和中文说明书"）与脚本行为一致 ✅，但脚本内部有**两级**契约（staging 2 个文件 §Package_E_Key_Monitor.ps1:95-100§ / 包 3 个文件 §:117-123§），AGENTS.md 只描述了其中一级。这是"文档层级低于实现层级"的不一致，未来维护者容易误改断言。

### C.10 §Start_E_Key_Monitor.ps1§ 等脚本的"兼容回退路径"从未被文档提及

三个根目录 PS1 的查找顺序是"publish 单文件 EXE → §bin\...\win-x64\E_Key_Monitor.exe§"（§Start_E_Key_Monitor.ps1:9-10,35-40§ 等）。后者是**多文件旧布局**（本机实测 60+ DLL）。AGENTS.md 的"当前文件职责"与"构建和发布"都没提这条回退路径，也没说它启动的不是便携形态。

### C.11 §.scratch/§ effort 与 AGENTS.md 树互相矛盾

§AGENTS.md§ 自己在 §:360§ 说"Local markdown under §.scratch/§ (one directory per effort)"，但 §:58-84§ 的树里没有 §.scratch/§。已完成的两个 effort（§stability-ux-hardening§、§singlefile-portable§）完全不出现在事实来源文档中。

### C.12 "13 个脚本"的表述粒度不一致

§E键监控工具中文使用说明书.md:148§："下面 13 个脚本都在源码目录里"，表格逐个列出 ✅ 正确。AGENTS.md §:280-299§ 则拆成"8 个 BAT"+"5 个 PS1"两组，不写总数。中文说明书和 §.scratch/singlefile-portable/spec.md:5§ 都以"13"为基数，建议 AGENTS.md 统一口径。

---

## D. Missing conventions

### D.1 ADR 目录 —— **不存在**

- 引用方：§AGENTS.md:368§（"§docs/adr/§"）、§docs/agents/domain.md:9§（"read ADRs that touch the area you're about to work in"）、§docs/agents/domain.md:21§（示例 "§Contradicts ADR-0007 (event-sourced orders)…§"）
- 现实：§docs/adr/§ **不存在**（§Test-Path§ = False），仓库里也**没有任何一份 ADR**（全仓库 grep §ADR-§ 只命中 §domain.md:21§ 的举例）
- 结论：§docs/agents/domain.md:21§ 的例句 §ADR-0007 (event-sourced orders)§ 是**从别的项目抄来的模板**，与本项目（WinForms + 单文件便携 + 被动观察 E 键）毫无关系，会直接误导 agent。
- 缺失的约定：**ADR 的目录约定、命名格式（§ADR-NNNN-<slug>.md§？状态字段？）、何时必须写 ADR、ADR 与 §docs/agents/domain.md§ 的关系**。AGENTS.md 声明了目录却没声明用法。

### D.2 术语表 / glossary —— **不存在**

- 仓库里唯一的 glossary 是两份 effort spec 内的 §## Glossary§（§.scratch/singlefile-portable/spec.md:35-38§、§.scratch/stability-ux-hardening/spec.md:38-42§），属于**一次性 effort 词汇**，不是项目级词汇。
- §docs/agents/domain.md:13-15§ 说"Domain vocabulary for this repo lives in §AGENTS.md§ … and the spec Glossary of the **active** §.scratch/<slug>/spec.md§"，但：
  - **没有任何机制标记"active" spec**（§docs/agents/issue-tracker.md§ 没定义 active 概念；§.scratch/WORKFLOW-ACTIVE.md§ 不存在）；
  - 两个 effort 都已完成（票全部 §resolved§、spec 的 slice 全部 §[x]§），没有 active；
  - §AGENTS.md§ 里没有正式术语表，只有"偏白色/高亮青绿色/穿透/跟随"等中文口语描述，没有唯一英文名。
- 缺失的约定：**项目级术语表**（中英对照 + 定义），至少覆盖：悬浮圆点/激活状态/§activeTime§/点击穿透/§Custom§ 位置/游戏跟随模式/§--launch-game§/便携包/单文件发布/看门狗/自愈/观察键。

### D.3 "构建 vs 校验"命令分区 —— **缺失**

§AGENTS.md:303-318§"构建和发布"只给了 5 条**生成**命令（§dotnet build§ / §dotnet run§ / §dotnet run -- --follow-game§ / §dotnet run -- --launch-game§ / §dotnet publish§ / §.\Package_E_Key_Monitor.ps1§），**没有任何一条"校验"命令**。

事实上项目有 5 类门禁，但只出现在两处：
- §AGENTS.md:325-326§ / §README.md:352-353§（"核查状态"清单里作为历史结论）；
- §docs/superpowers/plans/2026-07-20-remove-legacy-workflow-and-adopt-superpowers.md:95-177§（一整段内联 PowerShell：BOM 检查 + PS5.1 解析 + BAT 高位字节检查 + §config.json§ 解析 + 禁止 API grep）—— 埋在一份**历史计划**里，没有脚本文件，agent 不会去那儿找。

缺失的约定：**一个命名清晰、可重复执行的校验入口**（例如 §scripts/verify-conventions.ps1§ 或 §docs/agents/verification.md§），把下面 5 类门禁固化下来：

1. §dotnet build -c Release§（0 警告 0 错误）
2. §dotnet format --verify-no-changes§（现行代码风格门）
3. 8 个 BAT 纯 ASCII + 5 个 PS1 UTF-8 BOM + PS5.1 静态解析
4. §config.json§ 可解析 + 关键默认值（§position=MiddleLeft§、§activeTime=200§、§clickThrough=true§、§gameDirectory=null§、§key=E§）
5. 安全边界禁止 API grep（§SendInput|keybd_event|mouse_event|SetCursorPos|SendKeys|WH_MOUSE_LL|ReadProcessMemory|WriteProcessMemory|OpenProcess|VirtualAllocEx|CreateRemoteThread|Process.Kill|TerminateProcess|NarakaBladepoint.exe.*(Start|Launch)§ 等，完整正则见该 plan 的 §:138§）

另外还缺：**"哪些改动需要哪些门禁"的映射表**（改了 §.cs§ → build + format；改了 §.ps1§ → BOM + parse；改了 §.bat§ → ASCII；改了 §config.json§ → 可解析 + 默认值；改了 §csproj§ → publish + 便携包契约）。

### D.4 中文 / 源码文件的编码与换行规则 —— **缺失**

现有的"脚本编码规则"（§AGENTS.md:278-301§）只覆盖：
- 8 个 BAT → 纯 ASCII（§AGENTS.md:280-291§，README §:295-306§）✅
- 5 个 PS1 → UTF-8 with BOM，文件头 §EF BB BF§（§AGENTS.md:293-301§，README §:308-316§）✅
- §config.json§ → UTF-8 with BOM（§AGENTS.md:189§、README §:233§）✅

**没有任何一条规则覆盖 §.md§ 和 §.cs§。** 实测现状（今天全部如此）：

| 文件 | BOM | 换行 |
| --- | --- | --- |
| §README.md§ | ❌ 无 BOM | CRLF |
| §AGENTS.md§ | ❌ 无 BOM | CRLF |
| §E键监控工具中文使用说明书.md§ | ❌ 无 BOM | CRLF |
| §docs/agents/*.md§（3 份） | ❌ 无 BOM | CRLF |
| §MainForm.cs§ / §Program.cs§ | ❌ 无 BOM | CRLF |

缺失的约定：
- **Markdown（含中文）**：应明确 "UTF-8 **without** BOM，CRLF"（Windows 记事本/某些编辑器会加 BOM；AGENTS.md 说"必须始终保持"的对象只有 BAT/PS1，容易被误读为"所有文件都要 BOM"）。
- **C# 源码**：UTF-8 无 BOM + CRLF；中文提示字符串（§MainForm.cs§ 里大量中文字面量）在 PS5.1/记事本场景下的可读性。
- **换行符**：全仓库 CRLF

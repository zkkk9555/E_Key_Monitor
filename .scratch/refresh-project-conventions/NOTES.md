# NOTES — refresh-project-conventions

Skills called: ask-matt(inline)→grilling→to-spec(inline)→writing-for-agents→domain-modeling→tdd→implement(inline)→code-review

## 开工卡（文件级明细）

开工卡｜任务：理解 E_Key_Monitor，并把过时的「项目规范」核对、更新、补全｜slug：refresh-project-conventions｜lane：B｜档位：full
本轮调用：ask-matt(inline) → grilling → writing-for-agents → domain-modeling → to-spec(inline) → tdd → implement(inline) → code-review
调用面：ask-matt=inline(read ask-matt/SKILL.md:1，Skill 工具被拒)｜grilling=tool｜writing-for-agents=tool｜domain-modeling=tool｜to-spec=inline(read to-spec/SKILL.md:1，disable-model-invocation)｜tdd=tool｜implement=inline(read implement/SKILL.md:1，disable-model-invocation)｜code-review=tool
分流：shield n-a｜Gate0 HIT @AGENTS.md:1｜setup done
正身：ask-matt、grilling、writing-for-agents、domain-modeling、to-spec、tdd、implement、code-review
必读集：AGENTS.md:1-380、README.md:1-365、MainForm.cs:1-2034、Program.cs:1-27、E_Key_Monitor.csproj:1-19、config.json:1-12、Package_E_Key_Monitor.ps1:1-143、docs/agents/*.md
链·产物：grilling=.scratch/refresh-project-conventions/spec.md｜to-spec=.scratch/refresh-project-conventions/spec.md｜tdd=tools/verify-conventions.ps1｜implement=AGENTS.md、README.md、E键监控工具中文使用说明书.md、docs/agents/domain.md、docs/adr/*.md｜code-review=NOTES.md「review」节

## 本 lane 必读集已读

- `AGENTS.md` 全文 380 行（当前唯一 AI 事实源）
- `README.md` 全文 365 行
- `E键监控工具中文使用说明书.md` 全文 164 行
- `MainForm.cs`：1-145（配置/常量）、317-436（构造/OnLoad）、463-602（菜单/游戏目录）、856-965（Hook/激活/自愈）、1317-1420（跟随状态机）、1796-1873（保存/关闭/清理）
- `Program.cs` 全文、`E_Key_Monitor.csproj` 全文、`config.json` 全文
- `Package_E_Key_Monitor.ps1` 全文 143 行
- `docs/agents/{domain,issue-tracker,triage-labels}.md` 全文
- `.scratch/stability-ux-hardening/spec.md`、`.scratch/singlefile-portable/spec.md`

## shield 关键词 grep 结论

`publish|submit|upload|release|spend|credential|store|compliance|上架|提交|内购|支付` 全仓 grep：命中全部是 `dotnet publish` 语境、`Release` 构建配置、`docs/agents/issue-tracker.md` 的 “publish to the issue tracker”（本地 markdown 语义）、`issue-tracker.md:34` 的 GitHub remote 说明。**无对外发布、上架、凭据、付费、合规面** → shield n-a。

## Gate 0 判定

请求对象 = 「项目规范」。机制**已存在**：`AGENTS.md`（AI 事实源）、`README.md`、`docs/agents/*`、中文说明书。→ **HIT**。
按 prelane.md「HIT → 只补漂移，不重写」：本轮不改写规范的存在形态与文件分工，只修**与当前代码/仓库不一致的事实**并补齐**缺失的约定**；不新建同义文件、不换技术栈。
HIT 证据：`AGENTS.md:1`、`AGENTS.md:53`（文件职责树）、`README.md:251`（项目结构树）、`docs/agents/domain.md:7-9`。

## 已知漂移（本轮要修的事实清单，带证据）

1. `AGENTS.md:335`、`README.md:362`：「最近提交为 `838e43e Initial commit`」→ 实况 HEAD = `986cf84 docs: update source path to C:\Project\E_Key_Monitor`，共 5 条提交。**STALE**。
2. `AGENTS.md:323`、`README.md:349`：「.NET SDK `8.0.422`」→ 实况 `dotnet --version` = **8.0.426**。**STALE**。
3. `AGENTS.md:324`、`README.md:351`：「便携目录和 ZIP 已在 `C:\tmp` 生成」→ `Package_E_Key_Monitor.ps1:28` 默认输出是 `<源码父目录>\artifacts\E_Key_Monitor-portable`。**STALE**。
4. `AGENTS.md:317` 段、`README.md:34`、`README.md:101`、说明书 `:44`：「普通模式启动时校验 gameDirectory，取消选择会退出」→ 实况 `MainForm.cs:535-577`：纯独立模式弹 Yes/No/Cancel，**可跳过**；只有 follow/launch 模式强制。**WRONG**。
5. `AGENTS.md:53-77` 文件职责树：漏 `docs/agents/`（3 个文件，已被 AGENTS.md 自己的「Agent skills」节引用）、漏 `.scratch/`（git 跟踪 8 个文件）、漏 `docs/superpowers/plans|specs` 子目录。**INCOMPLETE**。
6. `AGENTS.md:370`：「先调 autopilot」→ skill 已改名 `automatt`，仓内无 `automatt: on` 凭据行。**改名迁移未落地**。
7. `AGENTS.md` 全篇：未记录 `PublishSingleFile=true`（`E_Key_Monitor.csproj:11-13`）、未记录 8s 关闭看门狗（`MainForm.cs:265,1808-1839`）、未记录 1s 渲染自愈（`MainForm.cs:264,916-925`）、未记录独立模式跳过目录、未记录 10 分钟游戏启动超时气泡（`MainForm.cs:1384-1400`）、未记录 `config.json` 写回带 BOM（`MainForm.cs:99`）。**INCOMPLETE**。
8. `AGENTS.md:4-8`：断言「E 盘上的项目副本是用户保留的备份」「E 盘同时包含真实游戏安装目录」→ 本机 `Get-PSDrive` 只有 C 盘，`Test-Path 'E:\'` = False。**WRONG（本机）**。
9. `AGENTS.md:330`、README `:345-362`、说明书 `:3`：「当前核查状态（2026-07-24）」→ 已过 3 个月且事实已变；且 AGENTS.md 与 README **双份**陈述同一批核查结论 = 重复事实源。**STALE + DUPLICATED**。
10. `docs/agents/domain.md:9`：指向 `docs/adr/`，该目录不存在 → 指针落空。**MISSING**。

## 本机环境事实（2026-10-22 实测）

- `dotnet --version` → `8.0.426`（SDK 装在 `C:\Program Files\dotnet`，**不在 PATH**；脚本/文档里要写全路径或先加 PATH）
- `dotnet build -c Release` → 退出码 0，0 警告 0 错误
- `dotnet publish -c Release -r win-x64 --self-contained true` → 退出码 0；产物 = `E_Key_Monitor.exe`(161,816,043 B) + `config.json` + **`E_Key_Monitor.pdb`**（裸 publish 会带 pdb；打包脚本用 `-p:DebugType=None -p:DebugSymbols=false` 才严格 2 文件）
- `dotnet format --verify-no-changes` → 退出码 0
- 8 个根 BAT 全部纯 ASCII（非 ASCII 字节数 = 0）；5 个根 PS1 全部 UTF-8 with BOM
- 全部 `Test-Path` 用法都是 `-LiteralPath`（22 处）
- 根 `config.json`：可解析、10 个字段齐、`gameDirectory` 为 null、UTF-8 with BOM
- 本机**没有 E 盘**；`E:\Youxi\网吧极速版\program`、`C:\Users\24310\Projects\E_Key_Monitor` 都不存在；桌面**没有**本项目生成的 `.lnk`
- 源码目录**没有** `.lnk`（与 AGENTS.md 一致）

## 门禁（verification-gates.md）

- 本轮 seam：`tools/verify-conventions.ps1`（约定校验器）。红基线 = 当前漂移（旧 SHA / 旧 skill 名 / 树不全 / 指针落空）；修完必须绿。
- 代码门：`dotnet build -c Release` 退出码 0、0 警告；`dotnet format --verify-no-changes` 退出码 0。
- 脚本门：BAT ASCII、PS1 BOM、PS 5.1 静态解析。
- 明确不声称：未实际启动游戏、未模拟 E 键、未验证圆点颜色切换、未验证托盘菜单、未验证桌面快捷方式生成。
## review 第一轮（父会话内联自跑，code-review）

固定点：HEAD = 986cf84（改动全部未提交时复核；随后以 0fe981e 落地）。

### Standards 轴

- 硬违规：**无**。改动没有违反 AGENTS.md 任何一条明文约定：BAT 仍纯 ASCII、全部 PS1 带 BOM、路径区分「当前源码目录 / 部署机示例」、无第三方依赖、C# 零改动。
- 判断项（已修）：`tools/verify-conventions.ps1` 里有两个投机性泛化——① 无人调用的 `-Quiet` 开关；② 改剩一个元素后仍保留的 `$Script:ProductRoots` 数组与遍历。**已删除**（5 处替换），改后校验器仍 11/0 绿、PS 5.1 解析 0 错。
- 判断项（接受）：`Get-RepoFiles` 的 `-ProductOnly` 与 `Get-Lines` 均有多个调用点，保留。
- Fowlery 气味基线逐条扫过：文档改动不产生新气味；`Write-Result` 统一收口输出格式，没有重复逻辑。

### Spec 轴

- 缺失/半成品：**无**。S1（校验器，红→绿实测）、S2（AGENTS.md + domain.md + 3 条 ADR + 旧名改写 + 凭据行）、S3（README + 中文说明书 + 核查状态单点化）全部落地。
- 越界：**无**。`git diff HEAD --name-only` 里没有任何 `.cs`；没有引入依赖、框架、CI；没有启动游戏或生成快捷方式。
- 看着像做了其实不对：**无**。唯一一处需说明的是 spec 的 S1 写成「8 类检查」而实现是 11 类——那是写 spec 时的估计数，不是契约；已把 spec 改成 11 并在「范围调整」一节记明，属于把 spec 改成事实，不是把实现改成 spec。
- Out of Scope 的「不把 docs/superpowers/ 下的历史计划文档重写成新格式」：只加了一行状态批注、正文未动，符合「历史只追加、不重写」。

### 复核结论

- Standards：0 硬违规，2 判断项已修。
- Spec：0 缺失、0 越界、0 实现错误。
## review 第二轮（两个子代理的实际报告，code-review 双轴）

固定点：HEAD = 986cf84（改动未提交时复核）。两个轴各派一个只读子代理，分别按 Standards / Spec 简报跑。

### Standards 轴（子代理报告，已逐条处理）

| # | 发现 | 处理 |
| --- | --- | --- |
| 1 | 「三个根目录启动脚本先找 publish 目录的单文件 EXE」点错了集合：真正有 publish→runtime 回退的是 `Start_E_Key_Monitor.ps1` 与 `Start_Naraka_With_Monitor.ps1`，`Start_Naraka_With_Monitor_Dev.ps1` 走 `dotnet run` | 已改写为精确的两脚本 + 明确 Dev 脚本走 dotnet run |
| 2 | 「直启游戏本体」检测是死的：要求同一行同时出现 `NarakaBladepoint.exe` 与 `Process.Start`，而真实代码分两行（`:651` 拼路径、`:667` 起进程），把启动器改指游戏本体仍会绿 | 改为**结构性**判据：拼 `NarakaBladepoint.exe` 路径的 `Path.Combine` 必须带 `IsRegularFile`（存在性校验），否则报红 |
| 3 | `doc-tree-covers` 只排 `bin/obj/.git`，而文档声明 `.appdata/.dotnet-home/.nuget/.agents/.codex` 是生成物，别的开发机一有这些目录就误报 | 改为**从 `.gitignore` 解析目录条目**（只认以 `/` 结尾的、字符组取首个候选），生成目录一律不进扫描面 |
| 4 | 「ADR 正文不记实现细节」这条新规被自己写的 3 条 ADR 违反 | 规则改写为「可以带一两个实现锚点，但不要复述实现」；ADR-0002 补 `## Consequences` |
| 5 | 28 条文件树在 AGENTS.md 与 README.md 各存一份，校验器只看前者，两边会静默漂移 | 新增 `doc-trees-match` 检查：两棵树的条目集合必须一致 |
| 6 | `portable-contract` 去 grep 打包脚本的字面源码文本，改个数组排版就误报 | 改为**解析** `$expectedNames` / `$expectedPackageNames` 数组后比对集合 |
| 7 | `-Quiet` 开关与 `$ProductRoots` 数组是投机性泛化 | 已删除（第一轮自跑时已删，子代理看到的是旧版本） |
| 8 | `Get-RepoFiles` 返回绝对路径却叫「仓库文件」；`$bad` 装的是计数不是字节 | 改名 `Get-RepoFilePaths`；`$bad` → `$nonAsciiCount` |

### Spec 轴（子代理报告，已逐条处理）

| # | 发现 | 处理 |
| --- | --- | --- |
| 1 | `no-pinned-sha` 只匹配 `git log` 里真实存在的提交号，文档钉一个不存在的 SHA 仍能过 | 增加**形态判据**：7–40 位十六进制串（含数字）一律不允许出现在三份规范文档里 |
| 2 | `adr-pointer` 只看 `docs/adr/` 有没有 md，不解析 domain.md 里列出的具体文件名，改名仍绿 | 改为**逐个解析并验证** domain.md 里 `docs/adr/*.md` 引用是否真实存在 |
| 3 | AGENTS.md 的核查状态里，BAT/BOM 与桌面 .lnk 两条没有给复算命令 | 两条后面都补了复算命令 |
| 4 | `Get-Lines $file` 传的是 FileInfo，PowerShell 会取 ToString()（只有文件名），导致**脚本依赖当前工作目录**：换目录执行就崩，且没有 FAIL 行、没有汇总 | 全部路径函数改为接受 FileInfo 并取 `.FullName`；实测从 `%TEMP%` 与仓库根执行结果一致（12/12） |
| 5 | 「图标取自游戏启动器」只对第二个快捷方式成立，第一个用的是监控 EXE 图标 | 已改成分别说明 |
| 6 | 「safety-boundary 静态兜底」夸大了覆盖范围（没查固定 PID / 网络 / 终止进程） | 补齐三类检查，并把 AGENTS.md 的表述改成「哪些能静态兜底、哪些只能靠自觉」 |
| 7 | 越界检查：无实质越界；`docs/agents/domain.md` 改写了「proceed silently」措辞、换掉 ADR-0007 示例、又加了一份术语表（第三份副本） | 措辞与示例已还原/改成本仓真实 ADR；术语表删除，改为指向单一出处 |

### 第二轮结论

- Standards：0 硬违规残留；7 条判断项已改。
- Spec：0 缺失、0 越界、0 实现错误；7 条已改。
- 改完复跑：校验器 12/12 绿（仓库根与 `%TEMP%` 两处执行结果一致）、build 0 警告 0 错误、format 通过、6 个 PS1 静态解析 0 错。



## 本轮门禁实测结果

红基线（改文档前）：

```text
FAIL doc-tree-covers / no-pinned-sha / no-old-skill-name / skill-credential-explicit / adr-pointer
=== verify-conventions: 6 passed, 5 failed ===   exit 1
```

绿（改完）：

```text
=== verify-conventions: 11 passed, 0 failed ===   exit 0
```

其它门：

```text
dotnet build -c Release            -> exit 0, 0 警告 0 错误
dotnet format --verify-no-changes  -> exit 0
PowerShell 5.1 静态解析            -> 6 个 PS1 全部 PARSE-OK
```

## 子代理与审计

- `scripts-auditor`（spawn_teammate）：两次运行都被 harness 中断，只留下空壳 `audit-scripts.md`；脚本/构建/编码核查改由我用确定性 PowerShell 脚本自己跑完，证据已重写进 `audit-scripts.md`。
- `docs-auditor`（spawn_teammate）：跑完并交付 `audit-docs.md`（334 行，事实核对到位）。它的 4 条补充发现已按「范围调整」处理。

## 工具摩擦（skill 侧）

- `edit` 工具写 `.ps1` 会**剥掉 UTF-8 BOM**，PS 5.1 随后按 ANSI 读该文件 → 解析错误。改完 PS1 必须重新打 BOM。
- PowerShell 里用 `-split [Environment]::NewLine` 改写文件时把内容写空了（只剩 3 字节 BOM），改脚本别走行拆分路径。
- `AGENTS.md` 的一次 `edit` 报 `ReplaceFileW EIO`；先 grep 确认未落盘，再重试即成功。

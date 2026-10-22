# spec: github-release-portable

Status: ready-for-agent

## Problem

用户只想在**游戏机**上用一个干净的单文件 EXE，不想碰开发仓库、不想拷文件夹。他要的是：仓库的 Releases 页面能下载到一个通用 EXE，双击 → 选一次游戏目录 → 就能用；之后还能在软件里打开「开机自启」，开机自动待命。

现状：便携包只在本地生成（`C:\Project\artifacts\E_Key_Monitor-portable`），仓库**一个 tag、一个 release 都没有**（`git tag` 空、`gh release list` 空），远端是 `zkkk9555/E_Key_Monitor`（公开）。

## Solution

在 `main` 上打第一个版本 **v1.0.0**，发布两个附件：

- `E_Key_Monitor.exe`：单文件 self-contained EXE（用户点名要的"干净 EXE"）。
- `E_Key_Monitor-portable.zip`：完整便携包（EXE + config.json + 中文说明书）。

并在 README 与中文说明书里加「去哪儿下载」一节，指向 Releases 页面。

## User-Stories

1. 作为在游戏机上用这个工具的人，我只想在仓库的 Releases 页面下载一个文件，不想处理开发仓库。
2. 作为下载的人，我下载的 EXE 必须**自带运行时**（目标机没装 .NET 也能跑）。
3. 作为下载的人，我下载的 EXE 应该是**单个文件**，放进任意空文件夹双击即可。
4. 作为下载的人，第一次运行时应该被引导去选游戏目录，之后不用再选。
5. 作为下载的人，我要在软件里能找到「开机自启」开关。
6. 作为仓库主人，我发出去的必须是**我验收过的那一个二进制**，不是重新构建的另一个。

## Implementation-Decisions

- **版本号 v1.0.0**：仓库此前没有任何 tag，这是第一个正式可用版本。
- **发布物来自打包脚本的输出目录**（`Package_E_Key_Monitor.ps1` 自带三重断言：发布目录恰好 2 件、便携包恰好 3 件、config 保留便携默认值），不重新构建、不手工拼。
- **发布前先冒烟**：把**要发的那一个 EXE** 复制到临时目录、配好 config 真跑一次，确认窗口出现，再发。
- **顺手修掉打包脚本的一个真问题**：`dotnet` 不在 PATH 时脚本直接崩（这台笔记本就是这样），改成先找 PATH、再找 `%ProgramFiles%\dotnet\dotnet.exe`，都没有才给中文报错。开发脚本同步。
- **发布说明写"已知边界"**：开机自启记的是绝对路径（挪文件夹要重开一次）、配置写在 EXE 旁边（目录要可写）、真游戏端到端未联调。

## Testing-Decisions

- 打包脚本产出：便携目录恰好 3 件、ZIP 存在、EXE 的 SHA-256 记录在案。
- 冒烟：复制**发布物**到临时目录运行 → 进程存活、窗口可见 → 结束。
- 脚本门：8 BAT 纯 ASCII、6 PS1 带 BOM、PS 5.1 解析 0 错、`dotnet format --verify-no-changes` 通过、`dotnet build` 0 警告 0 错误、约定门 12/12。
- **发布后**：`gh release view` 确认两个附件的体积与 SHA 一致。

## Out-of-Scope

- 不做安装器、不做代码签名、不做多架构（只有 win-x64）。
- 不改运行时行为（本轮只加脚本健壮性与文档）。
- 不把整个开发仓库打成安装包。

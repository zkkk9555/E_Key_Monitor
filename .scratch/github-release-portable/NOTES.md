# NOTES — github-release-portable

Skills called: to-spec(inline)→tdd→implement(inline)→code-review

开工卡｜任务：把便携 EXE 打成 GitHub Release，游戏机直接下载即用｜slug：github-release-portable｜lane=B｜档位=full
本轮调用：to-spec(inline)→tdd→implement(inline)→code-review
调用面：to-spec=inline｜tdd=tool｜implement=inline｜code-review=inline（子代理往返成本高于收益，本轮自跑）
分流：publish 面 → 用户本轮明确要求发版（本人仓库、本人凭据、release 可删可撤）→ 按显式指令执行；Gate0 MISS @gh release list（零 release）｜setup done
正身：to-spec、tdd、implement、code-review｜必读集：Package_E_Key_Monitor.ps1:1-143、docs/agents/issue-tracker.md:34
链·产物：to-spec=.scratch/github-release-portable/spec.md｜tdd=发布物冒烟｜implement=脚本+README+说明书+release｜code-review=NOTES.md「review」节

## 发布前的事实

- 远端 `https://github.com/zkkk9555/E_Key_Monitor.git`，**公开仓库**，默认分支 main。
- `gh auth status`：已登录 `zkkk9555`，scopes = gist / read:org / repo / workflow（repo 足够推 tag 与建 release）。
- `git tag` 空、`gh release list` 空 → 这是**第一个**版本。
- 本机没有 E 盘、没有游戏运行环境；便携包与冒烟都在本机做，真游戏留给用户。

## 冒烟与验收

```text
打包脚本产出：C:\Project\artifacts\E_Key_Monitor-portable\ = EXE + config.json + 说明书（三件，脚本自带断言）
ZIP：C:\Project\artifacts\E_Key_Monitor-portable.zip（65,685,925 B）
EXE SHA-256：E5B626766A48FD5007EB0EE31538A3DC896182E7701C8EEDBCE706D4BC372A47
冒烟：把**要发的那个 EXE** 复制到临时目录 + 配 config -> 进程存活、窗口可见 -> 结束
```

## 发布过程中的一个坑（已收拾干净）

第一次 `gh release create`（前台同步执行）撞上调用时限被砍，但它**已经把 release 建出来了**，只是一个 draft、附件没传完；第二次后台执行又建了一个同 tag 的正式版。结果 GitHub 上短暂存在**两个 v1.0.0**：一个 draft（assets=0）、一个已发布（assets=2）。

已用 `gh api -X DELETE repos/zkkk9555/E_Key_Monitor/releases/420183039` 删掉那个空 draft，回读确认现在只剩**一个** v1.0.0（draft=false, assets=2），tag 仍指向 45a9ef5，`/releases/latest` 正常。

## 顺手修的真问题

`dotnet` 不在 PATH 时 `Package_E_Key_Monitor.ps1` 直接崩（这台笔记本就是如此，用户在这台机器上打包会撞上）。改成：先找 PATH，再找 `%ProgramFiles%\dotnet\dotnet.exe`，都没有才给中文报错。修后**不带 PATH 重跑打包成功**，且 EXE 的 SHA-256 与之前一致（可重复构建）。开发脚本 `Start_Naraka_With_Monitor_Dev.ps1` 同步。

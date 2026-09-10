# 05: Configurable observe key

**Type:** task
**What to build:** `config.json` 的 `key` 生效（A-Z/0-9，不区分大小写），非法值回退 E。
**Anchors:** `MainForm.cs :: HookCallback`, `MainForm.cs :: MonitorSettings.Merge`, `README.md :: 配置文件`, `AGENTS.md :: 配置读取和保存`
**Blocked by:** None
**Status:** resolved

- [x] 纯函数解析：单字母/数字 → 对应 VK；空/多字符/非法 → E
- [x] Hook 回调观察配置键，不再硬编码 `VkE` 比较
- [x] 非法回退写诊断日志；托盘提示显示当前键
- [x] 仍只被动观察，不产生/拦截输入（安全边界不变）
- [x] README 与 AGENTS 的 `key` 说明同步更新（不再写“保留未用”）

## Answer

实现：`MonitorSettings.TryParseObserveKey` 纯函数（A-Z/0-9 不分大小写，非法回退 E）；`Merge` 归一化并回填 `ObserveVkCode`；`MainForm` 构造读取并记诊断日志，`HookCallback` 改比 `_observeVkCode`，托盘标题显示当前键。安全边界不变（仍只被动观察）。verify：`dotnet build -c Release --no-restore` 0 警告 0 错误。

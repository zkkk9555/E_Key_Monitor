# 只观察，不干预输入

Status: accepted

`E_Key_Monitor` 只用 `WH_KEYBOARD_LL` 被动读取按键、用 `Process.GetProcessesByName` 观察进程是否存在，从不发送、拦截或修改输入，也不读游戏内存、不注入进程。

这是产品的底线而不是实现细节：工具的价值在于「如实告诉用户宏还在按 E」，一旦它能改输入，就可能被当成按键宏的一部分，也会被反作弊规则盯上。

## Considered Options

- 「顺便做个连点/自动拾取」——被否：越过了观察边界，产品定位也变了。
- 用轮询 `GetAsyncKeyState` 代替 Hook——被否：轮询会漏掉极短按键，且更耗电；Hook 回调只记一个时间戳，开销恒定。

## Consequences

- Hook 回调里不能做磁盘、计算、网络或 UI 操作，只能记时间戳。
- 静态检查（`tools/verify-conventions.ps1` 的 `safety-boundary`）会拦输入模拟 API、鼠标 Hook、进程注入和直启游戏本体。

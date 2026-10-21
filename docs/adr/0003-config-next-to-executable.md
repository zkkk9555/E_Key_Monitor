# 配置跟着 EXE 放，不放用户目录

Status: accepted

程序读写的是 `AppContext.BaseDirectory\config.json`，也就是正在运行的 EXE 旁边的文件；根目录的 `config.json` 只是构建时复制过去的模板。

这样整个便携文件夹是自包含的：复制、删除、换机器都只动一个文件夹，配置跟着程序走，不会出现「设置存在哪、哪个生效」的歧义。

## Consequences

- 目标目录需要写权限；只读部署（例如从 U 盘或受保护目录运行）保存会失败，失败只记诊断、不崩溃。
- `config.json` 里存的是绝对路径（游戏目录），换机器后需要重新选一次。
- 文件带中文游戏路径，必须保持 UTF-8 with BOM，程序写回时也带 BOM，以免 Windows PowerShell 5.1 读成乱码。
- 「当前运行的是哪个 EXE」永远是确定答案：`AppContext.BaseDirectory`。

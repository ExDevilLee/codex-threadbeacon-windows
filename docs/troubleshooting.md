# ThreadBeacon for Windows 故障排查

[English](troubleshooting-en.md)

本文适用于从 GitHub Releases 下载的 Windows 11 技术预览版。开始前请确认 Codex Desktop
或 Codex CLI 已经运行过至少一个任务。

## Windows 阻止或警告运行 EXE

当前预览包尚未使用商业代码签名证书，SmartScreen 可能提示未知发布者。请只从本仓库
GitHub Release 下载，并核对 Release 标签。不要关闭 Defender、SmartScreen 或执行安全
绕过命令。对来源有疑问时停止运行并从 Release 页面重新下载。

## 窗口中没有任务

1. 在 Codex Desktop 或 Codex CLI 中运行一个真实主任务。
2. 确认标题栏没有开启“仅显示收藏”，任务也没有被临时忽略。
3. 确认自动监听没有暂停，或点击手动刷新。
4. 打开右下角数据源健康入口，检查“任务数据库”。

默认只显示最近主任务，不把 Subagent 作为独立主行。数据库不可用时，确认当前用户的
`%USERPROFILE%\.codex\state_5.sqlite` 存在；不要编辑、替换或上传该文件。若使用自定义
Codex 目录，请确认 `CODEX_HOME` 或 `CODEX_SQLITE_HOME` 指向有效位置。

## 标题、状态或 Token 没有及时变化

- Rename 索引不可用时会回退数据库标题，并在健康入口显示降级。
- 未闭合 turn 超过 120 秒无新事件时显示 `unknown`，避免长期误报运行中。
- `justCompleted` 保留约 60 秒后变为 `idle`。
- 暂停监听不会自动刷新，但手动刷新仍可用。
- Rollout 只读取文件尾部并可能在文件升级或轮转时降级。

请报告健康状态类别和成功／失败计数，不要附加 `session_index.jsonl` 或 rollout 文件。

## 没有出现服务异常或提示音

服务异常只来自白名单结构化日志中的 HTTP 400/429/503 和明确模型容量错误，不从正文、
静默或普通超时猜测。提示音问题请依次检查 Settings 总开关、事件开关、试听和系统音量。
自定义 WAV 被移动、删除或格式无效时会回退内置声音；启动、手动刷新或恢复监听不会补播
历史事件。

## 登录时启动无效

设置只写入当前用户的
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run\ThreadBeacon`。确认固定安装文件仍位于
`%LOCALAPPDATA%\ThreadBeacon\ThreadBeacon.App.exe`，并在 Settings 关闭后重新开启。不要改用
管理员级启动项或来源不明的计划任务。

## 升级、回滚与卸载

App 只提醒更新，不自动下载或安装。升级或回滚时退出 ThreadBeacon，从 GitHub Releases
取得目标版本，并替换 `%LOCALAPPDATA%\ThreadBeacon` 中的文件。设置保存在同一目录下的
JSON 文件中，不承诺所有未来版本完全向后兼容。

卸载前在 Settings 关闭登录时启动，退出 App，然后删除 `%LOCALAPPDATA%\ThreadBeacon`。
ThreadBeacon 不安装驱动、daemon 或管理员级服务。

## 提交 Issue 前

可以安全提供 Release 版本、Windows 11 版本、CPU 架构、Codex 版本、数据源健康类别和经过
脱敏的空白环境复现步骤。请勿公开提供任务标题、任务 ID、会话正文、reasoning、
`state_5.sqlite`、`logs_2.sqlite`、rollout、用户名、绝对路径、request ID、供应商 URL、
Token、Cookie、凭据、完整终端日志或未脱敏截图。安全漏洞请遵循 [`SECURITY.md`](../SECURITY.md)。

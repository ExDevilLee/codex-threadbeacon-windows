# ThreadBeacon for Codex on Windows

简体中文 | [English](README-EN.md)

ThreadBeacon 是一个用于集中查看 Codex Desktop 与 Codex CLI 主任务状态的原生 Windows 小窗口。

本项目是 [ThreadBeacon for macOS](https://github.com/ExDevilLee/codex-threadbeacon-macos) 的独立 Windows 平台实现。它是非官方社区工具，与 OpenAI 无隶属或背书关系。`Codex` 是其相应权利人的商标。

## 当前状态

项目处于 Windows POC 阶段。Win11 实机探测已经确认当前 Codex 版本的核心数据链路可用，但 Codex 本地文件格式不是公开稳定契约。

首版主链路 POC 已贯通：以短生命周期、无连接池的 SQLite read-only 连接读取最近 8 个未归档主任务并排除 Subagent；共享读取 `session_index.jsonl`，为每个任务选择最后一条有效 rename 标题；每个 rollout 最多读取文件尾部 2 MiB，只提取事件类型、时间和 Token 数字字段，用于推导 `running`、`justCompleted`、`idle` 与 `unknown`。统一 Loader 将这些数据合并为任务快照，WPF 窗口显示状态灯、标题、累计 Token 和状态持续时间，每 2 秒自动刷新并支持手动刷新。各数据源异常时会安全降级。

当前 WPF App 已接入本机真实任务数据。Win11 实机已完成超过 30 分钟的并行任务只读稳定性验证：900 次采样无探测失败、无数据源降级、无 App 崩溃，且未阻塞 Codex 写入。验证结果见 [Windows 30 分钟稳定性记录](docs/validation/2026-07-18-windows-30-minute-soak.md)。

已完成的窗口增强：右上角图钉按钮可让 ThreadBeacon 保持在其他普通窗口之前；置顶状态会保存到本机设置，并在重启后恢复。

右键主任务可置顶或忽略。状态优先级始终高于任务置顶，同一状态内置顶任务优先；普通忽略会在该任务出现新 turn 时自动恢复。存在已忽略任务时，标题栏会显示管理按钮，可逐项恢复或全部恢复。任务规则只在本机保存任务 ID、忽略时间和规则类型，不写入标题，也不修改 Codex 数据。任务级置顶与窗口“钉在最前面”相互独立。

标题栏中间的暂停/恢复按钮可临时停止每 2 秒自动监听。暂停期间仍可手动刷新；恢复时会立即刷新一次，App 重启后默认恢复自动监听。该控制只影响 ThreadBeacon 的本地只读刷新，不会暂停 Codex 任务。

累计 Token 后的信息按钮可查看会话总量、输入、缓存输入、非缓存输入、输出、Reasoning、当前 turn、缓存率和更新时间。悬停会短暂显示详情，点击可固定弹窗；任务列表每 2 秒刷新时，已打开的固定弹窗保持稳定。

右上角扬声器按钮可配置任务完成提示音，并提供与 macOS 版本一致的 Beacon、Chime、Pulse、Alert、Resolve 和 Knock 六种内置音色及试听功能。新安装默认使用 Chime 作为完成音、Alert 作为 429/503 异常音，两类通知都可独立选择任一种音色。只有自动刷新发现新的可靠 `task_complete` 事件时才播放一次；同一批多个完成事件会合并为一次提示。App 启动、手动刷新和恢复监听时只建立完成事件基线，不会补播历史任务。声音开关、所选音色和最多 256 个派生事件 ID 保存在本机，不保存任务标题、正文、Token 详情或 Codex 路径。

App 现在也会监控当前可见主任务的 HTTP 429/503 服务异常。重试阶段以黄色“服务异常”显示 HTTP 状态与重试进度，重试耗尽后变为红色“服务失败”，同一 turn 后续 HTTP 200 或较新的 rollout 生命周期事件会清除旧异常。每个异常 episode 最多播放一次独立可配置的提示音；异常与完成事件共享基线和 256 条本地派生 ID 历史。

创建过 Subagent 的主任务会在标题右侧显示中性的分支图标和直接 Subagent 总数。该数字来自父子关系表，是历史关系数量，不代表当前正在运行的数量；数量为 0 时不显示且不保留空白。点击数量可在主任务下行内展开直接子任务，默认显示 `Agent 别名 | 标题`、推导状态、最近活动和累计 Token，详情中显示角色、模型、Reasoning 与 Token 数字明细。只有当前可见且已展开的主任务会读取子任务记录和 rollout 尾部；收起后停止读取，不显示第二层子任务，也不读取正文。

窗口副标题显示 `运行中任务数/当前显示总数`，例如 `1/7`。分子只统计派生状态为 `Running` 的主任务，分母与列表中当前显示的主任务快照一致；暂停监听时保留上次成功刷新结果，手动刷新或恢复监听后重新计算。

第一阶段严格收敛为：

- 读取最近 8 个未归档主任务并排除 Subagent。
- 优先显示 `session_index.jsonl` 中 rename 后的标题。
- 从 rollout JSONL 尾部推导状态并显示状态灯。
- 显示会话累计 Token，并提供只包含数字统计的 Token 详情弹窗。
- 对自动刷新发现的新任务完成事件播放可配置的内置提示音。
- 从只读本地日志中识别当前可见主任务的 HTTP 429/503 重试和最终失败。
- 在主任务标题右侧显示非零的直接 Subagent 历史总数，并支持按需行内展开直接子任务。
- 在窗口副标题显示运行中主任务数与当前显示总数。
- 支持主任务右键置顶、临时忽略、下一 turn 自动恢复和手动恢复。
- 每 2 秒自动刷新并支持手动刷新。
- SQLite 全程只读。
- 不读取正文、不访问网络、不修改 Codex 数据。

其他失败/警告事件提示音、Subagent 异常提示与 Token 聚合和系统托盘仍未实现。

## 技术栈

- .NET 9
- WPF
- xUnit

## 仓库结构

- `src/ThreadBeacon.Core`：模型、只读数据访问、解析与状态规则，不引用 WPF。
- `src/ThreadBeacon.App`：Windows 窗口、交互和平台集成。
- `tests/ThreadBeacon.Core.Tests`：核心规则与数据兼容性测试。
- `tests/ThreadBeacon.App.Tests`：本机设置与窗口交互状态测试。
- `tools/ThreadBeacon.Probe`：只输出数据源健康状态和任务数量的本机探测工具。
- `docs`：Windows 数据探测和设计记录。

macOS 仓库只作为行为参考，不建立源码级依赖。

## 构建与运行

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet run --project src/ThreadBeacon.App
dotnet run --project tools/ThreadBeacon.Probe --configuration Release
```

## App 图标

<p align="center">
  <img src="Resources/AppIcon-1024.png" width="160" alt="ThreadBeacon App 图标">
</p>

Windows App 与 macOS 版本共享 `B1 Graphite / Code Beacon` 图标：石墨黑圆角底板、白色代码括号和纵向红黄绿三灯。

- `Resources/AppIcon-1024.png`：跨平台 1024px PNG 母版。
- `Resources/AppIcon.ico`：包含 16、24、32、48、64、128 和 256px 帧的 Windows 图标。

可在 PowerShell 中重复生成 ICO：

```powershell
.\script\generate_app_icon.ps1
```

## 提示音资源

Beacon、Chime、Pulse、Alert、Resolve 和 Knock 是由作者项目脚本确定性生成的短音效，不来自第三方音效包。Windows 直接复用与 macOS 相同的 44.1 kHz、单声道、16-bit PCM WAV，Release 构建会将它们复制到 `Resources/Sounds`。

## 隐私原则

- 只读取本机 Codex 数据，不修改 SQLite、session index 或 rollout 文件。
- 不读取或显示用户消息、助手回复正文、reasoning summary 或完整请求；429/503 监控只短暂解析三类白名单日志，并明确排除可能包含请求上下文的 transport 日志。
- 不启动网络服务，不上传任务数据。
- 数据源缺失、锁定或升级时安全降级，不影响 Codex 正常写入。

完整数据范围和处理边界见 [PRIVACY.md](PRIVACY.md)。

## License

[MIT](LICENSE)

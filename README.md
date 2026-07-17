# ThreadBeacon for Codex on Windows

简体中文 | [English](README-EN.md)

ThreadBeacon 是一个用于集中查看 Codex Desktop 与 Codex CLI 主任务状态的原生 Windows 小窗口。

本项目是 [ThreadBeacon for macOS](https://github.com/ExDevilLee/codex-threadbeacon-macos) 的独立 Windows 平台实现。它是非官方社区工具，与 OpenAI 无隶属或背书关系。`Codex` 是其相应权利人的商标。

## 当前状态

项目处于 Windows POC 阶段。Win11 实机探测已经确认当前 Codex 版本的核心数据链路可用，但 Codex 本地文件格式不是公开稳定契约。

当前已完成任务发现与标题链路：以短生命周期、无连接池的 SQLite read-only 连接读取最近 8 个未归档主任务并排除 Subagent；共享读取 `session_index.jsonl`，为每个任务选择最后一条有效 rename 标题，并在 rename 缺失或损坏时回退 SQLite 标题。各数据源缺失、忙碌或格式不兼容时会安全降级。rollout 状态和 Token 明细仍待接入。

第一阶段严格收敛为：

- 读取最近 8 个未归档主任务并排除 Subagent。
- 优先显示 `session_index.jsonl` 中 rename 后的标题。
- 从 rollout JSONL 尾部推导状态并显示状态灯。
- 显示会话累计 Token。
- 每 2 秒自动刷新并支持手动刷新。
- SQLite 全程只读。
- 不读取正文、不访问网络、不修改 Codex 数据。

提示音、窗口置顶、任务置顶/忽略、Subagent 展开、429/503 和系统托盘不进入第一阶段。

## 技术栈

- .NET 9
- WPF
- xUnit

## 仓库结构

- `src/ThreadBeacon.Core`：模型、只读数据访问、解析与状态规则，不引用 WPF。
- `src/ThreadBeacon.App`：Windows 窗口、交互和平台集成。
- `tests/ThreadBeacon.Core.Tests`：核心规则与数据兼容性测试。
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

## 隐私原则

- 只读取本机 Codex 数据，不修改 SQLite、session index 或 rollout 文件。
- 不读取或显示用户消息、助手回复正文、reasoning summary 或完整请求。
- 不启动网络服务，不上传任务数据。
- 数据源缺失、锁定或升级时安全降级，不影响 Codex 正常写入。

## License

[MIT](LICENSE)

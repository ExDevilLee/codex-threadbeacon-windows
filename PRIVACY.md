# 隐私说明

## 数据范围

ThreadBeacon 只在本机读取以下 Codex 数据：

- `state_5.sqlite` 中最近未归档主任务的 ID、标题、更新时间、归档状态、累计 Token、rollout 路径和直接父子关系；
- 用户展开主任务时，该主任务直接 Subagent 的标题、昵称、角色、模型、Reasoning effort、更新时间、累计 Token 和 rollout 路径；
- `session_index.jsonl` 中与当前主任务或已展开直接 Subagent ID 对应的最新 rename 名称；
- 对应 rollout JSONL 文件尾部最多 2 MiB 中的事件类型、时间戳和 Token 数字字段，用于推导状态和显示 Token 概览。

App 不提取用户消息、助手回答正文、reasoning summary、命令、工具输出、文件内容或完整请求，也不读取第二层及更深的子任务。

## 数据处理

- SQLite 使用短生命周期、无连接池、只读连接，并启用 `query_only`。
- 数据只在当前进程内存中用于生成界面状态。
- App 不上传数据、不启动网络服务、不写入或修改 Codex 数据。
- 只有当前可见且已展开的主任务会读取直接 Subagent；收起后停止请求这些记录。
- Subagent 展开状态和子任务元数据不会写入设置或其他持久化文件。
- App 仅在本地保存窗口置顶状态、提示音设置和最多 256 个派生完成事件 ID；不保存任务标题、正文、Token 详情或 Codex 路径。

## 已知边界

Codex 本地文件格式不是稳定的公开 API，未来版本可能改变表、字段或路径。数据缺失、数据库繁忙或格式不兼容时，ThreadBeacon 会显示降级状态或空结果，不会尝试修复或改写源数据。

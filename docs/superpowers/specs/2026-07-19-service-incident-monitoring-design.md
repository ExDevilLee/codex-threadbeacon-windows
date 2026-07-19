# HTTP 429/503 服务异常监控设计

## 目标

在不读取会话正文、不访问网络、不修改 Codex 数据的前提下，从 Codex 本地日志中识别当前可见主任务的 HTTP 429/503 重试与最终失败，并在列表和提示音设置中提供与 macOS `dd5ca08` 对齐的反馈。

本设计依据用户此前授权直接执行：Windows 功能与界面整体对齐 macOS，且不超出 macOS 已发布功能边界。

## 方案选择

采用只读查询 `logs_2.sqlite` 的方案。它与 macOS 已验证数据链路一致，能按 `thread_id` 精确关联任务，并可通过 SQL 白名单避免读取无关日志。

不采用以下方案：

- rollout 正文关键词推断：缺少稳定的重试事件，并会扩大正文读取范围。
- 启动独立 app-server：独立进程无法可靠观察 Codex Desktop 当前实例，且增加生命周期复杂度。

## 数据边界

只查询当前列表中最多 8 个未归档主任务的 ID，只允许以下目标：

- `codex_http_client::default_client`：仅接收 `Request completed` 且状态为 200、429 或 503 的记录。
- `codex_core::responses_retry`：仅接收带重试进度的采样重试记录。
- `codex_core::session::turn`：仅接收最终 `Turn error` 且状态为 429 或 503 的记录。

明确排除 `codex_http_client::transport`，因为它可能包含完整请求上下文。SQLite 使用只读连接和参数化任务 ID；原始 `feedback_log_body` 只在单次刷新解析期间存在。进入 `ThreadSnapshot` 的数据仅包括 episode ID、阶段、HTTP 状态码、重试次数/上限和发生时间。

路径通过 `CodexDataPaths.LogDatabase` 解析，为后续 `CODEX_HOME`、`CODEX_SQLITE_HOME` 保持接口空间，不写死 Windows 用户目录。

## 解析与状态规则

每个服务异常 episode 由日志中的 `turn.id` 或 `turn_id` 标识：

- 429/503 请求完成事件建立异常候选。
- 同一 episode 的重试事件更新 `attempt/limit`，阶段为 `Retrying`。
- 同一 episode 后续 HTTP 200 清除活动重试。
- 同一 episode 的 429/503 `Turn error` 将阶段升级为 `Failed`。
- 每个任务只保留发生时间最新的活动 episode。
- rollout 中更新的 `task_started` 晚于异常时，清除旧异常。
- 对重试阶段，rollout 中更新的 `task_complete` 晚于异常时也清除旧异常。

活动异常覆盖 rollout 推导状态：`Retrying` 映射为黄色 `Warning`，`Failed` 映射为红色 `Error`。异常发生时间作为状态起点；异常活动时清空完成事件，避免错误播放任务完成提示音。

## 界面

主任务行沿用现有布局，不增加新列：

- 重试中显示 `服务异常 · HTTP 429/503 · 重试 n/limit · 持续时间`。
- 最终失败显示 `服务失败 · HTTP 429/503 · 持续时间`。
- 缺少状态码或重试进度时省略对应片段，不显示占位符。

刷新仍为 2 秒一次，行对象原位更新，不能让 Token 详情弹窗或 Subagent 展开状态因刷新消失。

## 提示音

复用现有 Beacon、Chime、Pulse 三种音效，不引入 macOS 后续提交中的新增音效。设置中新增独立的 `429/503 服务异常` 开关和声音选择，默认启用并选择 Chime。

同一任务、同一 episode 只记录一次 `warning:{threadId}:{episodeId}`。首次基线刷新只记录不播放；后续新 episode 播放一次。重试升级为最终失败不重复播放。异常关闭或全局提示音关闭时仍记录事件，避免重新开启后补播历史异常。完成事件与异常事件共享最多 256 条历史。

## 错误处理

日志库缺失、正在迁移、锁定、结构变化或包含无效行时，异常源降级为空；主任务、标题、状态、Token 和 Subagent 继续刷新。声音播放和设置保存失败同样不能中断刷新。

## 测试与验收

- 纯解析测试覆盖 429、503、重试、最终失败、200 恢复、非法目标和无效格式。
- SQLite 临时库测试覆盖只读查询、任务 ID 过滤、SQL 白名单和缺失数据库降级。
- 加载器测试覆盖状态覆盖、两类清除规则、完成提示抑制与日志失败降级。
- ViewModel 和提示音测试覆盖显示文本、基线、不重复、开关和共享历史上限。
- Release 构建及真实 `logs_2.sqlite` 运行验证不得阻塞 Codex 写入。

## 非目标

本阶段不监控其他 HTTP 状态、认证等待、超时或网络错误，不读取错误正文之外的请求内容，不加入托盘、系统通知或 macOS 后续新增音效。

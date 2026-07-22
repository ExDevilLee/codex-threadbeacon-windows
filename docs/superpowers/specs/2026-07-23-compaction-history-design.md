# 压缩历史统计设计

## 目标

为 Windows 版本增加与 macOS 对齐的只读压缩历史统计：在每个任务的 Token 详情中显示累计压缩次数和最近完成时间。本阶段不修改 Codex 配置，不读取压缩摘要、正文、Reasoning 或 transcript，也不显示实时“压缩中”状态。

## 数据流

rollout JSONL -> `RolloutTailParser` -> `RolloutObservation` -> `ThreadSnapshot` -> `TokenDetailViewModel`

压缩事件识别两种已观测格式：顶层 `compacted`，以及 `event_msg` 的 `context_compacted`。每条记录只保留事件时间和去重所需的内部信息；成对事件在同一时间窗口内只计数一次。解析异常、缺少时间戳或未知结构安全忽略。

## 状态与兼容性

- 新增 `CompactionHistory` 值对象，默认次数为 0、最近时间为空。
- 旧 rollout 没有压缩事件时详情显示 `0` 和 `-`。
- 压缩历史不改变现有任务状态排序或自动恢复资格。
- `ThreadBeacon.Core` 保持无 WPF 依赖，SQLite 和 rollout 仍只读。

## 验收

- 单一 `compacted` 事件计为一次。
- `compacted` 与紧邻 `context_compacted` 成对事件只计一次。
- 两次独立压缩计为两次，并保留较新的完成时间。
- malformed JSON、无效时间戳和压缩摘要不会导致列表刷新失败，也不会被保存或展示。
- Token 详情在只有压缩历史而没有 Token/model 数据时仍可打开。

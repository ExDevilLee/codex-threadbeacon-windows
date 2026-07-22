# 实时压缩 Hook 设计

## 目标

在 Windows 上补齐 macOS 的可选实时“压缩中”状态。用户在设置中明确启用后，ThreadBeacon 合并 `%CODEX_HOME%\hooks.json`，注册 `PreCompact` 和 `PostCompact`，并通过独立 Hook Bridge 写入最小活动标记。未启用 Hook 时，第一阶段已经完成的历史压缩统计继续可用。

## 数据边界

Hook 只保留 `schemaVersion`、`sessionId`、`turnId`、`trigger` 和本机生成的 `startedAt`。不读取或写入会话正文、压缩摘要、Reasoning、工作目录或 transcript。活动文件按 session ID 隔离，使用临时文件和原子替换。

## 配置安全

- 解析 JSON，不使用字符串拼接。
- 写入前创建当前用户目录内的备份。
- 拒绝符号链接、非 JSON、无权限和 `config.toml` 内联 Hooks 并存的情况。
- 写入前重新读取并校验源文件摘要，检测并发修改后停止。
- 卸载时只删除 ThreadBeacon 自己的两个处理器，保留其他 Hook。
- 不绕过 Codex Hook trust；界面明确提示用户仍需在 Codex 中审核信任。

## 状态规则

- 有效活动标记且任务未归档、无服务异常时，任务状态显示为 `Compacting` / `压缩中`，排序与 Running 相同。
- 15 分钟 TTL 是崩溃兜底，不用于估算进度。
- 更新的完成或中断证据会清除活动标记。
- 服务异常和归档状态优先于实时压缩状态。

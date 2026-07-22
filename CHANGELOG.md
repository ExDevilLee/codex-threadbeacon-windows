# Changelog

ThreadBeacon for Windows 的重要用户可见变更记录在此文件中。

格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循
[Semantic Versioning](https://semver.org/lang/zh-CN/)。

## [Unreleased]

## [0.21.1] - 2026-07-23

### Fixed

- Restored exhausted stream-disconnect detection for current Codex log targets,
  so a failed 5/5 reconnect is shown as a service failure instead of a recently
  completed task.

### Privacy

- Transport logs remain restricted to exact retry and final-error signatures.
  ThreadBeacon ignores lower-level SSE failures that do not contain a task ID
  and never retains request URLs or log bodies.

## [0.21.0] - 2026-07-23

### Added

- Added an opt-in Codex Hook bridge for live `Compacting` / `压缩中` task status.
- Added structured install, status check, and selective uninstall controls to
  the General settings tab.
- Bundled the self-contained Hook Bridge in both portable and single-file
  release outputs.

### Security

- Existing `hooks.json` content is parsed and merged structurally, backed up,
  written atomically, and never overwritten after a concurrent change.
- Unsafe files, malformed JSON, and inline `config.toml` Hooks fail closed.

### Privacy

- Activity markers contain only session ID, turn ID, trigger, and local start
  time. Hook payload fields such as transcript path, working directory, model,
  conversation text, summaries, and Reasoning are not retained.

## [0.20.0] - 2026-07-23

### Added

- Added read-only rollout compression history to Token details, including the
  completed compression count and latest completion time.
- Deduplicated adjacent `compacted` and `context_compacted` records without
  retaining compression summaries or conversation content.

### Privacy

- This phase does not install or modify Codex Hooks and does not read or store
  compression summaries, conversation text, Reasoning, paths, or transcripts.

## [0.19.0] - 2026-07-22

### Changed

- Subagent 展开行优先使用 `agent_path` 最后一段生成表意别名，例如将 `/root/fix_external_sync` 显示为 `Fix external sync`；缺少该字段的旧记录继续回退到 Agent nickname。
- 表意别名采用更宽但有上限的显示区域，并按单词截断，在最小窗口宽度下仍为任务标题保留空间。

### Compatibility

- 查询 Subagent 前会先检测 `agent_path` 列；旧版 Codex 数据库缺少该列时返回空值而不是让整个 Subagent 数据源失败。

### Privacy

- `agent_path` 只在当前刷新生成的内存快照中使用，不写入 ThreadBeacon 设置、历史或日志。

## [0.18.0] - 2026-07-22

### Added

- 识别 rollout 中结构化的用户中断事件，主任务与 Subagent 会显示中性的“已中断”状态，并在后续新 turn 开始后恢复为运行中。
- 色盲安全状态标识新增独立停止符号，中英文、浅色和深色主题均使用同一状态语义。

### Fixed

- 中断时间优先使用有效的 ISO `completed_at`，字段为数字或格式异常时安全回退到事件顶层时间戳，避免漏判整条中断事件。
- 已中断状态不会重放历史完成提示音，也不会创建自动恢复候选。

### Privacy

- 中断探测只保留事件类型和时间戳，不读取或保存中断原因之外的正文、回复或任务内容。

## [0.17.0] - 2026-07-22

### Added

- 无人值守自动恢复现在会记录原前台窗口；发送成功、失败或取消后，仅当当前前台仍是本次 Codex 进程时才尝试恢复原应用。

### Security

- 前台恢复使用窗口句柄、PID 与进程启动时间共同校验；用户主动切换应用、原窗口退出、PID 复用或 Codex 身份不唯一时均安全跳过。
- 应用身份只存在于单次恢复过程的内存中，不写入设置、历史或日志。

## [0.16.0] - 2026-07-22

### Added

- “通用”设置新增默认关闭的色盲安全状态标识，开启后主任务与 Subagent 会同时使用颜色、形状和文字区分七种状态。
- 状态符号使用固定尺寸槽位，切换开关不会改变列表列宽；中英文、浅色和深色主题均即时生效。

### Fixed

- 显示设置保存时始终保留当前语言与主题，避免即时切换后由其他设置写回旧偏好。

## [0.15.0] - 2026-07-22

### Added

- 主任务 Subagent 徽标改为 `运行中数/历史总数`，例如 `2/27`；折叠状态也会随刷新更新。
- 活跃数只统计最近 120 秒内由 rollout 确认仍为 Running 的直接 Subagent，展开时复用同轮解析结果。

### Privacy

- 活跃计数只读查询最近更新的直接子任务 ID、rollout 路径和时间，不读取标题、模型、Token 或正文。

## [0.14.0] - 2026-07-22

### Added

- 主任务详情现在显示模型与推理强度；SQLite 字段逐项优先，缺失时由 rollout 最近有效的 `turn_context` 补充。
- 只有模型或推理强度而没有 Token 数据时仍可打开详情；详情弹窗继续支持悬停、点击固定与中英文即时切换。

### Privacy

- 新增字段仅为模型名称与推理强度，不读取或保留消息正文、回答、reasoning summary 或工具输出。

## [0.13.1] - 2026-07-22

### Fixed

- 已归档的收藏观察任务不再响应双击打开；未归档主任务、Subagent 展开和 Token 详情交互保持不变。

## [0.13.0] - 2026-07-22

### Added

- 识别同一 turn 中重连次数耗尽后出现的最终连接中断，显示“连接中断 · 重试 5/5”，并增加独立的自动恢复规则和中英文默认提示词。
- 保持自动恢复总开关默认关闭；单独的 `5/5` 仍为重试警告，缺少耗尽证据的断流文本不会被误判为终止失败。

### Security

- 最终断流仍只从白名单日志 target 只读解析；任务快照、界面、设置和恢复记录均不保留日志正文、URL 或本机路径。

## [0.12.1] - 2026-07-21

### Added

- 支持页同步 macOS 版本的微信支付与支付宝自愿赞助二维码；赞助不解锁功能、不影响免费使用，付款二维码不进入 App 主窗口。
- 增加仓库完整性测试，校验两张二维码资源的 JPEG 格式、文档引用及自愿赞助原则。

## [0.12.0] - 2026-07-21

### Added

- 双击主任务行可通过任务 ID 深链在 Codex App 中打开对应任务；导航复用自动恢复的唯一窗口、空 composer、精确 Rename 标题和 composer 实例切换校验。
- 双击 Subagent 展开与 Token 详情按钮不会触发任务导航；打开任务不会输入文字或发送消息。

## [0.11.0] - 2026-07-21

### Added

- 增加默认关闭的自动恢复：仅对启动后新出现的终止型 HTTP 400、429、其他 HTTP 错误和模型容量异常发送可配置续接提示，HTTP 503 默认关闭。
- 使用任务 ID 深链、精确 Rename 标题、空编辑器和唯一发送按钮进行 Windows UI Automation 安全校验；发送只调用一次，并通过 rollout 中的精确用户消息与 `task_started` 事件确认结果。
- 设置窗口增加自动恢复规则、双语默认提示和最多 100 条隐私安全的本地恢复记录；记录不保存提示词、任务标题、Codex 路径或异常原文。

### Security

- SQLite、session index 和 rollout 数据源保持只读；自动恢复只在用户明确开启后通过已安装的 Codex App 提交配置提示，不直接调用 Codex 网络 API。

## [0.10.3] - 2026-07-21

### Fixed

- 切换界面语言时原地更新主题选项文字，避免主题下拉框暂时变为空白，并保持用户已选择的主题不变。

## [0.10.2] - 2026-07-21

### Fixed

- 缩小主窗口标题栏按钮间距，在最小窗口宽度且所有工具按钮可见时仍完整显示 `ThreadBeacon` 标题，同时保持原有按钮尺寸和点击区域。

## [0.10.1] - 2026-07-21

### Fixed

- 当 Codex 将用户可见的独立任务保留为 `subagent` 来源、但没有父子关系时，使用健康的 Rename 索引保守地将其恢复为主任务候选；真正的子任务和无法确认的孤立记录仍被排除。

## [0.10.0] - 2026-07-20

### Documentation

- 增加贡献、安全报告、双语故障排查、Issue Forms 和 Pull Request 模板。
- 所有公开反馈入口明确禁止上传 Codex 数据文件、任务内容、本机路径或凭据。

## [0.9.0] - 2026-07-20

### Added

- About 增加 MIT License、项目支持入口和版权信息；支持页不包含付费渠道或功能解锁。

## [0.8.0] - 2026-07-20

### Added

- 与 macOS 对齐为八种内置声音，增加两个 CC0 可选音效和第三方来源说明。

### Fixed

- Release ZIP 增加重试与逐条完整性校验，避免发布瞬时文件占用造成的不完整归档。

## [0.7.0] - 2026-07-20

### Added

- 完成与服务异常可分别选择、试听和清除本地 WAV；无效文件自动回退内置声音。

## [0.6.0] - 2026-07-20

### Added

- 启动后静默检查 GitHub Release，底栏和 About 提供更新提醒与手动检查。

## [0.5.0] - 2026-07-20

### Added

- 单实例 About 窗口，展示版本、项目定位、非官方说明和公开项目链接。

## [0.4.0] - 2026-07-20

### Added

- 识别白名单结构化日志中的 HTTP 400 Bad Request，并显示红色服务失败状态。

## [0.3.0] - 2026-07-20

### Added

- 识别所选模型容量已满错误，显示红色失败并沿用异常提示音。

## [0.2.0] - 2026-07-20

### Added

- Settings 增加当前用户级“登录时启动”开关。

## [0.1.0] - 2026-07-20

### Added

- WPF 主窗口展示 Codex 主任务状态、rename 标题、Token、持续时间和运行数量。
- Subagent 数量及行内详情、收藏与归档关注、置顶、临时忽略、暂停监听和数据源健康。
- 完成与服务异常提示音、国际化、主题、窗口置顶及位置恢复。
- 自包含 Windows EXE、便携 ZIP、Git 标签和 GitHub Release 自动发布流程。

### Security

- 只读访问 Codex SQLite、session index、rollout 尾部和白名单日志；不读取正文、不修改
  Codex 数据、不上传本机任务信息。

[Unreleased]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.19.0...HEAD
[0.19.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.18.0...v0.19.0
[0.18.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.17.0...v0.18.0
[0.17.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.16.0...v0.17.0
[0.16.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.15.0...v0.16.0
[0.15.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.14.0...v0.15.0
[0.14.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.13.1...v0.14.0
[0.13.1]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.13.0...v0.13.1
[0.13.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.12.1...v0.13.0
[0.12.1]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.12.0...v0.12.1
[0.12.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.11.0...v0.12.0
[0.11.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.10.3...v0.11.0
[0.10.3]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.10.2...v0.10.3
[0.10.2]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.10.1...v0.10.2
[0.10.1]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.10.0...v0.10.1
[0.10.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.9.0...v0.10.0
[0.9.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.8.0...v0.9.0
[0.8.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.7.0...v0.8.0
[0.7.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.6.0...v0.7.0
[0.6.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.5.0...v0.6.0
[0.5.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/ExDevilLee/codex-threadbeacon-windows/releases/tag/v0.1.0

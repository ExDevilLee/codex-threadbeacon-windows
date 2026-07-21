# Changelog

ThreadBeacon for Windows 的重要用户可见变更记录在此文件中。

格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循
[Semantic Versioning](https://semver.org/lang/zh-CN/)。

## [Unreleased]

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

[Unreleased]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.12.1...HEAD
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

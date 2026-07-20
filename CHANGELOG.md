# Changelog

ThreadBeacon for Windows 的重要用户可见变更记录在此文件中。

格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循
[Semantic Versioning](https://semver.org/lang/zh-CN/)。

## [Unreleased]

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

[Unreleased]: https://github.com/ExDevilLee/codex-threadbeacon-windows/compare/v0.10.1...HEAD
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

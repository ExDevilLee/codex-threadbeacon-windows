# 参与 ThreadBeacon for Windows 开发

感谢你愿意改进 ThreadBeacon。项目优先保持状态窗口紧凑、只读、本地运行和隐私最小化，
不会为了功能数量把它扩展成另一个完整 Codex 客户端。English contributions are welcome.

## 开始之前

- Bug 请先阅读 [`中文故障排查`](docs/troubleshooting.md) 或
  [`English troubleshooting`](docs/troubleshooting-en.md)。
- 安全漏洞遵循 [`SECURITY.md`](SECURITY.md)，不要提交公开 Issue。
- 不要提交真实任务标题、任务 ID、会话内容、SQLite、rollout、日志、本机绝对路径或凭据。

## 本地开发

要求 Windows 11、.NET 9 SDK 和 Git。运行完整测试：

```powershell
dotnet test ThreadBeacon.slnx --configuration Release
```

生成自包含 EXE 和便携 ZIP：

```powershell
.\script\publish_release.ps1
```

## 修改原则

- 默认只读 `%USERPROFILE%\.codex`，不直接修改 Codex SQLite。
- 新数据源必须说明读取范围、稳定性、失败回退和隐私边界。
- 不从会话正文、静默或超时猜测状态。
- 不新增网络、写入或系统权限，除非需求、风险和替代方案已经讨论清楚。
- `ThreadBeacon.Core` 不引用 WPF；共享状态语义和测试场景，不与 macOS 建立源码依赖。
- UI 变化验证浅色、深色、最小窗口尺寸以及简体中文和 English。

## Commit 与 Pull Request

- 使用聚焦的 Conventional Commits，例如 `feat(settings): ...` 或 `fix(status): ...`。
- 提交前运行完整测试、`git diff --check` 和敏感信息检查。
- Pull Request 使用仓库模板填写验证证据、隐私影响和兼容性边界。
- UI 截图必须脱敏，不得包含真实任务、桌面内容或本机身份信息。

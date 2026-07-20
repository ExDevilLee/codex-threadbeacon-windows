# Pull Request Checklist

## 变更摘要 / Summary

- 请说明用户可见变化及其解决的问题。

## 验证 / Verification

- [ ] `dotnet test ThreadBeacon.slnx --configuration Release`
- [ ] 与改动相关的 App 构建或手动验证已完成
- [ ] `git diff --check`
- [ ] 推送前已检查敏感信息和生成产物

## 隐私与兼容性 / Privacy And Compatibility

- [ ] 未提交任务标题、任务 ID、会话内容、SQLite、rollout、日志、本机路径或凭据
- [ ] 新增数据读取、写入、网络或系统权限已列出；不涉及则注明“不涉及”
- [ ] Codex 本机数据格式假设及安全回退已说明；不涉及则注明“不涉及”

## 截图 / Screenshots

仅在 UI 变化时提供脱敏截图；不要显示真实任务、桌面内容或本机身份信息。

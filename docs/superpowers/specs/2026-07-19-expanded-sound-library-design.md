# 扩展提示音库设计

## 目标

将 Windows 提示音选项从 Beacon、Chime、Pulse 扩展为与 macOS `f213fad` 一致的六种音色：Beacon、Chime、Pulse、Alert、Resolve、Knock，并对齐新安装的默认选择。

本阶段沿用用户对 macOS 对齐功能的持续授权，不新增 macOS 尚未实现的行为。

## 方案

直接复用 macOS 仓库中由项目脚本确定性生成的三个 WAV 文件，并校验 SHA-256、RIFF/WAVE 格式、单声道、16-bit PCM 和 44.1 kHz。这样两端使用完全相同的音频资产，不需要在 Windows 增加另一套合成脚本或运行时音频生成器。

新增资源：

- `Done-Alert.wav`：下降警示音。
- `Done-Resolve.wav`：上升解决音。
- `Done-Knock.wav`：短促敲击音。

现有 App 项目已经用 `Resources/Sounds/*.wav` 通配符复制资源，因此无需修改项目文件。

## 设置与兼容性

`CompletionSound` 新增 Alert、Resolve、Knock。设置 ViewModel 的选项顺序固定为 Beacon、Chime、Pulse、Alert、Resolve、Knock，完成和异常两组下拉框共享这六项。

新默认值与 macOS 对齐：

- 任务完成：Chime。
- 429/503 服务异常：Alert。

已有 `sound-settings.json` 中保存的 Beacon、Chime 或 Pulse 会按原值读取，不进行迁移或覆盖。旧文件没有异常字段时，由属性默认值补为 Alert；格式错误或文件缺失时使用 Chime/Alert 新默认值。六种枚举值继续使用字符串序列化。

## 播放与界面

`WavSoundPlaybackService.GetSoundPath` 为三个新枚举映射对应文件名。播放失败继续返回 `false`，不能影响刷新或设置交互。

界面不新增控件、不改变弹窗尺寸或布局。两个 ComboBox 自动显示六个选项，试听按钮继续使用现有播放链路。

## 测试与验收

- ViewModel 测试固定六项顺序和 Chime/Alert 默认值。
- JSON 设置测试覆盖缺失、损坏、旧三音色设置兼容和六音色往返。
- 播放服务测试覆盖六种文件名映射。
- 资源测试检查六个输出 WAV 存在、非空、格式参数正确，新增三项哈希与 macOS 一致。
- Release 构建后实际打开设置弹窗，确认两个下拉框均显示六项并可试听新声音。

## 非目标

本阶段不改变提示事件规则、音量、不重复策略、系统混音、资源生成算法或提示音弹窗布局，也不加入 macOS 后续的任务置顶/忽略功能。

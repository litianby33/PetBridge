# 常见问题

| 现象 | 检查与处理 |
| --- | --- |
| 双击源码包没有 exe | 先完整解压，运行 `Build.cmd`，再 `Start.cmd`。Windows x64 需要 .NET Framework 4.8。 |
| PowerShell 不允许运行脚本 | 基础构建／启动可直接用 `.cmd`；也可让 Codex 在你批准的开发环境运行脚本。不要改系统安全策略来绕过提示。 |
| WebP 导入失败 | 运行程序本身不需要 Pillow；导入才需要。让 Codex 建 `.venv` 并装 `requirements-tools.txt`，或先提供 PNG。 |
| 没有跳跃动画 | 将 `jump.frameCount` 设为 0，保留 `fallback: idle`；人物会整体上移模拟跳。 |
| 人物不显示／格子越界 | 检查每格宽高、从 0 开始的行列、是否透明、素材路径是否位于角色文件夹内。不要把整张网格图当成一帧。 |
| 站立抖动 | 降低 `idle.fps`，保留 `alignIdleFeet: true`；尽量避免源图每帧尺寸或底线不一致。 |
| 没有注视方向 | `lookDirections.enabled: false` 即可；不用补画 16 张。 |
| 桥接未连接 | ChatGPT 桌面应用需运行；“绑定”页需要真实本机 Codex 对话编号；检查接收方是否安装了 app-tools 组件。旧版本可能需要 Codex 适配。 |
| 云端 Work 的提问未提醒 | 查询接口可能没有导出该异步问题；本项目不会从问号猜测。可在桌面原生活动／通知中查看。 |
| 额度和普通聊天不一样 | 面板读取的是 Codex 5 小时／周额度，不是所有 ChatGPT 模型共用的额度。 |
| 额度是 0 又弹提醒 | 1.0.0 只在每个确认结束任务提醒一次；定时刷新保持安静。不同任务结束可以各提醒一次。 |
| 更换角色未即时显示 | 退出并重新打开 PetBridge。`--character` 只作用于本次；`--set-character` 才保存为默认。 |
| 找不到配置 | `%LOCALAPPDATA%\PetBridge\profile`；如果设过 `PETBRIDGE_DATA_DIR`，就在该目录的 `profile`。 |
| 关闭后又自动出现 | 检查是否启用了 PetBridge 的随 ChatGPT 启动；只调整 PetBridge 自己的设置／快捷方式。 |

提交问题时优先给出：Windows 版本、软件版本、哪个功能、是否内置角色也能复现、**去掉个人路径的**错误摘要。不要公开原始聊天记录、完整运行配置、令牌或未授权角色素材。

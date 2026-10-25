# 下载者：交给自己的 Codex 适配

## 最省事的做法

1. 下载 Windows 体验包并完整解压到自己选的文件夹。源码包也可以，Codex 会先编译。
2. 在 Codex 中打开这个文件夹。把 `给Codex的提示词.txt` 的内容发给它，再附上自己的角色素材路径。
3. Codex 会读取 `AGENTS.md`，检查本机组件、绑定你自己的对话编号、导入角色、验证并预览。

这不是把别人的账户搬过来。程序使用你自己已安装、已登录的 Codex 组件；无需向作者提供 API Key。不要把 `.codex/auth.json`、Cookies 或整份对话目录上传给任何仓库。

## “绑定”到底是什么

本地任务监听读取本机 `CODEX_HOME/sessions` 中的状态事件。桌面应用兼容桥接还需要一个真实的本机 Codex 对话技术编号作为调用上下文。**绑定编号不是密码，也不会发送消息。** 它保存在你自己的 PetBridge 配置里，不在源码中。

可以让 Codex 从当前会话上下文／`list_threads` 核实，选择 `kind=codex`、`hostId=local` 的真实编号。也可在桌面版的本机 Codex 对话里复制深链接：Windows 默认快捷键 `Ctrl + Alt + L`，若改过快捷键请以应用设置为准。进入桌宠设置的“绑定”页粘贴并保存。

命令方式：先退出 PetBridge，再执行（把编号替换成你自己的）：

```powershell
.\scripts\Bind-Codex.ps1 -ThreadLink 'codex://threads/你的本机对话编号'
```

也可直接运行：

```powershell
.\PetBridge.exe --bind '你的本机对话编号'
```

GUI 程序的命令结果在 `%LOCALAPPDATA%\PetBridge\profile\setup-result.json`。使用 `Start-Process -Wait` 时会等待命令完成。不要用 `src/*Checks.cs` 中的 `1111…` 或 `2222…` 示例编号。

## 换成自己的角色

准备其中一种：

- `pet.json` 与 `spritesheet.webp` / PNG 的角色文件夹。
- 1536×1872 的 8 列 9 行精灵图，或 1536×2288 的 8 列 11 行精灵图。
- 一张透明静态 PNG；其它动作会用基本动作，跳跃自动模拟。
- 自定义网格图：告诉 Codex每格宽高、每段动作的位置；不要只凭文件名猜动画。

例如：

```powershell
.\scripts\Import-Character.ps1 -Source '你的角色文件夹' -Name '我的搭档'
```

导入工具只读取原文件，在 `%LOCALAPPDATA%\PetBridge\characters` 新建角色，不改原图。标准图自动生成逐项带 `purpose` 的 `character.json`。非标准图需让 Codex 检查并调整动画映射。

WebP 导入需要 Python + Pillow。优先使用现有 Codex 依赖或项目 `.venv`；没有时让 Codex 执行：

```powershell
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements-tools.txt
```

应用日常运行本身不需要 Python。

## 不想立刻保存为默认角色

```powershell
.\PetBridge.exe --character '角色目录\character.json' --offline
```

这只覆盖本次运行的角色。退出后不会把它设成默认。`--set-character` 或设置里的角色选择才会保存。

## 检查结果

让 Codex 至少检查：基础站立、思考持续循环、缺动作回退、三次提醒跳跃、左右露半身、提问与完成气泡、点击回桌面。可以在设置的“测试”页预览，不用发送真实聊天。

桌面桥接有版本差异：没有安装 app-tools、应用没运行或云端没有导出等待状态时，需要据实说明。只在确认为终态后报告完成；网络断开、等待回答和普通文字中的问号不等于完成或需要你选择。

参考：[官方深链接与快捷键](https://learn.chatgpt.com/docs/reference/commands)。

## 还想让 Codex 改人物形象？

见 [角色定制与可复制提示词](CUSTOMIZE-CHARACTER.zh-CN.md)：可以换现成素材、改配色／手部／服装细节，或确认站立设计后再生成动作。先保留原图和确认生成范围，不需要把私人角色上传到 GitHub。

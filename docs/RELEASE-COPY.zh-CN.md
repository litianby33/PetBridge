# GitHub 项目介绍与发布文案

## 仓库 Description

ChatGPT 桌面端宠物：用 Codex 制作，支持任务与提问提醒、Codex 额度、像素动画和自定义角色。

## 推荐 Topics

`desktop-pet` `windows` `codex` `chatgpt` `pixel-art` `desktop-companion` `csharp`

## 发帖 / 转发介绍

做了一个 ChatGPT 桌面端宠物，项目叫 PetBridge。

起初觉得官方宠物与任务状态的互动还不够贴合自己的习惯，于是用 Codex 做了这个项目：工作时陪你思考，需要回答时叫你，完成后弹气泡，还能随手看 Codex 额度。

支持左右贴边、胶囊工具条、快捷聊天和详细外观设置。附带灰黑微紫像素 ChatGPT 娘，也可以换成自己的角色，或把项目交给自己的 Codex 帮忙适配、修改形象。

Windows 体验包、源码、动作演示和中文适配指南都已备好。PetBridge 是独立社区项目，非 OpenAI 官方产品；代码 MIT，角色和标识另见素材说明。

## Release 标题

ChatGPT 桌面端宠物 v1.3.2

## Release 正文

让自己的角色，陪你聊天和完成任务。

### 为什么做这个

起初使用官方宠物时，我觉得它还不够懂我的工作状态：我希望它能跟着任务思考、在需要回答时叫我、完成后告诉我，也能随手看一眼剩余额度。于是，我用 Codex 做了这个更贴合自己使用习惯的 ChatGPT 桌面端宠物，并把它整理成可以自己适配、自己换角色的开源项目。

[功能与动作演示](https://github.com/litianby33/PetBridge#readme) · [本机适配指南](https://github.com/litianby33/PetBridge/blob/main/docs/CODEX-ADAPT.zh-CN.md) · [让 Codex 改角色](https://github.com/litianby33/PetBridge/blob/main/docs/CUSTOMIZE-CHARACTER.zh-CN.md)

### v1.3.2 新增

- **悬停查看 Token**：把鼠标放到任务三点上，查看累计 Token、最近请求上下文和估算输出均速。
- **选择正在进行的聊天**：点击用量卡片顶部的标题，切换进行中或最近的本机 Codex 聊天；选中后固定显示。
- **三点位置通过设置调整**：三点禁用鼠标拖动，悬停直接展开用量卡片。

聊天累计 5 小时 / 周额度消耗尚未接入；账号剩余额度胶囊不受影响。输出均速是本机采样估算，包含工具和等待时间。

### 它能做什么

- **陪你工作**：任务进行时持续思考，旁边用三点显示状态；完成或需要回答时弹出提醒。
- **随手看额度**：查看 Codex 的 5 小时、本周剩余额度和重置时间，支持阈值提醒，靠边自动变为竖排胶囊。
- **快捷开始聊天**：点击聊天框左侧图标选择 Codex 或普通 ChatGPT，也可用 Alt+1 / Alt+2 切换。
- **按习惯接收提醒**：Work、Codex 和普通聊天分别设置；气泡可选择点击收起或跳转对话。
- **安静待在桌边**：左右贴边探头，点击展开，悬停看简洁额度；支持临时隐藏和全屏避让。
- **调整成喜欢的样子**：字体、位置、阴影、气泡颜色和三点加载都可调，有 12 个详细设置页。
- **用自己的角色**：附灰黑微紫像素 ChatGPT 娘，包含六组动作和 16 向注视；支持更换角色，也可以让 Codex 帮你改形象。缺少动作时使用基础动作，没有跳跃时模拟跳。

### 下载与开始

| 附件 | 用途 |
| --- | --- |
| PetBridge-v1.3.2-windows-x64.zip | Windows 体验包：程序、源码、默认角色和文档 |
| PetBridge-v1.3.2-source.zip | 编译或修改代码的源码包 |
| SHA256SUMS.txt | 下载文件的 SHA-256 校验值 |

完整解压，进入 PetBridge 文件夹，双击 Start.cmd。右键人物 → 详细设置 → 测试工具，可以预览动作与提醒。绑定自己的环境或换角色，请阅读 README，再把“给Codex的提示词.txt”交给能操作本机项目的 ChatGPT / Codex。

### 使用说明

- Windows x64 / .NET Framework 4.8。角色导入工具需要 Python + Pillow。
- Codex 聊天在桌宠独立会话中回复；普通 ChatGPT 打开桌面草稿，仍需在桌面手动发送和查看回复。
- 额度显示的是 Codex 账号额度。普通 ChatGPT 联动与完成气泡默认关闭，Work / Codex 和提问提醒可分别调整。
- ChatGPT / Work 联动依赖本机桌面组件与版本；异步提问的识别范围取决于组件提供的状态。演示使用示例数据。
- PetBridge 是独立社区项目，非 OpenAI 官方产品。代码 MIT；生成角色与第三方标识见 ARTWORK.md / THIRD_PARTY_NOTICES.md。

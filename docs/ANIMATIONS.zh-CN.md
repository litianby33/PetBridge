# 动画绑定逐项说明

## 先分清：一张图、网格图、播放动作

精灵图是很多小画面拼在一张大图上。`frameWidth` / `frameHeight` 是**原图每格**的大小；`row` / `column` 是动作起点；`frameCount` 是从该列开始连续读取多少格。坐标从 0 开始，不是从 1 开始。

每个动作有 `purpose` 备注，运行器会忽略这段说明文字，但保留给你和 Codex 理解。`fps` 是每秒切换几张；像素画待机可从 2 fps 开始。`frameCount: 0` 或 `enabled: false` 表示没有这段动作。

## 当前随包角色（4 列）

默认 ChatGPT娘使用 4 列、10 行、256 像素方格：0 待机（2 fps）、1 向右（4 fps）、2 向左（4 fps）、3 招手（4 fps）、4 跳跃（7 fps）、5 思考（3 fps）；每段 4 帧。第 6–9 行为 16 个注视姿态。失败、等待和收尾没有另画，复用待机。`characters/default/character.json` 已逐段标明 `purpose`。

[离线动作预览](character-preview.mp4) 展示六段独立循环。它是检查动作的展示视频；桌宠跳跃次数、气泡碰撞及思考时机由运行时代码控制。

只有重新生成角色素材时才需 `tools/build_character_atlas.py`（Pillow）。它接收 4×6 动作图、4×4 注视图并输出 atlas、映射和布局报告。原始生成图放在自己的私有素材目录。`tools/render_character_preview.py` 可导出 MP4，需 FFmpeg 在 PATH，或额外安装可选 `imageio-ffmpeg`；日常运行不需要这些工具。

## 经典 8 列布局

| 原图行 | 绑定名 | 典型格数 | 这段什么时候播放 | 没有怎么办 |
| --- | --- | --- | --- | --- |
| 0 | `idle` | 6 | 站立、普通呼吸 | 至少给 1 个非透明基本画面 |
| 1 | `moveRight` | 8 | 向右拖、跑入右侧 | `idle` |
| 2 | `moveLeft` | 8 | 向左拖、跑入左侧 | 镜像 `moveRight`，再用 `idle` |
| 3 | `wave` | 4 | 空闲一段时间后招手 | `idle` |
| 4 | `jump` | 5 | 提醒反应、悬停／菜单跳跃 | 基本图像整体上移、回落 |
| 5 | `failed` | 8 | 预留失败表情 | `idle`；不当成成功 |
| 6 | `waiting` | 6 | 等待姿态 | `idle` |
| 7 | `thinking` | 6 | 运行任务／生成回复期间循环 | `waiting` → `idle` |
| 8 | `review` | 6 | 收尾、恢复站立前过渡 | `idle` |
| 9–10 | `lookDirections` | 16 | 眼神跟随输入位置 | 正面基础姿势 |

格数是经典素材的参考，不是程序强制要求。原生跳跃可以是 2、5、8 等不同帧数。`failed` 是可选可调用槽位，普通失败提醒并不会强制改变整个人物的当前姿势。

## 一个绑定块

```json
"thinking": {
  "row": 7,
  "column": 0,
  "frameCount": 6,
  "fps": 5,
  "fallback": "waiting",
  "purpose": "任务运行或聊天生成期间循环；缺失时先用等待，再用站立。"
}
```

如果原图没有思考动画，把 `frameCount` 改成 0。不是把一格透明图当成动作；这样人物就不会消失。

## 没有跳跃动画

```json
"jump": {
  "frameCount": 0,
  "fallback": "idle",
  "purpose": "缺少跳跃帧，用基本图像整体上移模拟，回落到原位。"
}
```

模拟只移动人物的绘制图像，不移动工具条或窗口；上方气泡与人物接触时会被顶起。下方气泡不触发顶气泡跳跃。每张原始格会统一装入 192×208 的画布，顶部默认留 32 px，模拟最大上移 24 px，避免把头裁掉。

## 注视和贴边

`lookDirections.enabled: false`：没有 16 向帧，继续显示正面，不必额外绘制。存在时从上方开始顺时针，每 22.5° 一格。

贴边优先用合适的注视图；没有时用待机图裁出左右一半。目的已在 `peekLeftPurpose` / `peekRightPurpose` 中注明，不再要求一个特定人物的固定裁剪位置。

## 验证

```powershell
Start-Process .\PetBridge.exe -ArgumentList '--validate-character', '角色目录\character.json' -Wait
```

在自己的 PetBridge profile 中查看 `setup-result.json`：`native` 表示哪些动作来自原素材，`resolvedFrom` 表示实际回退到哪个动作，`hasNativeJump` 表示是否使用原生跳跃。

越界、循环回退或配置了全透明画格会明确报错，方便 Codex 修正绑定。不要为了让验证通过而从别人的角色文件里复制帧。

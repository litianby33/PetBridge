"""Local atlas import only. No model calls, background uploads, or source file edits.

Requires Pillow. Imported art and its manifest go outside the public repository by default.
"""
import argparse
import json
import re
from pathlib import Path
from PIL import Image

SLOTS = [
    ("idle", 6, 2, "", "站立／呼吸；所有缺失动作的最终基础动作。"),
    ("moveRight", 8, 9, "idle", "向右拖动或跑入右边缘；缺失用待机。"),
    ("moveLeft", 8, 9, "moveRight", "向左移动；缺失镜像向右，再用待机。"),
    ("wave", 4, 5, "idle", "无人碰触一段时间后的招手；缺失用待机。"),
    ("jump", 5, 8, "idle", "提醒和悬停跳跃；缺失时整个人物上移模拟跳，不移动工具条。"),
    ("failed", 8, 7, "idle", "可选失败表情；缺失用待机，不伪装任务成功。"),
    ("waiting", 6, 4, "idle", "等待时的基本动作；缺失用待机。"),
    ("thinking", 6, 5, "waiting", "任务运行／回复生成期间循环；缺失用等待，再用待机。"),
    ("review", 6, 5, "idle", "收尾／回到待机前的过渡；缺失用待机。"),
]

def import_pet(source, destination, name=None, width=None, height=None, overwrite=False):
    source, destination = Path(source).resolve(), Path(destination).resolve()
    meta = {}
    if source.is_dir():
        if (source / "pet.json").is_file():
            meta = json.loads((source / "pet.json").read_text(encoding="utf-8-sig"))
            sheet = (source / meta.get("spritesheetPath", "spritesheet.webp")).resolve()
            if not sheet.is_relative_to(source):
                raise ValueError("pet.json sheet path must stay inside its folder")
        else:
            sheet = next((p for p in (source / "spritesheet.png", source / "spritesheet.webp") if p.is_file()), None)
            if sheet is None:
                raise ValueError("No spritesheet.png / spritesheet.webp found")
    else:
        sheet = source
    if not sheet.is_file():
        raise ValueError("Sprite image not found")
    if (destination / "character.json").exists() and not overwrite:
        raise ValueError("Destination already contains a role; use --overwrite explicitly")
    with Image.open(sheet) as opened:
        if opened.width * opened.height > 40_000_000:
            raise ValueError("Sprite image exceeds 40 million pixels")
        image = opened.convert("RGBA")
    standard = image.size in ((1536, 1872), (1536, 2288)) and not width and not height
    cell_w, cell_h = ((192, 208) if standard else (width or image.width, height or image.height))
    if min(cell_w, cell_h) <= 0 or max(cell_w, cell_h) > 2048:
        raise ValueError("Frame dimensions must be between 1 and 2048")
    if image.width % cell_w or image.height % cell_h:
        raise ValueError("Atlas dimensions must divide exactly into cells")
    cols, rows = image.width // cell_w, image.height // cell_h
    animations = {}
    for row, (key, expected, fps, fallback, purpose) in enumerate(SLOTS):
        count = 0
        # Unknown layouts get a single baseline; let Codex map additional actions explicitly.
        maximum = min(expected, cols) if (standard or width and height) and row < rows else (1 if row == 0 else 0)
        for col in range(maximum):
            frame = image.crop((col * cell_w, row * cell_h, (col + 1) * cell_w, (row + 1) * cell_h))
            if frame.getchannel("A").getbbox() is None:
                break
            count += 1
        animations[key] = dict(row=row, column=0, frameCount=count, fps=fps, fallback=fallback, purpose=purpose)
    if not animations["idle"]["frameCount"]:
        raise ValueError("First idle cell is transparent; map a real baseline before importing")
    display = name or meta.get("displayName") or source.stem
    identity = re.sub(r"[^a-z0-9-]+", "-", str(meta.get("id") or display).lower()).strip("-") or "my-character"
    spec = dict(schemaVersion=1, id=identity, displayName=display, sheet="atlas.png", frameWidth=cell_w,
                frameHeight=cell_h, topInset=32, alignIdleFeet=True, animations=animations,
                lookDirections=dict(enabled=standard and rows >= 11, startRow=9, startColumn=0, frameCount=16,
                                    purpose="从正上方起顺时针的 16 个注视方向；没有时保持基础正面动作。"),
                peekLeftPurpose="左侧露半身；优先注视方向，没有专用姿势就用待机的一半。",
                peekRightPurpose="右侧露半身；优先注视方向，没有专用姿势就用待机的一半。",
                assetLicense="User-supplied artwork; not covered by the PetBridge code license.")
    destination.mkdir(parents=True, exist_ok=True)
    image.save(destination / "atlas.png", optimize=True)
    output = destination / "character.json"
    output.write_text(json.dumps(spec, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return output

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source")
    parser.add_argument("destination")
    parser.add_argument("--name")
    parser.add_argument("--frame-width", type=int)
    parser.add_argument("--frame-height", type=int)
    parser.add_argument("--overwrite", action="store_true")
    args = parser.parse_args()
    result = import_pet(args.source, args.destination, args.name, args.frame_width, args.frame_height, args.overwrite)
    print(str(result))

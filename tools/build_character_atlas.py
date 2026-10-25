"""Pack generated action/gaze sheets without repainting character pixels.

Input contracts: 4 columns x 6 action rows; 4 columns x 4 gaze rows.
Use one shared scale for all action poses and one for the whole gaze sheet.
Register feet across grounded states; preserve measured jump displacement.
Review the contact sheet and animation preview before using the output.
"""
import argparse
import json
from pathlib import Path
from statistics import median
from PIL import Image, ImageDraw

NAMES = ['idle', 'moveRight', 'moveLeft', 'wave', 'jump', 'thinking']
PURPOSE = [
    '站立呼吸与眨眼，低帧率循环，避免高频抖动。',
    '向右移动／跑入右侧屏幕边缘，四帧步态。',
    '向左移动／跑入左侧屏幕边缘，独立绘制四帧。',
    '空闲时抬手招呼，随后回到待机。',
    '提醒反应：蓄力、离地、最高点、落地；保留原生高度。',
    '任务进行中手托下巴、转动目光，持续循环。',
]

def cells(path, rows):
    with Image.open(path) as opened:
        sheet = opened.convert('RGBA')
    frames = []
    for row in range(rows):
        for col in range(4):
            box = (round(col*sheet.width/4), round(row*sheet.height/rows),
                   round((col+1)*sheet.width/4), round((row+1)*sheet.height/rows))
            frame = sheet.crop(box)
            alpha = frame.getchannel('A')
            # Discard near-transparent matte noise only; preserve the visible artwork.
            alpha = alpha.point(lambda value: 0 if value < 32 else value)
            frame.putalpha(alpha)
            bounds = alpha.point(lambda value: 255 if value >= 128 else 0).getbbox()
            if bounds is None:
                raise ValueError(f'Empty generated pose at row {row}, column {col}')
            frames.append((frame, bounds))
    return frames

def pack(actions, looks, output):
    output = Path(output)
    output.mkdir(parents=True, exist_ok=True)
    action_frames, look_frames = cells(actions, 6), cells(looks, 4)
    scale = 222 / median(b[3]-b[1] for _, b in action_frames[:4])
    look_scale = 222 / median(b[3]-b[1] for _, b in look_frames)
    atlas = Image.new('RGBA', (1024, 2560))
    metrics = []
    jump_ground = median([action_frames[16][1][3], action_frames[19][1][3]])
    def foot_center(frame, bounds):
        alpha=frame.getchannel('A')
        points=[x for y in range(max(bounds[1],bounds[3]-8),bounds[3])
                for x in range(bounds[0],bounds[2]) if alpha.getpixel((x,y))>=128]
        return sum(points)/len(points)
    action_anchor=median((foot_center(f,b)-f.width/2)*scale for f,b in action_frames[:4])
    look_anchor=median((foot_center(f,b)-f.width/2)*look_scale for f,b in look_frames)
    gaze_offset=round(action_anchor-look_anchor)
    for index, (frame, bounds) in enumerate(action_frames + look_frames):
        factor = scale if index < 24 else look_scale
        row, col = divmod(index, 4)
        w, h = round(frame.width*factor), round(frame.height*factor)
        resized = frame.resize((w, h), Image.Resampling.NEAREST)
        source_ground = jump_ground if 16 <= index < 20 else bounds[3]
        x, y = round(128-frame.width*factor/2), round(248-source_ground*factor)
        if index>=24:
            x+=gaze_offset
        if 16<=index<20:
            # Keep the real airborne pose and enhance its measured displacement
            # for readable motion after the desktop runtime downscales the atlas.
            y-=min(18,round(max(0,jump_ground-bounds[3])*factor*1.5))
        registered = Image.new('RGBA', (256, 256))
        registered.alpha_composite(resized, (x, y))
        atlas.alpha_composite(registered, (col*256, row*256))
        solid = registered.getchannel('A').point(lambda value: 255 if value >= 128 else 0)
        actual = solid.getbbox()
        if not actual or min(actual[:2]) <= 0 or actual[2] >= 256 or actual[3] >= 256:
            raise ValueError(f'Pose {index} touches its packed cell edge: {actual}')
        metrics.append(dict(index=index, bounds=list(actual), row=row, column=col))
    atlas.save(output/'atlas.png', optimize=True)
    animations = {}
    for row, (name, purpose) in enumerate(zip(NAMES, PURPOSE)):
        animations[name] = dict(row=row, column=0, frameCount=4,
                                fps=2 if row == 0 else 7 if row in (1,2,4) else 4 if row == 3 else 3,
                                fallback='idle', purpose=purpose)
    for name, fallback, purpose in [
        ('failed','idle','可选失败表情，本版复用待机，不把失败显示为成功。'),
        ('waiting','idle','等待时使用基础站立；没有额外绘制。'),
        ('review','idle','收尾后回到基础站立；没有额外绘制。'),
    ]:
        animations[name] = dict(frameCount=0, fallback=fallback, purpose=purpose)
    spec = dict(schemaVersion=1, id='chatgpt-girl', displayName='ChatGPT娘', sheet='atlas.png',
                frameWidth=256, frameHeight=256, topInset=32, alignIdleFeet=True,
                animations=animations,
                lookDirections=dict(enabled=True,startRow=6,startColumn=0,frameCount=16,
                    purpose='从正上方起顺时针的16向注视。保持身体正面，仅头部和眼神轻转。'),
                peekLeftPurpose='左侧露半身时优先使用向右看的第4号注视帧。',
                peekRightPurpose='右侧露半身时优先使用向左看的第12号注视帧。',
                assetNotes='AI-generated pixel mascot. Six native action loops; optional states reuse idle. See ARTWORK.md.')
    (output/'character.json').write_text(json.dumps(spec,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    jump = [metrics[i]['bounds'][3] for i in range(16,20)]
    lift = max(jump[0],jump[3])-min(jump[1:3])
    if lift < 8:
        raise ValueError(f'Jump must visibly leave the baseline, measured only {lift}px')
    for row in [0,1,2,3,5]:
        feet = [metrics[row*4+i]['bounds'][3] for i in range(4)]
        if max(feet)-min(feet) > 2:
            raise ValueError(f'Grounded row {row} has unstable feet: {feet}')
    report = dict(packedFrames=40, nativeActions=6, gazeFrames=16, optionalFallbacks=3,
                  actionSharedScale=scale, gazeSharedScale=look_scale, jumpLift=lift,
                  gazeSharedOffsetX=gaze_offset, jumpDisplacementEnhanced=True,
                  jumpFeet=jump, groundedFeetRegistered=True, metrics=metrics)
    (output/'layout-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    # Contact sheet is a QA visualization, separate from the runtime texture.
    contact = Image.new('RGB',(1024,2560),(235,238,242))
    contact.paste(atlas,(0,0),atlas)
    draw = ImageDraw.Draw(contact)
    for row in range(10):
        draw.text((8,row*256+4),NAMES[row] if row<6 else 'look '+str((row-6)*4),fill=(25,35,45))
    contact.save(output/'contact-sheet.png',optimize=True)
    return report

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--actions',required=True)
    parser.add_argument('--looks',required=True)
    parser.add_argument('--output',required=True)
    args=parser.parse_args()
    result=pack(args.actions,args.looks,args.output)
    print(json.dumps({key:value for key,value in result.items() if key!='metrics'}))

"""Render GIF / MP4 native loops for visual review. No model calls."""
import argparse
import json
import subprocess
import shutil
import tempfile
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

def main(manifest, output, walking=False, slow=False):
    manifest=Path(manifest).resolve(); spec=json.loads(manifest.read_text(encoding='utf-8-sig'))
    atlas=Image.open(manifest.parent/spec['sheet']).convert('RGBA')
    names=['moveRight','moveLeft'] if walking else ['idle','moveRight','moveLeft','wave','jump','thinking']
    labels=['向右行走','向左行走'] if walking else ['待机 / 眨眼','向右移动','向左移动','招手','跳跃','思考']
    font_path=Path('C:/Windows/Fonts/msyh.ttc')
    font=ImageFont.truetype(str(font_path),22) if font_path.exists() else ImageFont.load_default()
    w,h=spec['frameWidth'],spec['frameHeight']; fps=12
    columns=2 if walking else 3
    dimensions=(columns*320,360 if walking else 720)
    gif_frames=[]
    with tempfile.TemporaryDirectory(prefix='petbridge-preview-') as temp:
        temp=Path(temp)
        for tick in range(fps*4):
            canvas=Image.new('RGB',dimensions,(239,241,246)); draw=ImageDraw.Draw(canvas)
            for i,name in enumerate(names):
                x,y=(i%columns)*320,(i//columns)*360
                draw.rounded_rectangle((x+10,y+10,x+310,y+350),radius=16,fill=(255,255,255))
                draw.text((x+28,y+26),labels[i] if font_path.exists() else name,font=font,fill=(48,52,62))
                binding=spec['animations'][name]
                rate=3 if slow else binding['fps']
                frame=int(tick/fps*rate)%binding['frameCount']
                left=(binding.get('column',0)+frame)*w; top=binding['row']*h
                art=atlas.crop((left,top,left+w,top+h)).resize((256,256),Image.Resampling.NEAREST)
                canvas.paste(art,(x+32,y+70),art)
            if Path(output).suffix.lower()=='.gif':
                gif_frames.append(canvas)
            else:
                canvas.save(temp/f'{tick:04d}.png')
        if gif_frames:
            palette=gif_frames[0].quantize(colors=256)
            converted=[f.quantize(palette=palette,dither=Image.Dither.NONE) for f in gif_frames]
            # GIF stores 10-ms units. Distribute rounding instead of shortening every frame.
            durations=[round((i+1)*100/fps)*10-round(i*100/fps)*10 for i in range(len(converted))]
            converted[0].save(output,save_all=True,append_images=converted[1:],duration=durations,loop=0,optimize=True)
            return
        ffmpeg=shutil.which('ffmpeg')
        if not ffmpeg:
            try:
                import imageio_ffmpeg
                ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
            except ImportError:
                raise SystemExit('Preview export requires ffmpeg on PATH or optional imageio-ffmpeg. The desktop app does not require either.')
        subprocess.run([ffmpeg,'-y','-loglevel','error','-framerate',str(fps),'-i',str(temp/'%04d.png'),
                        '-c:v','libx264','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart',str(Path(output).resolve())],check=True)

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('manifest');parser.add_argument('output')
    parser.add_argument('--walking',action='store_true');parser.add_argument('--slow',action='store_true')
    args=parser.parse_args()
    main(args.manifest,args.output,args.walking,args.slow)

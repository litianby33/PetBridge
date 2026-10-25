"""Replace only two walking rows of an existing four-column atlas.

The input is a newly drawn 4x2 contact/passing/contact/passing sheet.
Other actions and gaze pixels are copied exactly, without regeneration.
"""
import argparse
import hashlib
import json
from pathlib import Path
from statistics import median
from PIL import Image

def replace(atlas_path, walk_path, output):
    atlas=Image.open(atlas_path).convert('RGBA')
    walk=Image.open(walk_path).convert('RGBA')
    frames=[]
    for row in range(2):
        for col in range(4):
            f=walk.crop((round(col*walk.width/4),round(row*walk.height/2),
                         round((col+1)*walk.width/4),round((row+1)*walk.height/2)))
            f.putalpha(f.getchannel('A').point(lambda a:0 if a<32 else a))
            # A sheet neighbour may leave a tiny disconnected fragment at a cell edge.
            # Remove only small edge islands; retain all components of the main pose.
            a=f.getchannel('A');seen=set();removed=[]
            for y in range(f.height):
                for x in range(f.width):
                    if (x,y) in seen or a.getpixel((x,y))<32: continue
                    stack=[(x,y)];seen.add((x,y));component=[]
                    while stack:
                        px,py=stack.pop();component.append((px,py))
                        for nx,ny in ((px-1,py),(px+1,py),(px,py-1),(px,py+1)):
                            if 0<=nx<f.width and 0<=ny<f.height and (nx,ny) not in seen and a.getpixel((nx,ny))>=32:
                                seen.add((nx,ny));stack.append((nx,ny))
                    if len(component)<f.width*f.height*.008 and (max(v[0] for v in component)<f.width*.07 or min(v[0] for v in component)>f.width*.93):
                        removed.extend(component)
            for point in removed:a.putpixel(point,0)
            f.putalpha(a)
            b=f.getchannel('A').point(lambda a:255 if a>=128 else 0).getbbox()
            if not b: raise ValueError('Empty walking pose')
            frames.append((f,b))
    scale=222/median(b[3]-b[1] for f,b in frames)
    before=atlas.copy()
    metrics=[]
    for i,(f,b) in enumerate(frames):
        resized=f.resize((round(f.width*scale),round(f.height*scale)),Image.Resampling.NEAREST)
        pose=Image.new('RGBA',(256,256))
        # Register the head horizontally; keep each supporting foot on the ground.
        a=f.getchannel('A'); cut=b[1]+round((b[3]-b[1])*.4)
        head=a.crop((0,b[1],f.width,cut)).point(lambda v:255 if v>=128 else 0).getbbox()
        center=(head[0]+head[2])/2
        pose.alpha_composite(resized,(round(128-center*scale),round(248-b[3]*scale)))
        actual=pose.getchannel('A').point(lambda v:255 if v>=128 else 0).getbbox()
        if not actual or min(actual[:2])<=0 or actual[2]>=256 or actual[3]>=256:
            raise ValueError('Walking pose touches cell edge: '+str(actual))
        row,col=divmod(i,4)
        atlas.paste(pose,(col*256,(row+1)*256))
        metrics.append(dict(row=row+1,column=col,bounds=list(actual)))
    for box in [(0,0,1024,256),(0,768,1024,2560)]:
        if before.crop(box).tobytes()!=atlas.crop(box).tobytes():
            raise ValueError('Unexpected changes outside walking rows')
    output=Path(output);output.parent.mkdir(parents=True,exist_ok=True)
    atlas.save(output,optimize=True)
    return dict(replacedRows=[1,2],sharedScale=scale,otherPixelsUnchanged=True,
                choreography='near-contact,far-passing,far-contact,near-passing',metrics=metrics,
                sha256=hashlib.sha256(output.read_bytes()).hexdigest())

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('atlas');p.add_argument('walk');p.add_argument('output')
    args=p.parse_args()
    report=replace(args.atlas,args.walk,args.output)
    Path(args.output).with_suffix('.walk-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report,indent=2))

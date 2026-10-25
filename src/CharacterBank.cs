using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace PetBridge {
    // ANIMATION BINDINGS: these numeric slots keep the UI independent of character art.
    // Change characters/*/character.json, not these trigger points, when replacing a role.
    static class AnimationSlots {
        // 0 idle: standing / low-frequency breathing. Required baseline for every fallback.
        // 1 moveRight: dragging or entering the right edge. Missing -> idle.
        // 2 moveLeft: dragging or entering the left edge. Missing -> mirrored moveRight, then idle.
        // 3 wave: occasional greeting after inactivity. Missing -> idle.
        // 4 jump: hover / notification reaction. Missing -> idle plus whole-character vertical motion.
        // 5 failed: optional unsuccessful-task expression. Missing -> idle.
        // 6 waiting: optional waiting expression. Missing -> idle.
        // 7 thinking: loop while a task is running. Missing -> waiting, then idle.
        // 8 review: finishing / returning to rest. Missing -> idle.
        public const int Idle=0,MoveRight=1,MoveLeft=2,Wave=3,Jump=4,Failed=5,Waiting=6,Thinking=7,Review=8;
        public static readonly string[] Names={"idle","moveRight","moveLeft","wave","jump","failed","waiting","thinking","review"};
    }
    sealed class AnimationSpec {
        public int row=0,column=0,frameCount=0;public double fps=6;public bool enabled=true,mirror=false;
        public string fallback="idle",purpose="";
    }
    sealed class LookSpec {
        public bool enabled=false;public int startRow=9,startColumn=0,frameCount=16;
        public string purpose="16 directions clockwise from up; missing directions keep the idle pose.";
    }
    sealed class CharacterSpec {
        public int schemaVersion=1,frameWidth=192,frameHeight=208;public string id="default",displayName="Companion",sheet="builtin:orb";
        public bool alignIdleFeet=true;public int topInset=32;
        public Dictionary<string,AnimationSpec> animations=new Dictionary<string,AnimationSpec>();
        public LookSpec lookDirections=new LookSpec();
        public string peekLeftPurpose="Left-edge half-body pose; uses look-right or idle when missing.";
        public string peekRightPurpose="Right-edge half-body pose; uses look-left or idle when missing.";
    }
    sealed class SpriteBank : IDisposable {
        public const int CanvasWidth=192,CanvasHeight=208;
        public readonly int[] Counts=new int[9],Delays=new int[9];
        public readonly Bitmap[][] Frames=new Bitmap[9][];
        public readonly Bitmap[] Looks=new Bitmap[16];
        public readonly bool[] Native=new bool[9];public readonly string[] ResolvedFrom=new string[9];
        public Point[] IdleOffsets;public int[] JumpHeadTops;public Rectangle BodyBounds;
        public CharacterSpec Spec;public string ManifestPath;
        public bool HasNativeJump {get{return Native[4]&&Counts[4]>1;}}
        public int JumpApexTop {get{return JumpHeadTops.Min();}}
        public int JumpCycle {get{return HasNativeJump?Math.Max(250,Counts[4]*Delays[4]):650;}}
        public int JumpDuration {get{return NoticeJumpMotion.Delay+JumpCycle*NoticeJumpMotion.Repeats;}}
        public SpriteBank(string manifest=null){
            string selected=manifest??Program.CharacterOverride??PetSettings.Load().CharacterPath??Path.Combine(Program.Root,"characters","default","character.json");
            ManifestPath=Path.GetFullPath(Path.IsPathRooted(selected)?selected:Path.Combine(Program.Root,selected));
            if(!File.Exists(ManifestPath))throw new FileNotFoundException("Character manifest not found. Use Import-Character or --set-character.",ManifestPath);
            if(new FileInfo(ManifestPath).Length>1024*1024)throw new InvalidDataException("Character manifest is too large.");
            Spec=new JavaScriptSerializer().Deserialize<CharacterSpec>(File.ReadAllText(ManifestPath));
            if(Spec==null||Spec.schemaVersion!=1||Spec.frameWidth<1||Spec.frameHeight<1||Spec.frameWidth>2048||Spec.frameHeight>2048)throw new InvalidDataException("Invalid character schema or cell dimensions.");
            Spec.topInset=Math.Max(24,Math.Min(64,Spec.topInset));if(Spec.animations==null)Spec.animations=new Dictionary<string,AnimationSpec>();
            Bitmap sheet=null;
            try{
                if(Spec.sheet!="builtin:orb"){
                    string parent=Path.GetDirectoryName(ManifestPath),path=Path.GetFullPath(Path.Combine(parent,Spec.sheet??""));
                    if(!path.StartsWith(parent+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Sheet must stay inside its character folder.");
                    sheet=new Bitmap(path);if((long)sheet.Width*sheet.Height>40000000)throw new InvalidDataException("Atlas exceeds 40 million pixels.");
                }
                for(int i=0;i<9;i++)LoadSlot(i,sheet,new HashSet<int>());
                if(Spec.lookDirections!=null&&Spec.lookDirections.enabled&&sheet!=null){
                    int columns=sheet.Width/Spec.frameWidth,count=Math.Min(16,Math.Max(0,Spec.lookDirections.frameCount));
                    for(int i=0;i<count;i++){int cell=Spec.lookDirections.startRow*columns+Spec.lookDirections.startColumn+i;Looks[i]=ReadCell(sheet,cell/columns,cell%columns);}
                }
                BodyBounds=Bounds(Frames[0][0]);if(BodyBounds.IsEmpty)throw new InvalidDataException("Idle baseline is transparent.");
                JumpHeadTops=Frames[4].Select(f=>Bounds(f).Top).ToArray();IdleOffsets=new Point[Counts[0]];
                for(int i=0;i<Counts[0];i++)IdleOffsets[i]=new Point(0,Spec.alignIdleFeet?BodyBounds.Bottom-Bounds(Frames[0][i]).Bottom:0);
            }catch{Dispose();throw;}finally{if(sheet!=null)sheet.Dispose();}
        }
        void LoadSlot(int slot,Bitmap sheet,HashSet<int> resolving){
            if(Frames[slot]!=null)return;if(!resolving.Add(slot))throw new InvalidDataException("Animation fallback cycle: "+AnimationSlots.Names[slot]);
            AnimationSpec def;Spec.animations.TryGetValue(AnimationSlots.Names[slot],out def);
            bool declared=def!=null&&def.enabled&&def.frameCount>0;
            if(slot==0&&sheet==null){Frames[0]=new[]{Builtin()};Native[0]=true;ResolvedFrom[0]="builtin";Delays[0]=500;}
            else if(declared&&sheet!=null){
                if(def.frameCount>64||def.row<0||def.column<0)throw new InvalidDataException("Invalid animation range: "+AnimationSlots.Names[slot]);
                Frames[slot]=new Bitmap[def.frameCount];for(int i=0;i<def.frameCount;i++){var frame=ReadCell(sheet,def.row,def.column+i);if(def.mirror)frame.RotateFlip(RotateFlipType.RotateNoneFlipX);Frames[slot][i]=frame;}
                Native[slot]=true;ResolvedFrom[slot]=AnimationSlots.Names[slot];Delays[slot]=(int)Math.Round(1000/Math.Max(1,Math.Min(30,def.fps)));
            }else{
                if(slot==0)throw new InvalidDataException("Provide at least one nontransparent idle frame.");
                string fallback=def==null?DefaultFallback(slot):def.fallback;int target=Array.IndexOf(AnimationSlots.Names,fallback);if(target<0)target=0;
                LoadSlot(target,sheet,resolving);Frames[slot]=Frames[target].Select(f=>(Bitmap)f.Clone()).ToArray();
                if(slot==2&&target==1)foreach(var f in Frames[slot])f.RotateFlip(RotateFlipType.RotateNoneFlipX);
                ResolvedFrom[slot]=ResolvedFrom[target];Delays[slot]=Delays[target];
            }
            Counts[slot]=Frames[slot].Length;resolving.Remove(slot);
        }
        static string DefaultFallback(int slot){return slot==2?"moveRight":slot==7?"waiting":"idle";}
        Bitmap ReadCell(Bitmap sheet,int row,int col){
            var r=new Rectangle(col*Spec.frameWidth,row*Spec.frameHeight,Spec.frameWidth,Spec.frameHeight);
            if(r.Left<0||r.Top<0||r.Right>sheet.Width||r.Bottom>sheet.Height)throw new InvalidDataException("Animation cell exceeds atlas: row "+row+", column "+col);
            using(var raw=sheet.Clone(r,PixelFormat.Format32bppArgb)){
                if(Bounds(raw).IsEmpty)throw new InvalidDataException("Configured animation cell is empty: "+row+", "+col);
                var result=new Bitmap(CanvasWidth,CanvasHeight,PixelFormat.Format32bppArgb);
                using(var g=Graphics.FromImage(result)){g.Clear(Color.Transparent);g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;double fit=Math.Min(CanvasWidth/(double)raw.Width,(CanvasHeight-Spec.topInset)/(double)raw.Height);int w=(int)Math.Round(raw.Width*fit),h=(int)Math.Round(raw.Height*fit);g.DrawImage(raw,new Rectangle((CanvasWidth-w)/2,CanvasHeight-h,w,h),0,0,raw.Width,raw.Height,GraphicsUnit.Pixel);}return result;
            }
        }
        static Bitmap Builtin(){
            // Original code-drawn fallback mascot. No third-party character pixels are embedded.
            var b=new Bitmap(CanvasWidth,CanvasHeight,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(b)){g.SmoothingMode=SmoothingMode.AntiAlias;using(var fill=new SolidBrush(Color.FromArgb(101,172,184)))g.FillEllipse(fill,43,54,106,106);using(var face=new SolidBrush(Color.FromArgb(238,249,249)))g.FillEllipse(face,54,66,84,82);using(var eye=new SolidBrush(Color.FromArgb(38,66,76))){g.FillEllipse(eye,76,97,8,13);g.FillEllipse(eye,108,97,8,13);}using(var pen=new Pen(Color.FromArgb(101,172,184),9)){g.DrawLine(pen,73,153,65,182);g.DrawLine(pen,118,153,126,182);}using(var pen=new Pen(Color.FromArgb(38,66,76),3))g.DrawArc(pen,86,114,21,14,0,180);}return b;
        }
        // Peek bindings: a supplied gaze pose if available; otherwise cut half of idle.
        public Bitmap PeekPose(string side){return Looks[side=="left"?4:12]??Frames[0][0];}
        public Rectangle PeekSource(string side){var b=Bounds(PeekPose(side));int w=Math.Max(1,b.Width/2);return new Rectangle(side=="left"?b.Right-w:b.Left,b.Top,w,b.Height);}
        public static Rectangle Bounds(Bitmap image){int l=image.Width,t=image.Height,r=-1,b=-1;for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(image.GetPixel(x,y).A>20){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);}return r<0?Rectangle.Empty:Rectangle.FromLTRB(l,t,r+1,b+1);}
        public void Dispose(){foreach(var row in Frames)if(row!=null)foreach(var frame in row)if(frame!=null)frame.Dispose();foreach(var f in Looks)if(f!=null)f.Dispose();}
    }
}

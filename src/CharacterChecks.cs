using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Web.Script.Serialization;
namespace PetBridge {
    static class CharacterChecks {
        static void Require(bool yes,string label){if(!yes)throw new Exception("Character binding: "+label);}
        public static void Run(){
            string dir=Path.Combine(Program.DataRoot,"character-fixtures");Directory.CreateDirectory(dir);string sheet=Path.Combine(dir,"atlas.png");
            using(var b=new Bitmap(64,32,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(b)){g.Clear(Color.Transparent);g.FillEllipse(Brushes.Teal,6,10,20,20);g.FillEllipse(Brushes.Teal,38,2,20,20);b.Save(sheet,ImageFormat.Png);}
            var json=new JavaScriptSerializer();string basic=Path.Combine(dir,"basic.json");
            File.WriteAllText(basic,json.Serialize(new{schemaVersion=1,id="test-basic",displayName="Single frame",sheet="atlas.png",frameWidth=32,frameHeight=32,topInset=32,animations=new{idle=new{row=0,column=0,frameCount=1,fps=2}}}));
            using(var bank=new SpriteBank(basic)){Require(!bank.HasNativeJump&&bank.Counts[4]==1,"missing jump uses static basic art");Require(bank.ResolvedFrom[7]=="idle"&&bank.Counts[7]>0,"thinking to waiting to idle");Require(bank.Looks[0]==null&&bank.PeekSource("left").Width>0,"missing look and peek are usable");Require(bank.BodyBounds.Top>=24,"simulation has transparent headroom");for(int row=0;row<9;row++)Require(bank.Counts[row]>=1,"every animation slot resolves");}
            string native=Path.Combine(dir,"native.json");File.WriteAllText(native,json.Serialize(new{schemaVersion=1,id="test-native",sheet="atlas.png",frameWidth=32,frameHeight=32,animations=new{idle=new{row=0,column=0,frameCount=1,fps=2},jump=new{row=0,column=0,frameCount=2,fps=7}}}));
            using(var bank=new SpriteBank(native)){Require(bank.HasNativeJump&&bank.Counts[4]==2,"native jump does not require five frames");Require(bank.JumpHeadTops[1]<bank.JumpHeadTops[0],"native jump positions preserved");for(int t=NoticeJumpMotion.Delay;t<NoticeJumpMotion.Delay+bank.JumpCycle*3;t+=17)Require(NoticeJumpMotion.FrameAt(t,2,bank.Delays[4],bank.JumpCycle)<2,"variable jump playback stays within source frames");}
            Require(Math.Abs(NoticeJumpMotion.SimulatedPush(300,650))<.001&&Math.Abs(NoticeJumpMotion.SimulatedPush(625,650)-1)<.001&&NoticeJumpMotion.SimulatedPush(949,650)<.001,"simulated jump rises and returns without a sprite");
            string invalid=Path.Combine(dir,"invalid.json");File.WriteAllText(invalid,json.Serialize(new{schemaVersion=1,sheet="atlas.png",frameWidth=32,frameHeight=32,animations=new{idle=new{row=8,column=0,frameCount=1}}}));bool rejected=false;try{using(var bank=new SpriteBank(invalid)){} }catch(InvalidDataException){rejected=true;}Require(rejected,"bad atlas coordinates produce a useful error");
            File.WriteAllText(Path.Combine(Program.DataRoot,"verification-character.json"),json.Serialize(new{passed=true,singleFrame=true,nativeVariableFrames=true,missingJumpSimulation=true,missingLooksAndPeek=true,fallbackThinking=true,invalidCoordinatesRejected=true}));
        }
    }
}

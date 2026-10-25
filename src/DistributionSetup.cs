using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Web.Script.Serialization;
namespace PetBridge {
    static class DistributionSetup {
        static string Arg(string[] args,string key){int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:null;}
        public static bool Handle(string[] args){
            string bind=Arg(args,"--bind"),set=Arg(args,"--set-character"),validate=Arg(args,"--validate-character"),preview=Arg(args,"--render-preview");
            if(bind!=null){Guid id;string value=bind.Trim().TrimEnd('/');int slash=value.LastIndexOf('/');if(slash>=0)value=value.Substring(slash+1);if(!Guid.TryParse(value,out id))throw new ArgumentException("Use a local Codex thread ID or codex://threads/<id> link.");var settings=PetSettings.Load();settings.AppBridgeThreadId=id.ToString();settings.ChatLink="codex://threads/"+id;settings.Save();Report(new{ok=true,bound=true});return true;}
            if(set!=null||validate!=null){string path=Path.GetFullPath(set??validate);using(var bank=new SpriteBank(path)){if(set!=null){var settings=PetSettings.Load();settings.CharacterPath=path;settings.Save();}Report(new{ok=true,character=bank.Spec.displayName,native=bank.Native,resolvedFrom=bank.ResolvedFrom,frameCounts=bank.Counts,hasNativeJump=bank.HasNativeJump,simulationWhenMissing=!bank.HasNativeJump});}return true;}
            if(preview!=null){using(var bank=new SpriteBank()){using(var canvas=new Bitmap(384,416,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(canvas)){g.Clear(Color.FromArgb(239,244,246));g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;g.DrawImage(bank.Frames[0][0],new Rectangle(0,0,384,416));canvas.Save(Path.GetFullPath(preview),ImageFormat.Png);}}return true;}
            return false;
        }
        static void Report(object value){File.WriteAllText(Path.Combine(Program.DataRoot,"setup-result.json"),new JavaScriptSerializer().Serialize(value));}
    }
}

using System;
namespace PetBridge {
    static class NoticeJumpMotion {
        public const int Delay=300,FrameMs=130,Frames=5,Repeats=3;
        public const int Cycle=FrameMs*Frames,Duration=Delay+Cycle*Repeats;
        // Dynamic binding: any jump frame count is accepted; no native jump uses frame 0.
        public static int FrameAt(double elapsed,int frames,int frameMs,int cycle){if(elapsed<Delay)return -1;return Math.Min(Math.Max(0,frames-1),(int)((elapsed-Delay)%cycle)/Math.Max(1,frameMs));}
        // Missing jump fallback: move the entire rendered character, never its toolbar or window.
        public static double SimulatedPush(double elapsed,int cycle){if(elapsed<Delay)return 0;double phase=((elapsed-Delay)%cycle)/cycle;return .5-.5*Math.Cos(2*Math.PI*phase);}
        public static int FrameAt(double elapsed){return elapsed<Delay||elapsed>=Duration?-1:(int)((elapsed-Delay)%Cycle)/FrameMs;}
        static double Smooth(double value){value=Math.Max(0,Math.Min(1,value));return value*value*(3-2*value);}
        public static double PushAt(double elapsed){if(FrameAt(elapsed)<0)return 0;double t=(elapsed-Delay)%Cycle;
            if(t<FrameMs)return 0;
            if(t<2*FrameMs)return Smooth((t-FrameMs)/FrameMs);
            if(t<2.6*FrameMs)return 1;
            return 1-Smooth((t-2.6*FrameMs)/(Cycle-2.6*FrameMs));
        }
        public static int BubbleLift(double elapsed,int tailY,int apexY,int available){double amplitude=Math.Max(12,tailY-apexY+2);return (int)Math.Round(Math.Min(Math.Max(0,available),amplitude)*PushAt(elapsed));}
    }
    static class NoticeJumpChecks {
        public static void Run(){
            if(NoticeJumpMotion.FrameAt(0)!=-1||NoticeJumpMotion.FrameAt(299)!=-1||NoticeJumpMotion.FrameAt(NoticeJumpMotion.Duration)!=-1)throw new Exception("Jump animation timing boundary");
            for(int loop=0;loop<3;loop++)for(int frame=0;frame<5;frame++){double t=NoticeJumpMotion.Delay+loop*NoticeJumpMotion.Cycle+frame*NoticeJumpMotion.FrameMs+30;if(NoticeJumpMotion.FrameAt(t)!=frame)throw new Exception("Must play all five original frames three times");}
            for(int loop=0;loop<3;loop++){double t=NoticeJumpMotion.Delay+loop*NoticeJumpMotion.Cycle;if(NoticeJumpMotion.PushAt(t)!=0||NoticeJumpMotion.PushAt(t+2*NoticeJumpMotion.FrameMs)!=1||NoticeJumpMotion.PushAt(t+NoticeJumpMotion.Cycle-1)>.001)throw new Exception("Bubble impact must follow each jump apex and settle");}
            if(NoticeJumpMotion.BubbleLift(560,400,350,200)!=52||NoticeJumpMotion.BubbleLift(560,400,350,8)!=8||NoticeJumpMotion.BubbleLift(0,400,350,200)!=0)throw new Exception("Bubble contact and top boundary");
            using(var bank=new SpriteBank()){if(bank.Counts[4]<1||bank.JumpHeadTops.Length!=bank.Counts[4])throw new Exception("Jump binding must support native and fallback counts");}
        }
    }
}

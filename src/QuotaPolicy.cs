using System;
using System.Collections.Generic;
namespace PetBridge {
    sealed class QuotaAlarmState {
        public string FiveSent="",WeekSent="",ResetSent="";
        public long PreviousReset;public double PreviousRemaining=-1;
    }
    sealed class QuotaDecision {public string Kind,Title,Message;}
    sealed class QuotaAlarm {
        readonly QuotaAlarmState state;
        public QuotaAlarm(QuotaAlarmState value){state=value;}
        static string Key(Quota q,int threshold){return q==null?"":q.ResetsAt+":"+threshold;}
        static bool Low(Quota q,int threshold){return q!=null&&threshold>0&&q.Remaining<threshold;}
        public static string Countdown(Quota q,long now){if(q==null||q.ResetsAt<=0)return "重置时间暂不可用";long minutes=(long)Math.Ceiling(Math.Max(0,q.ResetsAt-now)/60.0);if(minutes==0)return "正在等待额度刷新";return "等待 "+(minutes>=60?minutes/60+" 小时 ":"")+(minutes%60>0?minutes%60+" 分钟 ":"")+"后重置";}
        static QuotaDecision Describe(UsageSnapshot s,bool primary,bool weekly,long now){
            if(s==null)return null;
            if(s.Primary!=null&&s.Primary.Remaining<=0)return new QuotaDecision{Kind="empty",Title="5 小时已用完啦",Message=Countdown(s.Primary,now)};
            if(s.Secondary!=null&&s.Secondary.Remaining<=0)return new QuotaDecision{Kind="empty",Title="本周额度已用完啦",Message=Countdown(s.Secondary,now)};
            var parts=new List<string>();if(primary)parts.Add("5 小时剩余 "+s.Primary.Remaining.ToString("0")+"%");if(weekly)parts.Add("本周剩余 "+s.Secondary.Remaining.ToString("0")+"%");
            return parts.Count==0?null:new QuotaDecision{Kind="low",Title="剩余额度提醒",Message=String.Join(" · ",parts.ToArray())};
        }
        public QuotaDecision Observe(UsageSnapshot s,int five,int week,long now){if(s==null)return null;var q=s.Primary;QuotaDecision reset=null;
            if(q!=null){bool renewed=state.PreviousRemaining>=0&&state.PreviousReset>0&&now>=state.PreviousReset&&q.Remaining>state.PreviousRemaining+1&&(q.ResetsAt>state.PreviousReset||state.PreviousRemaining<=0);string cycle=q.ResetsAt.ToString();if(renewed&&state.ResetSent!=cycle){state.ResetSent=cycle;reset=new QuotaDecision{Kind="reset",Title="5 小时额度已重置",Message="现在剩余 "+q.Remaining.ToString("0")+"% · 可以继续聊天啦"};}state.PreviousReset=q.ResetsAt;state.PreviousRemaining=q.Remaining;}
            bool a=Low(s.Primary,five),b=Low(s.Secondary,week);string ak=Key(s.Primary,five),bk=Key(s.Secondary,week);bool fireA=a&&state.FiveSent!=ak,fireB=b&&state.WeekSent!=bk;
            if(fireA)state.FiveSent=ak;if(fireB)state.WeekSent=bk;
            // Exhaustion notices belong only to confirmed terminal tasks, never periodic quota reads.
            bool exhausted=s.Primary!=null&&s.Primary.Remaining<=0||s.Secondary!=null&&s.Secondary.Remaining<=0;
            return reset??(!exhausted&&(fireA||fireB)?Describe(s,fireA,fireB,now):null);
        }
        public QuotaDecision AfterTask(UsageSnapshot s,int five,int week,long now){if(s==null)return null;bool a=Low(s.Primary,five),b=Low(s.Secondary,week);
            // Exhaustion is useful even when that low-warning threshold is disabled.
            if(s.Primary!=null&&s.Primary.Remaining<=0)a=true;if(s.Secondary!=null&&s.Secondary.Remaining<=0)b=true;
            if(a)state.FiveSent=Key(s.Primary,five);if(b)state.WeekSent=Key(s.Secondary,week);
            return a||b?Describe(s,a,b,now):null;
        }
    }
}

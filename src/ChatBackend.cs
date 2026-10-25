using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
namespace PetBridge {
    static class ChatBackend {
        public static string Run(string prompt,PetSettings settings,Action<Process> capture){
            if(Program.Offline)throw new InvalidOperationException("当前是离线预览模式，不会发送聊天。请正常启动后再使用聊天功能。");
            string cli=FindCli(settings.CliPath);
            string work=Path.Combine(Program.DataRoot,"chat-work");Directory.CreateDirectory(work);
            string model=settings.Model;
            if(!String.IsNullOrWhiteSpace(model)&&!System.Text.RegularExpressions.Regex.IsMatch(model,"^[A-Za-z0-9_.-]+$"))throw new Exception("聊天模型名称无效");
            string modelArgument=String.IsNullOrWhiteSpace(model)?"":" -m "+model;
            var start=new ProcessStartInfo(cli,"exec --ignore-user-config --skip-git-repo-check --ephemeral --sandbox read-only --json"+modelArgument+" -c model_reasoning_effort=\"\\\"low\\\"\" -") {
                WorkingDirectory=work,UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8
            };
            using(var p=new Process {StartInfo=start}){
                capture(p);p.Start();
                Task<string> output=p.StandardOutput.ReadToEndAsync(), errors=p.StandardError.ReadToEndAsync();
                byte[] promptBytes=Encoding.UTF8.GetBytes(prompt);p.StandardInput.BaseStream.Write(promptBytes,0,promptBytes.Length);p.StandardInput.Close();
                if(!p.WaitForExit(120000)){try{p.Kill();}catch{}throw new Exception("这次回复等待超过两分钟，请稍后重试。");}
                Task.WaitAll(output,errors);
                string answer="",failure="";
                foreach(string line in output.Result.Split('\n')){
                    try{
                        var evt=new JavaScriptSerializer().DeserializeObject(line) as Dictionary<string,object>;if(evt==null)continue;
                        object type;evt.TryGetValue("type",out type);
                        if((string)type=="item.completed"){
                            var item=evt["item"] as Dictionary<string,object>;
                            if(item!=null&&item.ContainsKey("type")&&(string)item["type"]=="agent_message")answer+=(answer.Length==0?"":"\n")+(string)item["text"];
                        }
                        if((string)type=="error"&&evt.ContainsKey("message"))failure=(string)evt["message"];
                    }catch{}
                }
                if(answer.Length>0)return answer.Trim();
                if(p.ExitCode!=0)throw new Exception(failure.Length>0?failure:"ChatGPT 连接未完成。请确认桌面应用仍已登录，或使用“带到 ChatGPT 应用”。");
                throw new Exception("未收到文字回复，请重试。");
            }
        }
        public static string FindCli(string configured){
            if(!String.IsNullOrWhiteSpace(configured)&&File.Exists(configured))return configured;
            string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"OpenAI","Codex","bin");
            if(Directory.Exists(root)){
                var files=new List<string>(Directory.GetFiles(root,"codex.exe",SearchOption.AllDirectories));
                files.Sort((a,b)=>File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));if(files.Count>0)return files[0];
            }
            throw new Exception("没有找到已安装的 Codex 聊天组件，可用下方按钮打开 ChatGPT 应用。");
        }
    }

}

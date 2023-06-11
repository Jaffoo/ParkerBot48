using Mirai.Net.Utils.Scaffolds;
using Newtonsoft.Json.Linq;
using ParkerBot;
using MiniExcelLibs;

namespace Helper
{
    public class Pocket
    {
        public static LiteContext? _liteContext { get; set; }
        public static async Task PocketMessageReceiver(string str)
        {
            try
            {
                await Task.Run(async () =>
                {
                    var log = Directory.GetCurrentDirectory() + "/logs";
                    if (!Directory.Exists(log)) Directory.CreateDirectory(log);
                    var logFile = log + "/message.txt";
                    if (!File.Exists(logFile)) File.Create(logFile);
                    using var sw = new StreamWriter(logFile);
                    await sw.WriteLineAsync(str);
                    await sw.DisposeAsync();
                    sw.Close();
                });
                if (!Const.EnableModule.kd) return;
                _liteContext = new();
                var result = JObject.Parse(str);
                var time = result["time"]!.ToString();
                var channelName = result["channelName"]!.ToString();
                var name = result["ext"]!["user"]!["nickName"]!.ToString();
                int roleId = result["ext"]!["user"]!["roleId"]!.ToString().ToInt();
                string msgType = result["type"]!.ToString();
                string msbBody = "";
                //是否是计分
                var fen = false;
                if (result.ContainsKey("attach"))
                {
                    if (result["fromAccount"]!.ToString() == "admin") return;
                    var attachFen = (JObject)result["attach"]!;
                    if (attachFen.ContainsKey("giftInfo"))
                    {
                        var listFen = new List<string> { "0.1分", "1分", "9分", "99分", "999分" };
                        if (listFen.Contains(attachFen["giftInfo"]!["giftName"]!.ToString()))
                        {
                            fen = true;
                            //创建工作表
                            var path = Directory.GetCurrentDirectory() + "/wwwroot/excel";
                            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                            var excel = path + "/CelebrationScore"+DateTime.Now.ToString("MMdd")+".csv";
                            if (!File.Exists(excel))
                            {
                                MiniExcel.SaveAs(excel, null);
                                MiniExcel.Insert(excel, new { 口袋ID = "口袋ID", 昵称 = "昵称", 分数 = "分数", 时间 = "时间",来源="来源" });
                            }
                            var value = new { 口袋ID = result["ext"]!["user"]!["userId"], 昵称 = name, 分数 = attachFen["giftInfo"]!["tpNum"]!, 时间 = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),来源= channelName };
                            MiniExcel.Insert(excel, value);
                        }
                    }
                }
                if (!fen && roleId != 3) return;
                MessageChainBuilder mcb = new();
                mcb.Plain($"【{channelName}】\n【{time}】\n{name}:");
                if (msgType == "image")
                {
                    msbBody = result["attach"]!["url"]!.ToString();
                    mcb.ImageFromBase64(Base64.UrlImgToBase64(msbBody).Result);
                    await Task.Run(async () =>
                    {
                        await Weibo.FatchFace(msbBody);
                    });
                }
                else if (msgType == "text")
                {
                    //"230226137"
                    msbBody = result["body"]!.ToString();
                    mcb.Plain(msbBody);
                }
                else if (msgType == "video")
                {
                    mcb.Plain(result["attach"]!["url"]!.ToString());
                }
                else if (msgType == "audio")
                {
                    mcb.VoiceFromUrl(result["attach"]!["url"]!.ToString());
                }
                else if (msgType == "custom")
                {
                    var attach = result["attach"]!;
                    if (attach["messageType"]!.ToString() == "REPLY")
                    {
                        msbBody = attach["replyInfo"]!["text"] + "\n" + attach["replyInfo"]!["replyName"]! + ":" + attach["replyInfo"]!["replyText"]!;
                        mcb.Plain(msbBody);
                    }
                    else if (attach["messageType"]!.ToString() == "GIFTREPLY")
                    {
                        msbBody = attach["giftReplyInfo"]!["text"] + "\n" + attach["giftReplyInfo"]!["replyName"]! + ":" + attach["giftReplyInfo"]!["replyText"]!;
                        mcb.Plain(msbBody);
                    }
                    else if (attach["messageType"]!.ToString() == "GIFT_TEXT")
                    {
                        msbBody = "为" + attach["giftInfo"]!["userName"]! + "的作品打出了" + attach["giftInfo"]!["giftName"] + "。";
                        mcb.Plain(msbBody);
                    }
                    else if (attach["messageType"]!.ToString() == "LIVEPUSH")
                    {
                        //判断是否at所有人
                        msbBody = "直播啦！\n标题：" + attach["livePushInfo"]!["liveTitle"];
                        mcb.Plain(msbBody).ImageFromBase64(Base64.UrlImgToBase64(Const.ConfigModel.KD.imgDomain + attach["livePushInfo"]!["liveCover"]!.ToString()).Result);
                    }
                    else if (attach["messageType"]!.ToString() == "AUDIO")
                    {
                        mcb.VoiceFromUrl(attach["audioInfo"]!["url"]!.ToString());
                    }
                    else if (attach["messageType"]!.ToString() == "VIDEO")
                    {
                        mcb.VoiceFromUrl(attach["videoInfo"]!["url"]!.ToString());
                    }
                    // 房间电台
                    else if (attach["messageType"]!.ToString() == "TEAM_VOICE")
                    {
                        //判断是否at所有人
                        msbBody = name + "开启了房间电台";
                        mcb.Plain(msbBody);
                    }
                    //文字翻牌
                    else if (attach["messageType"]!.ToString() == "FLIPCARD")
                    {
                        return;
                    }
                    //语音翻牌
                    else if (attach["messageType"]!.ToString() == "FLIPCARD_AUDIO")
                    {
                        return;
                    }
                    //视频翻牌
                    else if (attach["messageType"]!.ToString() == "FLIPCARD_VIDEO")
                    {
                        return;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    Console.WriteLine(msgType);
                    return;
                }
                if (!Const.ConfigModel.KD.forwardGroup) return;
                string group = !string.IsNullOrWhiteSpace(Const.ConfigModel.KD.group) ? Const.ConfigModel.KD.group : Const.ConfigModel.QQ.group;
                List<string> groups = !string.IsNullOrWhiteSpace(group) ? group.Split(",").ToList() : new();
                groups.ForEach(async (item) =>
                {
                    await Msg.SendGroupMsg(item, mcb.Build());
                });
                if (!Const.ConfigModel.KD.forwardQQ) return;
                if (!string.IsNullOrWhiteSpace(Const.ConfigModel.KD.qq))
                {
                    var qqs = Const.ConfigModel.KD.qq.Split(",").ToList();
                    qqs.ForEach(async (item) =>
                    {
                        await Msg.SendFriendMsg(item, mcb.Build());
                    });
                }
                else if (!string.IsNullOrWhiteSpace(Const.ConfigModel.QQ.admin))
                {
                    await Msg.SendFriendMsg(Const.ConfigModel.QQ.admin, mcb.Build());
                }
            }
            catch (Exception e)
            {
                _liteContext = new();
                await _liteContext.Logs.AddAsync(new()
                {
                    message = e.Message,
                    createDate = DateTime.Now,
                });
                await _liteContext.SaveChangesAsync();
                await _liteContext.DisposeAsync();
                await Msg.SendFriendMsg(Msg.Admin, "程序报错了，请联系反馈给开发人员！");
            }
        }
    }
}
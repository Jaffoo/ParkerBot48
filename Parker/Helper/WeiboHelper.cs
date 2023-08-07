﻿using Newtonsoft.Json.Linq;
using ParkerBot;
using System.Globalization;
using Mirai.Net.Utils.Scaffolds;
using Newtonsoft.Json;
using System.Linq;

namespace Helper
{
    public class Weibo
    {
        public static LiteContext? dbContext;
        public static List<string> Uids
        {
            get { return Const.ConfigModel.WB.url.ToListV2(); }
        }
        public static int Similarity
        {
            get { return Const.ConfigModel.BD.similarity.ToInt(); }
        }
        public static int Audit
        {
            get { return Const.ConfigModel.BD.audit.ToInt(); }
        }
        public static int TimeSpan
        {
            get { return Const.ConfigModel.WB.timeSpan.ToInt(); }
        }
        public static List<string> Keywords
        {
            get { return Const.ConfigModel.WB.keyword.ToListV2(); }
        }
        public static List<string> ChiGuaId
        {
            get { return Const.ConfigModel.WB.cg.ToListV2(); }
        }
        public static async Task Seve()
        {
            string url = "";
            try
            {
                var index = -1;
                foreach (var item in Uids)
                {
                    index++;
                    url = "https://weibo.com/ajax/statuses/mymblog?uid=" + item;
                    var handler = new HttpClientHandler() { UseCookies = true };
                    HttpClient httpClient = new(handler);
                    httpClient.DefaultRequestHeaders.Add("user-agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.64 Safari/537.36 Edg/101.0.1210.53");
                    httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\" Not A; Brand\";v=\"99\", \"Chromium\";v=\"101\", \"Microsoft Edge\";v=\"101\"");
                    httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "Windows");
                    httpClient.DefaultRequestHeaders.Add("cookie", "SINAGLOBAL=5941175256214.382.1642773872907; ALF=1679281645; SCF=AhRx5Mk_RQnso6fKaohKePld2ACjdhTBOnxiAuojS-dpOKDZ28Z_zCDf8sNRIBYNX9eloeTXveotTWb5RnxcuTM.; SUB=_2AkMVObiwf8NxqwJRmP0Sz2_hZYt2zw_EieKjZUlrJRMxHRl-yT92qhYdtRB6PrmWX9HM2ihb-yVCcUUmaIfpbzQFEJoB; SUBP=0033WrSXqPxfM72-Ws9jqgMF55529P9D9WFencmWZyNhNlrzI6f0SiqP; login_sid_t=feb5d6bbda373d92c0c6e139217b5db3; cross_origin_proto=SSL; _s_tentry=cn.bing.com; UOR=,,cn.bing.com; Apache=6304595585897.093.1653402506204; ULV=1653402506208:6:1:1:6304595585897.093.1653402506204:1650801573670; wb_view_log=1920*10801; XSRF-TOKEN=HGapIzQJMJSifxxET5AdtejC; WBPSESS=a_YZA6I5qCR3U8i3Rfvlpuuut1qO7V23G4iYU50rBIh48BgY8rLDiveiRcJ7gBViMW4yXZTrlj1ALj997n-skQeQkUEuApt0KJq31YKkNMQPi9GTi0yYk7gm2rXw-ymmx_tg2neuAfVC1UZcHK4O3JCatmXj_y8HEVwjPjcFNls=");
                    var res = await httpClient.GetAsync(url);
                    var content = await res.Content.ReadAsStringAsync();
                    var data = JObject.Parse(content);
                    var list = JArray.FromObject(data["data"]!["list"]!);
                    foreach (JObject blog in list.Cast<JObject>())
                    {
                        DateTime createDate = new();
                        CultureInfo cultureInfo = CultureInfo.CreateSpecificCulture("en-US");
                        string format = "ddd MMM d HH:mm:ss zz00 yyyy";
                        string stringValue = blog!["created_at"]!.ToString();
                        if (!string.IsNullOrWhiteSpace(stringValue))
                            createDate = DateTime.ParseExact(stringValue, format, cultureInfo); // 将字符串转换成日期
                        if (createDate >= DateTime.Now.AddMinutes(-TimeSpan))
                        {
                            //可以认定为新发的微博
                            //获取微博类型0-视频，2-图文,1-转发微博
                            var mblogtype = -1;
                            if (blog.ContainsKey("page_info")) mblogtype = 0;
                            if (blog.ContainsKey("pic_infos")) mblogtype = 2;
                            if (blog.ContainsKey("topic_struct")) mblogtype = 1;
                            //需要发送通知则发送通知
                            if (index == 0)
                            {
                                if (Const.ConfigModel.WB.forwardGroup)
                                {
                                    var groups = string.IsNullOrWhiteSpace(Const.ConfigModel.WB.group) ? Const.ConfigModel.QQ.group : Const.ConfigModel.WB.group;
                                    var glist = groups.ToListV2();
                                    var mcb = new MessageChainBuilder();
                                    //预留是否要at所有人
                                    //if (false)
                                    //{
                                    //    mcb.AtAll();
                                    //}

                                    if (mblogtype == 2)
                                    {
                                        mcb.Plain($"{blog["user"]!["screen_name"]}发微博啦！(https://weibo.com/{blog["user"]!["id"]}/{blog["mid"]})\n");
                                        //获取第一张图片发送
                                        var first = blog["pic_infos"]![JArray.FromObject(blog["pic_ids"]!)[0]!.ToString()]!["large"]!["url"]!.ToString();
                                        mcb.Plain(blog["text_raw"]!.ToString()).ImageFromUrl(first);
                                    }
                                    else if (mblogtype == 0)
                                    {
                                        mcb.Plain($"{blog["user"]!["screen_name"]}发微博啦！(https://weibo.com/{blog["user"]!["id"]}/{blog["mid"]})\n");
                                        var pageInfo = (JObject?)blog["page_info"];
                                        if (pageInfo != null)
                                        {
                                            var objType = pageInfo["object_type"]!.ToString();
                                            if (objType == "video")
                                            {
                                                mcb.Plain(blog["text_raw"]!.ToString());
                                                mcb.Plain("视频链接：" + pageInfo["media_info"]!["h5_url"]!);
                                            }
                                        }
                                    }
                                    else if (mblogtype == 1)
                                    {
                                        mcb.Plain($"{blog["user"]!["screen_name"]}转发微博(https://weibo.com/{blog["user"]!["id"]}/{blog["mid"]})\n");
                                        mcb.Plain(blog["text_raw"]!.ToString());
                                    }
                                    else
                                    {
                                        mcb.Plain($"{blog["user"]!["screen_name"]}发微博啦！(https://weibo.com/{blog["user"]!["id"]}/{blog["mid"]})\n");
                                        mcb.Plain(blog["text_raw"]?.ToString() ?? "");
                                    }
                                    foreach (var group in glist)
                                    {
                                        await Msg.SendGroupMsg(group, mcb.Build());
                                    }
                                }
                                if (Const.ConfigModel.WB.forwardQQ)
                                {
                                    var qqs = string.IsNullOrWhiteSpace(Const.ConfigModel.WB.qq) ? Msg.Admin : Const.ConfigModel.WB.qq;
                                    var qlist = qqs.ToListV2();
                                    var mcb = new MessageChainBuilder();
                                    mcb.Plain($"{blog["user"]!["screen_name"]}发微博啦！");
                                    if (mblogtype == 2)
                                    {
                                        //获取第一张图片发送
                                        var first = blog["pic_infos"]![JArray.FromObject(blog["pic_ids"]!)[0]!.ToString()]!["large"]!["url"]!.ToString();
                                        mcb.Plain(blog["text_raw"]!.ToString()).ImageFromUrl(first);
                                    }
                                    else if (mblogtype == 0)
                                    {
                                        var pageInfo = (JObject?)blog["page_info"];
                                        if (pageInfo != null)
                                        {
                                            var objType = pageInfo["object_type"]!.ToString();
                                            if (objType == "video")
                                            {
                                                mcb.Plain(blog["text_raw"]!.ToString());
                                                mcb.Plain("视频链接：" + pageInfo["media_info"]!["h5_url"]!);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        mcb.Plain("未知类型微博，更多类型通知尽请期待！");
                                    }
                                    foreach (var qq in qlist)
                                    {
                                        await Msg.SendFriendMsg(qq, mcb.Build());
                                    }
                                }
                            }
                            //保存图片
                            if (mblogtype == 2)
                            {
                                var picList = blog["pic_ids"]!.Select(t => t.ToString()).ToList();
                                if (picList == null) continue;
                                foreach (var picId in picList)
                                {
                                    var picInfo = (JObject)blog["pic_infos"]!;
                                    if (picInfo.ContainsKey(picId))
                                    {
                                        var imgUrl = picInfo[picId]!["original"]!["url"]!.ToString();
                                        var fileName = Path.GetFileName(imgUrl);
                                        imgUrl = "https://cdn.ipfsscan.io/weibo/large/" + fileName;
                                        await FatchFace(imgUrl, true);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                e.AddLog("报错链接：" + url + "\n错误信息：" + e.Message);
                return;
            }
        }

        public static async Task ChiGua()
        {
            try
            {
                foreach (var item in ChiGuaId)
                {
                    var url = "https://weibo.com/ajax/statuses/mymblog?uid=" + item;
                    var handler = new HttpClientHandler() { UseCookies = true };
                    HttpClient httpClient = new(handler);
                    httpClient.DefaultRequestHeaders.Add("user-agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.64 Safari/537.36 Edg/101.0.1210.53");
                    httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\" Not A; Brand\";v=\"99\", \"Chromium\";v=\"101\", \"Microsoft Edge\";v=\"101\"");
                    httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "Windows");
                    httpClient.DefaultRequestHeaders.Add("cookie", "SINAGLOBAL=5941175256214.382.1642773872907; ALF=1679281645; SCF=AhRx5Mk_RQnso6fKaohKePld2ACjdhTBOnxiAuojS-dpOKDZ28Z_zCDf8sNRIBYNX9eloeTXveotTWb5RnxcuTM.; SUB=_2AkMVObiwf8NxqwJRmP0Sz2_hZYt2zw_EieKjZUlrJRMxHRl-yT92qhYdtRB6PrmWX9HM2ihb-yVCcUUmaIfpbzQFEJoB; SUBP=0033WrSXqPxfM72-Ws9jqgMF55529P9D9WFencmWZyNhNlrzI6f0SiqP; login_sid_t=feb5d6bbda373d92c0c6e139217b5db3; cross_origin_proto=SSL; _s_tentry=cn.bing.com; UOR=,,cn.bing.com; Apache=6304595585897.093.1653402506204; ULV=1653402506208:6:1:1:6304595585897.093.1653402506204:1650801573670; wb_view_log=1920*10801; XSRF-TOKEN=HGapIzQJMJSifxxET5AdtejC; WBPSESS=a_YZA6I5qCR3U8i3Rfvlpuuut1qO7V23G4iYU50rBIh48BgY8rLDiveiRcJ7gBViMW4yXZTrlj1ALj997n-skQeQkUEuApt0KJq31YKkNMQPi9GTi0yYk7gm2rXw-ymmx_tg2neuAfVC1UZcHK4O3JCatmXj_y8HEVwjPjcFNls=");
                    var res = await httpClient.GetAsync(url);
                    var content = await res.Content.ReadAsStringAsync();
                    var data = JObject.Parse(content);
                    var list = JArray.FromObject(data["data"]!["list"]!);
                    foreach (JObject blog in list)
                    {
                        DateTime createDate = new();
                        CultureInfo cultureInfo = CultureInfo.CreateSpecificCulture("en-US");
                        string format = "ddd MMM d HH:mm:ss zz00 yyyy";
                        string stringValue = blog!["created_at"]!.ToString();
                        if (!string.IsNullOrWhiteSpace(stringValue))
                            createDate = DateTime.ParseExact(stringValue, format, cultureInfo); // 将字符串转换成日期
                        if (createDate >= DateTime.Now.AddMinutes(-TimeSpan))
                        {
                            //可以认定为新发的微博
                            //获取微博类型0-视频，2-图文
                            var mblogtype = -1;
                            if (blog.ContainsKey("page_info")) mblogtype = 0;
                            if (blog.ContainsKey("pic_infos")) mblogtype = 2;
                            var blogContent = blog["text_raw"]!.ToString();
                            if (!Keywords.Select(keyword => blogContent.Contains(keyword)).Any(x => x) && Keywords.Count > 0)
                                return;
                            var mcb = new MessageChainBuilder();
                            mcb.Plain($"{blog["user"]!["screen_name"]}发了一条相关微博！");
                            if (mblogtype == 2)
                            {
                                //获取第一张图片发送
                                var first = blog["pic_infos"]![JArray.FromObject(blog["pic_ids"]!)[0]!.ToString()]!["large"]!["url"]!.ToString();
                                mcb.Plain(blogContent).ImageFromUrl(first);
                            }
                            else if (mblogtype == 0)
                            {
                                var pageInfo = (JObject?)blog["page_info"];
                                if (pageInfo != null)
                                {
                                    var objType = pageInfo["object_type"]!.ToString();
                                    if (objType == "video")
                                    {
                                        mcb.Plain(blogContent);
                                        mcb.Plain("视频链接：" + pageInfo["media_info"]!["h5_url"]!);
                                    }
                                }
                            }
                            else
                            {
                                mcb.Plain("未知类型微博，更多类型通知尽请期待！");
                            }
                            //需要发送通知则发送通知
                            if (Const.ConfigModel.WB.forwardGroup)
                            {
                                var groups = string.IsNullOrWhiteSpace(Const.ConfigModel.WB.group) ? Const.ConfigModel.QQ.group : Const.ConfigModel.WB.group;
                                var glist = groups.ToListV2();
                                //预留是否要at所有人
                                //if (false)
                                //{
                                //    mcb.AtAll();
                                //}
                                foreach (var group in glist)
                                {
                                    await Msg.SendGroupMsg(group, mcb.Build());
                                }
                            }
                            if (Const.ConfigModel.WB.forwardQQ)
                            {
                                var qqs = string.IsNullOrWhiteSpace(Const.ConfigModel.WB.qq) ? Msg.Admin : Const.ConfigModel.WB.qq;
                                var qlist = qqs.ToListV2();
                                foreach (var qq in qlist)
                                {
                                    await Msg.SendFriendMsg(qq, mcb.Build());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                e.AddLog();
                return;
            }
        }

        public static async Task SaveByUrl(string id)
        {
            try
            {
                await Msg.SendFriendMsg(Msg.Admin, "开始识别微博连接");
                var url = $"https://m.weibo.cn/statuses/show?id={id}";
                var handler = new HttpClientHandler() { UseCookies = true };
                HttpClient httpClient = new(handler);
                httpClient.DefaultRequestHeaders.Add("user-agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.64 Safari/537.36 Edg/101.0.1210.53");
                httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\" Not A; Brand\";v=\"99\", \"Chromium\";v=\"101\", \"Microsoft Edge\";v=\"101\"");
                httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "Windows");
                httpClient.DefaultRequestHeaders.Add("cookie", "_T_WM=77461816220; MLOGIN=0; WEIBOCN_FROM=1110006030; M_WEIBOCN_PARAMS=oid%3D4820613401674865%26luicode%3D20000061%26lfid%3D4820613401674865; XSRF-TOKEN=1a9e6c; mweibo_short_token=8dbe1b1ab1");
                var res = await httpClient.GetAsync(url);
                var content = await res.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(content)) return;
                var obj = JObject.Parse(content);
                var picsStr = obj["data"]!["pics"]!;
                var picList = JsonConvert.DeserializeObject<List<JObject>>(picsStr.ToString())!;
                await Msg.SendFriendMsg(Msg.Admin, $"检测到有{picList.Count}张图！开始进行识别保存！");
                foreach (var item in picList)
                {
                    var img = item["large"]!["url"]!.ToString();
                    await FatchFace(img, true);
                    Thread.Sleep(1000);
                }
            }
            catch (Exception e)
            {
                e.AddLog("微博识别失败，请检查微博ID是否存在！");
            }
        }

        public static async Task FatchFace(string url, bool save = false)
        {
            try
            {
                if (!Const.EnableModule.bd)
                {
                    dbContext = new();
                    await dbContext.Caches.AddAsync(new()
                    {
                        content = url,
                        type = 1
                    });
                    await dbContext.SaveChangesAsync();
                    await dbContext.DisposeAsync();
                    await Msg.SendFriendMsg(Msg.Admin, $"未启用人脸识别，加入待审核，目前有{Msg.Check.Count}张图片待审核");
                    return;
                }
                var face = await Baidu.IsFaceAndCount(url);
                if (face == 1)
                {
                    var score = await Baidu.FaceMatch(url);
                    if (score != Audit) await Msg.SendFriendMsg(Msg.Admin, $"人脸对比相似度：{score}");

                    if (score >= Audit && score < Similarity)
                    {
                        dbContext = new();
                        await dbContext.Caches.AddAsync(new()
                        {
                            content = url,
                            type = 1
                        });
                        await dbContext.SaveChangesAsync();
                        await dbContext.DisposeAsync();
                        await Msg.SendFriendMsg(Msg.Admin, $"相似度低于{Similarity}，加入待审核，目前有{Msg.Check.Count}张图片待审核");
                        return;
                    }
                    if (score >= Similarity && score <= 100)
                    {
                        if (!FileHelper.Save(url))
                        {
                            dbContext = new();
                            await dbContext.Caches.AddAsync(new()
                            {
                                content = url,
                                type = 1
                            });
                            await dbContext.SaveChangesAsync();
                            await dbContext.DisposeAsync();
                            await Msg.SendFriendMsg(Msg.Admin, $"保存失败，加入待审核，目前有{Msg.Check.Count}张图片待审核");
                        }
                        else
                        {
                            string msg = $"相似大于{Similarity}，已保存本地";
                            if (FileHelper.SaveAliyunDisk) msg += $"，正在上传至阿里云盘【{Const.ConfigModel.BD.albumName}】相册";
                            await Msg.SendFriendMsg(Msg.Admin, msg);
                        }
                        return;
                    }
                }
                else if (face > 1)
                {
                    dbContext = new();
                    await dbContext.Caches.AddAsync(new()
                    {
                        content = url,
                        type = 1
                    });
                    await dbContext.SaveChangesAsync();
                    await dbContext.DisposeAsync();
                    await Msg.SendFriendMsg(Msg.Admin, $"识别到多个人脸，加入待审核，目前有{Msg.Check.Count}张图片待审核");
                    return;
                }
                else if (face == 0 && save)
                {
                    dbContext = new();
                    await dbContext.Caches.AddAsync(new()
                    {
                        content = url,
                        type = 1
                    });
                    await dbContext.SaveChangesAsync();
                    await dbContext.DisposeAsync();
                    await Msg.SendFriendMsg(Msg.Admin, $"未识别到人脸，加入待审核，目前有{Msg.Check.Count}张图片待审核");
                }
                return;
            }
            catch (Exception e)
            {
                e.AddLog();
                return;
            }
        }
    }
}

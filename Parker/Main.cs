﻿using NetDimension.NanUI;
using NetDimension.NanUI.HostWindow;
using NetDimension.NanUI.JavaScript;

namespace ParkerBot
{
    public class Main : Formium
    {
        // 设置窗体样式类型
        public override HostWindowType WindowType => HostWindowType.System;
        // 指定启动 Url
        //public override string StartUrl => "http://parkerbot/";
        public override string StartUrl => "http://localhost:5173/";
        
        public Main()
        {
            // 在此处设置窗口样式
            Size = new Size(1024, 768);
            StartPosition = FormStartPosition.CenterParent;
            EnableSplashScreen = false;
            Subtitle = "帕克机器人";
            Title = "by 子墨Jaffoo";
        }

        protected override void OnReady()
        {
            //建立数据库
            var b = SqlHelper.CreateDBFile("config.db");
            //创建表（不存在时创建）
            //读取表结构sql
            if (b)
            {
                string path = Environment.CurrentDirectory + @"/wwwroot/sql/main.sql";
                var sql = File.ReadAllText(path);
                SqlHelper.ExecuteNonQuery(sql);
            }
            
            Const.SetCache();
            RegistJs();

            LoadEnd += PageLoadEnd;
        }

        private void PageLoadEnd(object? sender, NetDimension.NanUI.Browser.LoadEndEventArgs e)
        {
            //预留
        }

        private void RegistJs()
        {
            var obj = new JavaScriptObject();

            RegisterJavaScriptObject("main", obj);
        }
    }
}

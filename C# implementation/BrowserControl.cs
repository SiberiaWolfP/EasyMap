using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CefSharp;
using CefSharp.Wpf;
using CefSharp.Web;
using log4net;
using static EasyMap.Common;

namespace EasyMap
{
    // 浏览器控制，是用户与地图之间的中间层，负责获取用户输入并调用地图控制
    public class BrowserControl
    {
        private readonly ILog _log = LogManager.GetLogger("BrowserControl");

        public BrowserControl()
        {
            log4net.Config.XmlConfigurator.Configure();

            //设置语言环境
            var setting = new CefSettings
            {
                Locale = "zh-CN",
                // 缓存路径
                CachePath = "/BrowserCache",
                // 浏览器引擎的语言
                AcceptLanguageList = "zh-CN,zh;q=0.8",
                // 日志文件
                LogFile = "/LogData",
                PersistSessionCookies = true,
                UserAgent =
                    "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 Safari/537.36",
                UserDataPath = "/userData",
                // 设置网页可访问本地资源
                CefCommandLineArgs = { "--disable-web-security", "-–allow-file-access-from-files" }
            };
            Cef.Initialize(setting);
            // 网页路径
            string url = AppDomain.CurrentDomain.BaseDirectory + @"web\map.html";
            url = url.Replace("\\", "/").Replace(" ", "%20");
            // 实例化浏览器
            Browser = new ChromiumWebBrowser { Address = url };
            CefSharpSettings.LegacyJavascriptBindingEnabled = true;
            Browser.FrameLoadEnd += Browser_FrameLoadEnd;
            // 将js环境与程序绑定
            BindingOptions bo = new BindingOptions {CamelCaseJavascriptNames = false};
            Map = new MapControl();
            Browser.JavascriptObjectRepository.Register(@"server", Map, true, bo);
        }

        // 页面加载完成后
        public void Browser_FrameLoadEnd(object sender, CefSharp.FrameLoadEndEventArgs e)
        {
            _log.Debug("browser_FrameLoadEnd:" + e.Url);
            // Browser.GetBrowser().ShowDevTools();
            // var result = await Common.Browser.GetSourceAsync();
        }

        public void Btn_Click(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "BtnMoveToCenter":
                    Map.MoveToCenter();
                    break;
                case "BtnAddPoint":
                    if (!BtnAddPointIsActive)
                    {
                        Map.AddPlace();
                        BtnAddPointIsActive = true;
                        UpdateUi("BtnAddPoint", "停止添加地点");
                    }
                    else
                    {
                        Map.EndOperation();
                        BtnAddPointIsActive = false;
                        UpdateUi("BtnAddPoint", "添加地点");
                    }
                    break;
                case "BtnAddRoad":
                    if (!BtnAddRoadIsActive)
                    {
                        Map.AddRoad();
                        BtnAddRoadIsActive = true;
                        UpdateUi("BtnAddRoad", "停止添加道路");
                    }
                    else
                    {
                        Map.EndOperation();
                        BtnAddRoadIsActive = false;
                        UpdateUi("BtnAddRoad", "添加道路");
                    }
                    break;
                case "BtnDelRoad":
                    if (!BtnDelRoadIsActive)
                    {
                        Map.DelRoad();
                        BtnDelRoadIsActive = true;
                        UpdateUi("BtnDelRoad", "停止删除道路");
                    }
                    else
                    {
                        Map.EndOperation();
                        BtnDelRoadIsActive = false;
                        UpdateUi("BtnDelRoad", "删除道路");
                    }
                    break;
                case "ButtonPickMapStart":
                    Map.PickStart();
                    UpdateUi("StartImg", "images/map-blue.png");
                    break;
                case "ButtonPickMapEnd":
                    Map.PickEnd();
                    UpdateUi("EndImg", "images/map-blue.png");
                    break;
                case "BtnSearch":
                    break;
            }
        }

        public void Text_Changed(object sender, TextChangedEventArgs e)
        {

        }

        public void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // 使用位操作对设置赋值，若数据源选取了高德则第零位置1，选取了自定义则第1位置1
            switch (((CheckBox) sender).Name)
            {
                case "CheckBoxPointDataSourceAMap":
                    Properties.Settings.Default.pointDataSource |= 0x1;
                    break;
                case "CheckBoxPointDataSourceCustom":
                    Properties.Settings.Default.pointDataSource |= 0x2;
                    break;
                case "CheckBoxRoadDataSourceAMap":
                    Properties.Settings.Default.roadDataSource |= 0x1;
                    break;
                case "CheckBoxRoadDataSourceCustom":
                    Properties.Settings.Default.roadDataSource |= 0x2;
                    break;
            }
            Map.SetFeatures(Properties.Settings.Default.pointDataSource, 
                Properties.Settings.Default.roadDataSource);
            Properties.Settings.Default.Save();
        }

        public void CheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            // 使用位操作对设置赋值，若数据源取消选取了高德则第零位置0，选取了自定义则第1位置0
            switch (((CheckBox)sender).Name)
            {
                case "CheckBoxPointDataSourceAMap":
                    Properties.Settings.Default.pointDataSource &= ~0x1;
                    break;
                case "CheckBoxPointDataSourceCustom":
                    Properties.Settings.Default.pointDataSource &= ~0x2;
                    break;
                case "CheckBoxRoadDataSourceAMap":
                    Properties.Settings.Default.roadDataSource &= ~0x1;
                    break;
                case "CheckBoxRoadDataSourceCustom":
                    Properties.Settings.Default.roadDataSource &= ~0x2;
                    break;
            }
            Map.SetFeatures(Properties.Settings.Default.pointDataSource,
                Properties.Settings.Default.roadDataSource);
            Properties.Settings.Default.Save();
        }
    }
}

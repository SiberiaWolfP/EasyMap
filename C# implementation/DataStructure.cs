using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Xml.Serialization;
using CefSharp.Wpf;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static EasyMap.Common;

namespace EasyMap
{
    public delegate void OpenDialogDelegate(JObject json);

    public delegate void UpdateUiDelegate(string objName, string msg);

    // 全局变量
    public static class Common
    {
        // UI线程同步操作
        public static AsyncOperation UiOperation;
        // UI线程打开地点添加对话框委托
        public static OpenDialogDelegate OpenDialog { set; get; }
        public static UpdateUiDelegate UpdateUi { set; get; }
        // 按钮状态指示变量
        public static bool BtnAddPointIsActive { set; get; } = false;
        public static bool BtnAddRoadIsActive { set; get; } = false;
        public static bool BtnDelPointIsActive { set; get; } = false;
        public static bool BtnDelRoadIsActive { set; get; } = false;

        // 当前最大的地点ID
        public static int LastPlaceId { set; get; }
        // 当前最大的道路点ID
        public static int LastRoadPointId { set; get; }
        // 当前最大的道路ID
        public static int LastRoadId { set; get; }
        // 浏览器
        public static ChromiumWebBrowser Browser { set; get; }
        // 浏览器控制
        public static BrowserControl BsControl { set; get; }
        // 地图交互
        public static MapControl Map { set; get; }
        // 地点表
        public static List<PlacePoint> Places { set; get; } = new List<PlacePoint>();
        // 主道路表
        // key: int 道路起点ID
        // value: RoadDetail 道路信息
        public static MyDictionary<int, List<RoadDetail>> RoadList { set; get; } = new MyDictionary<int, List<RoadDetail>>();
        // 所有道路上的点数据
        // key: int 点ID
        // value: RoadPoint 点
        public static MyDictionary<int, RoadPoint> RoadPoints { set; get; } = new MyDictionary<int, RoadPoint>();
        // 道路所含的点表
        // key: int 道路ID
        // value: List<int> 点列表，表示一条路上的所有点
        public static MyDictionary<int, List<int>> Roads { set; get; } = new MyDictionary<int, List<int>>();

        //地球半径，单位米
        private const double EARTH_RADIUS = 6378137;

        /// <summary>
        /// <para>计算两点位置的距离，返回两点的距离，单位：米</para>
        /// <para>该公式为GOOGLE提供，误差小于0.2米</para>
        /// </summary>
        /// <param name="lng1">第一点经度</param>
        /// <param name="lat1">第一点纬度</param>        
        /// <param name="lng2">第二点经度</param>
        /// <param name="lat2">第二点纬度</param>
        /// <returns>两点间的距离</returns>
        public static double GetDistance(double lng1, double lat1, double lng2, double lat2)
        {
            double radLat1 = Rad(lat1);
            double radLng1 = Rad(lng1);
            double radLat2 = Rad(lat2);
            double radLng2 = Rad(lng2);
            double a = radLat1 - radLat2;
            double b = radLng1 - radLng2;
            double result = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) + Math.Cos(radLat1) * Math.Cos(radLat2) * Math.Pow(Math.Sin(b / 2), 2))) * EARTH_RADIUS;
            return result;
        }

        /// <summary>
        /// 经纬度转化成弧度
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        private static double Rad(double d)
        {
            return (double)d * Math.PI / 180d;
        }

        public static void LoadData()
        {
            try
            {
                FileStream stream = new FileStream("./data/Places.xml", FileMode.Open, FileAccess.Read, FileShare.Read);
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<PlacePoint>));
                Places = (List<PlacePoint>) xmlSerializer.Deserialize(stream);
                stream.Close();
                LastPlaceId = Places.Last().Id;
                // Console.WriteLine(JsonConvert.SerializeObject(RoadList));
                // Console.WriteLine(JsonConvert.SerializeObject(RoadPoints));
                // Console.WriteLine(JsonConvert.SerializeObject(Roads));
            }
            catch (IOException e)
            {
                Console.WriteLine(e);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            try
            {
                FileStream stream = new FileStream("./data/RoadList.xml", FileMode.Open, FileAccess.Read, FileShare.Read);
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(MyDictionary<int, List<RoadDetail>>));
                RoadList = (MyDictionary<int, List<RoadDetail>>)xmlSerializer.Deserialize(stream);
                stream.Close();
            }
            catch (IOException e)
            {
                Console.WriteLine(e);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            try
            {
                FileStream stream = new FileStream("./data/RoadPoints.xml", FileMode.Open, FileAccess.Read, FileShare.Read);
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(MyDictionary<int, RoadPoint>));
                RoadPoints = (MyDictionary<int, RoadPoint>)xmlSerializer.Deserialize(stream);
                stream.Close();
                stream = new FileStream("./data/Roads.xml", FileMode.Open, FileAccess.Read, FileShare.Read);
                xmlSerializer = new XmlSerializer(typeof(MyDictionary<int, List<int>>));
                Roads = (MyDictionary<int, List<int>>)xmlSerializer.Deserialize(stream);
                stream.Close();
                LastRoadPointId = RoadPoints.Keys.Max();
                LastRoadId = Roads.Keys.Max();
            }
            catch (IOException e)
            {
                Console.WriteLine(e);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void SaveData()
        {
            try
            {
                FileStream stream = new FileStream("./data/Places.xml", FileMode.Create, FileAccess.Write, FileShare.None);
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<PlacePoint>));
                xmlSerializer.Serialize(stream, Places);
                stream.Close();
                stream = new FileStream("./data/RoadList.xml", FileMode.Create, FileAccess.Write, FileShare.None);
                xmlSerializer = new XmlSerializer(typeof(MyDictionary<int, List<RoadDetail>>));
                xmlSerializer.Serialize(stream, RoadList);
                stream.Close();
                stream = new FileStream("./data/RoadPoints.xml", FileMode.Create, FileAccess.Write, FileShare.None);
                xmlSerializer = new XmlSerializer(typeof(MyDictionary<int, RoadPoint>));
                xmlSerializer.Serialize(stream, RoadPoints);
                stream.Close();
                stream = new FileStream("./data/Roads.xml", FileMode.Create, FileAccess.Write, FileShare.None);
                xmlSerializer = new XmlSerializer(typeof(MyDictionary<int, List<int>>));
                xmlSerializer.Serialize(stream, Roads);
                stream.Close();
            }
            catch (IOException e)
            {
                Console.WriteLine(e);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    public class PlacePoint : Point
    {
        public string Name { get; set; }

        public PlacePoint() { }

        public PlacePoint(string name, double longitude, double latitude) : base(longitude, latitude)
        {
            this.Id = LastPlaceId + 1;
            LastPlaceId = this.Id;
            this.Name = name;
        }
    }

    public class RoadPoint : Point
    {
        public RoadPoint() { }

        public RoadPoint(double longitude, double latitude) : base(longitude, latitude)
        {
            this.Id = LastRoadPointId + 1;
            LastRoadPointId = this.Id;
        }
    }

    public class RoadDetail
    {
        // 道路ID
        public int Id { get; set; }
        // 道路长度
        public double Length { get; set; }
        // 道路名
        public string Name { get; set; }
        // 道路终点ID
        public int EndPointId { get; set; }

        public RoadDetail() { }

        public RoadDetail(int endPoint, int roadId,string name = null)
        {
            this.Id = roadId;
            for (int i = 1; i < Roads[roadId].Count; i++)
            {
                this.Length += GetDistance(RoadPoints[Roads[roadId][i - 1]].Longitude,
                    RoadPoints[Roads[roadId][i - 1]].Latitude,
                    RoadPoints[Roads[roadId][i]].Longitude,
                    RoadPoints[Roads[roadId][i]].Latitude);
            }
            this.EndPointId = endPoint;
            this.Name = name;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public override bool Equals(object obj)
        {
            RoadDetail point = obj as RoadDetail;
            if (point == null) return false;
            return Id == point.Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }

    public abstract class Point
    {
        public int Id { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }

        public Point() { }

        public Point(double longitude, double latitude)
        {
            this.Longitude = longitude;
            this.Latitude = latitude;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public override bool Equals(object obj)
        {
            Point point = obj as Point;
            if (point == null) return false;
            return Id == point.Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }

}

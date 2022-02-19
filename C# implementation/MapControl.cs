using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static EasyMap.Common;

namespace EasyMap
{
    public class MapControl
    {
        public MapControl()
        {

        }

        public String Show(String msg)
        {
            Console.WriteLine(msg);
            return "success";
        }

        public void Debug(String msg)
        {
            Console.WriteLine(msg);
        }

        public void ProgramToJs()
        {
            string data = "参数";
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync("show('" + data + "')");
            t.Wait();
            Console.WriteLine($@"返回值：{t.Result.Result}");
        }

        public void MoveToCenter()
        {
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync("moveToCenter()");
        }

        public void SetFeatures(int pointFeature, int roadFeature)
        {
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"setFeatures('{pointFeature}','{roadFeature}')");
        }

        public void DisplayPath(List<List<double>> path)
        {
            // List<List<double>> pathList = new List<List<double>>();
            // pathList.Add(new List<double> {startLng, startLat});
            // foreach (int i in path)
            // {
                // pathList.Add(new List<double> { RoadPoints[i].Longitude, RoadPoints[i].Latitude });
            // }
            // pathList.Add(new List<double> { endLng, endLat });
            string str = JArray.FromObject(path).ToString(Formatting.None);
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"displayPath('{str}')");
        }

        // 前端调用，地图加载完成
        public void MapLoaded()
        {
            Map.SetFeatures(Properties.Settings.Default.pointDataSource,
                Properties.Settings.Default.roadDataSource);
            InitMap();
            // DisplayPath(RoadPoints[Roads[1].First()].Longitude, RoadPoints[Roads[1].First()].Latitude,
                // RoadPoints[Roads[1].Last()].Longitude, RoadPoints[Roads[1].Last()].Latitude, Roads[1]);
        }

        // 将保存的数据全部送到前端进行绘制
        public void InitMap()
        {
            JObject jObject = new JObject
            {
                ["Places"] = JArray.FromObject(Places),
                ["Roads"] = JObject.FromObject(Roads),
                ["RoadPoints"] = JObject.FromObject(RoadPoints)
            };
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"initMap('{jObject.ToString(Formatting.None)}')");
            t.Wait();
        }

        public JObject SearchClosetPoint(double longitude, double latitude)
        {
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"searchClosetPoint('{longitude}', '{latitude}')");
            t.Wait();
            JObject result = JObject.Parse((string)t.Result.Result);
            Console.WriteLine(result);
            return result;
        }

        public void PickStart()
        {
            JObject jsonJObject = new JObject();
            jsonJObject["class"] = "point";
            jsonJObject["type"] = "start";
            string parameter = jsonJObject.ToString(Formatting.None);
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"startAdd('{parameter}')");
        }

        public void PickEnd()
        {
            JObject jsonJObject = new JObject();
            jsonJObject["class"] = "point";
            jsonJObject["type"] = "end";
            string parameter = jsonJObject.ToString(Formatting.None);
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"startAdd('{parameter}')");
        }

        public void EndPick(string type, string msg)
        {
            JObject jObject = JObject.Parse(msg);
            if (type == "start")
            {
                UpdateUi("TextBoxStartPoint", jObject["lng"] + ";" + jObject["lat"]);
                UpdateUi("StartImg", "images/map.png");
            }
            else if (type == "end")
            {
                UpdateUi("TextBoxEndPoint", jObject["lng"] + ";" + jObject["lat"]);
                UpdateUi("EndImg", "images/map.png");
            }
        }

        /**
         * point 地点数据
         */
        public void AddPlace()
        {
            JObject jsonJObject = new JObject();
            jsonJObject["class"] = "point";
            jsonJObject["type"] = "normal";
            string parameter = jsonJObject.ToString(Formatting.None);
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"startAdd('{parameter}')");
        }

        // 地图添加地点完成，将坐标传入后台处理
        public void AddPlaceEnd(string position)
        {
            Console.WriteLine($@"后台收到前端数据,方法AddPlaceEnd,数据:{position}");
            JObject jsonJObject = JObject.Parse(position);
            OpenDialog(jsonJObject);
        }

        // 添加地点成功，更新地图
        public void AddPlaceSuccess(string message)
        {
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"addPointSuccess('{message}')");
        }

        // 添加地点失败，删除地图上刚添加的点
        public void AddPlaceFail()
        {
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"addPointFail()");
        }

        // 结束添加地点
        public void EndAddPlace()
        {
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"endAddPoint()");
        }

        // 后台调用，开始添加道路
        public void AddRoad()
        {
            JObject jsonJObject = new JObject();
            jsonJObject["class"] = "road";
            string parameter = jsonJObject.ToString(Formatting.None);
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"startAdd('{parameter}')");
        }

        // 地图添加道路完成，将坐标传入后台处理
        public void AddRoadEnd(string positions, string crossPoints)
        {
            // 新路折点组成的点列表
            JArray jsonJArray = JArray.Parse(positions);
            // 新路与老路的交叉点
            JObject jsonJObject = JObject.Parse(crossPoints);
            // Console.WriteLine($@"后台收到前端数据,方法AddRoadEnd,数据:{jsonJArray}, {jsonJObject}");
            List<int> roadsId = new List<int>();

            for (int i = 0; i < jsonJArray.Count; i++)
            {
                // 如果新路中的该点为交叉点，则需要考虑是否将新路或老路进行截断
                if (jsonJObject.ContainsKey(i.ToString()))
                {
                    int oldRoadId = (int) jsonJObject[i.ToString()];
                    // 调用高德数学计算API，求得交叉点到老路上距离最近的点,并同时获取点所在的小路段
                    JObject msg = new JObject
                    {
                        ["p"] = new JArray {(double) jsonJArray[i]["lng"], (double) jsonJArray[i]["lat"]}
                    };
                    JArray array = new JArray();
                    foreach (int j in Roads[oldRoadId])
                    {
                        array.Add(new JArray { RoadPoints[j].Longitude, RoadPoints[j].Latitude });
                    }

                    // Console.WriteLine(i);
                    msg["line"] = array;
                    Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"closestOnLine('{msg.ToString(Formatting.None)}')");
                    t.Wait();
                    // 求得的最近点
                    msg = JObject.Parse((string) t.Result.Result);
                    // Console.WriteLine(msg);
                    // 最近点所在的小路段端点在大路段上的索引
                    int point1Index = (int)msg["segment"][0], point2Index = (int)msg["segment"][1];
                    // 将最近点与老路折点进行比较，判断是交叉点在折点还是路段中央
                    if (Common.GetDistance((double) msg["p"][0], (double) msg["p"][1],
                        RoadPoints[Roads[oldRoadId][point1Index]].Longitude,
                        RoadPoints[Roads[oldRoadId][point1Index]].Latitude) < 3)
                    {
                        CutOffRoad(oldRoadId, point1Index);
                        roadsId.Add(Roads[oldRoadId][point1Index]);
                    }
                    else if (Common.GetDistance((double)msg["p"][0], (double)msg["p"][1],
                        RoadPoints[Roads[oldRoadId][point2Index]].Longitude,
                        RoadPoints[Roads[oldRoadId][point2Index]].Latitude) < 3)
                    {
                        CutOffRoad(oldRoadId, point2Index);
                        roadsId.Add(Roads[oldRoadId][point2Index]);
                    }
                    // 若最近点离小路段端点过远，则需要将老路进行截断
                    else
                    {
                        // 将交叉点作为新道路点添加
                        RoadPoint newPoint = new RoadPoint((double)msg["p"][0], (double)msg["p"][1]);
                        RoadPoints.Add(newPoint.Id, newPoint);
                        CutOffRoad(oldRoadId, point1Index, point2Index, newPoint);
                        roadsId.Add(newPoint.Id);
                    }
                    // 如果该交叉点之前有点，说明该交叉点在新路中央，则需把新路由交叉点截断，之前的路段成为新路
                    if (roadsId.Count > 1)
                    {
                        List<int> newRoad = new List<int>(roadsId.ToArray());
                        Roads.Add(LastRoadId + 1, newRoad);
                        LastRoadId++;
                        AddToRoadList(roadsId.First(), roadsId.Last());
                        roadsId.RemoveRange(0, roadsId.Count - 1);

                        JObject obj = new JObject();
                        JArray arr = new JArray();
                        foreach (int i1 in newRoad)
                        {
                            arr.Add(new JArray {RoadPoints[i1].Longitude, RoadPoints[i1].Latitude});
                        }

                        obj["id"] = LastRoadId;
                        obj["path"] = arr;
                        Task<CefSharp.JavascriptResponse> t1 = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"addPloyLine('{obj.ToString(Formatting.None)}')");
                        t1.Wait();
                    }
                }
                else
                {
                    RoadPoint roadPoint = new RoadPoint((double)jsonJArray[i]["lng"], (double)jsonJArray[i]["lat"]);
                    RoadPoints.Add(roadPoint.Id, roadPoint);
                    roadsId.Add(roadPoint.Id);
                }

                // 如果已经处理到了最后一个点且未添加到路中的点不为1个，则将剩余的点组成一条新路
                if (i == jsonJArray.Count - 1 && roadsId.Count != 1)
                {
                    Roads.Add(LastRoadId + 1, roadsId);
                    LastRoadId++;
                    AddToRoadList(roadsId.First(), roadsId.Last());

                    JObject obj = new JObject();
                    JArray arr = new JArray();
                    foreach (int i1 in roadsId)
                    {
                        arr.Add(new JArray { RoadPoints[i1].Longitude, RoadPoints[i1].Latitude });
                    }

                    obj["id"] = LastRoadId;
                    obj["path"] = arr;
                    Task<CefSharp.JavascriptResponse> t1 = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"addPloyLine('{obj.ToString(Formatting.None)}')");
                    t1.Wait();
                }
            }

            // Console.WriteLine(JObject.FromObject(RoadList).ToString());
            Console.WriteLine(JObject.FromObject(Roads).ToString());
            // InitMap();
            AddRoadSuccess(LastRoadId);
        }

        // 从已有点开始将一条路分成两段
        public void CutOffRoad(int roadId, int pointIndex)
        {
            // 假设道路由交叉点分为左右段
            // 被截断道路的起终点
            int oldStartId = Roads[roadId].First(), oldEndId = Roads[roadId].Last();

            // 右段新路
            List<int> newRoad = new List<int>();
            // 注意需要ToArray转换进行克隆
            newRoad.AddRange(Roads[roadId].GetRange(pointIndex, Roads[roadId].Count - pointIndex).ToArray());
            // 添加右段新路
            Roads.Add(LastRoadId + 1, newRoad);
            LastRoadId++;

            // 左段路在老路的表的基础上减去右段路
            // 从老路点列表中减去右段路
            Roads[roadId].RemoveRange(pointIndex, Roads[roadId].Count - pointIndex);

            double roadLength = 0;
            // 将已有路表中左段路所在的原道路起点对应的表项修改
            bool MyPredicate(RoadDetail m) => m.EndPointId == oldEndId;
            // 从主表中取得老路左端点开头的表项
            RoadDetail oldDetail = RoadList[oldStartId].Find(MyPredicate);
            oldDetail.EndPointId = RoadPoints[Roads[roadId][pointIndex]].Id;
            oldDetail.Length = 0;
            for (int n = 1; n < Roads[roadId].Count; n++)
            {
                roadLength += Common.GetDistance(RoadPoints[Roads[roadId][n - 1]].Longitude,
                    RoadPoints[Roads[roadId][n - 1]].Latitude,
                    RoadPoints[Roads[roadId][n]].Longitude,
                    RoadPoints[Roads[roadId][n]].Latitude);
            }
            oldDetail.Length = roadLength;
            // 在主表中添加左段形成的新路对应的双向表项
            RoadList.Add(RoadPoints[Roads[roadId][pointIndex]].Id, new List<RoadDetail> { new RoadDetail(oldStartId, roadId) });

            roadLength = 0;
            // 将已有路表中右路所在的原道路起点对应的表项修改
            bool MyPredicate2(RoadDetail m) => m.EndPointId == oldStartId;
            // 从主表中取得老路左端点开头的表项
            oldDetail = RoadList[oldEndId].Find(MyPredicate2);
            oldDetail.Id = LastRoadId;
            oldDetail.EndPointId = RoadPoints[Roads[roadId][pointIndex]].Id;
            oldDetail.Length = 0;
            for (int n = 1; n < Roads[LastRoadId].Count; n++)
            {
                roadLength += Common.GetDistance(RoadPoints[Roads[LastRoadId][n - 1]].Longitude,
                    RoadPoints[Roads[LastRoadId][n - 1]].Latitude,
                    RoadPoints[Roads[LastRoadId][n]].Longitude,
                    RoadPoints[Roads[LastRoadId][n]].Latitude);
            }
            oldDetail.Length = roadLength;
            // 在主表中添加右段形成的新路对应的双向表项
            RoadList[RoadPoints[Roads[roadId][pointIndex]].Id].Add(new RoadDetail(Roads[roadId].Last(), LastRoadId));

            // 将道路更改发送到地图
            JObject obj = new JObject();
            obj["modifyRoadId"] = roadId;
            JArray array = new JArray();
            foreach (int i in Roads[roadId])
            {
                array.Add(new JArray { RoadPoints[i].Longitude, RoadPoints[i].Latitude });
            }

            obj["modifyRoadPath"] = array;
            obj["newRoadId"] = LastRoadId;
            array = new JArray();
            foreach (int i in Roads[LastRoadId])
            {
                array.Add(new JArray { RoadPoints[i].Longitude, RoadPoints[i].Latitude });
            }

            obj["newRoadPath"] = array;
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"updateRoad('{obj.ToString(Formatting.None)}')");
        }

        // 从新点开始将一条道路分成两段
        public void CutOffRoad(int roadId, int point1Index, int point2Index ,RoadPoint newPoint)
        {
            // 假设道路由交叉点分为左右段
            // 被截断道路的起终点
            int oldStartId = Roads[roadId].First(), oldEndId = Roads[roadId].Last();
            Console.WriteLine("oldStartId:" + oldStartId + " " + "oldEndId:" + oldEndId);

            // 右段新路
            List<int> newRoad = new List<int>();
            newRoad.Add(newPoint.Id);
            // 注意需要ToArray转换进行克隆
            newRoad.AddRange(Roads[roadId].GetRange(point2Index, Roads[roadId].Count - point2Index).ToArray());
            // 添加右段新路
            Roads.Add(LastRoadId + 1, newRoad);
            LastRoadId++;

            // 左段路在老路的表的基础上减去右段路
            // 从老路点列表中减去右段路
            Roads[roadId].RemoveRange(point2Index, Roads[roadId].Count - point2Index);
            Roads[roadId].Add(newPoint.Id);

            double roadLength = 0;
            // 将已有路表中左段路所在的原道路起点对应的表项修改
            bool MyPredicate(RoadDetail m) => m.EndPointId == oldEndId;
            // 从主表中取得老路左端点开头的表项
            RoadDetail oldDetail = RoadList[oldStartId].Find(MyPredicate);
            oldDetail.EndPointId = newPoint.Id;
            oldDetail.Length = 0;
            for (int n = 1; n < Roads[roadId].Count; n++)
            {
                roadLength += Common.GetDistance(RoadPoints[Roads[roadId][n - 1]].Longitude,
                    RoadPoints[Roads[roadId][n - 1]].Latitude,
                    RoadPoints[Roads[roadId][n]].Longitude,
                    RoadPoints[Roads[roadId][n]].Latitude);
            }
            oldDetail.Length = roadLength;
            // 在主表中添加左段形成的新路对应的双向表项
            RoadList.Add(newPoint.Id, new List<RoadDetail> {new RoadDetail(oldStartId, roadId)});

            roadLength = 0;
            // 将已有路表中右路所在的原道路起点对应的表项修改
            bool MyPredicate2(RoadDetail m) => m.EndPointId == oldStartId;
            // 从主表中取得老路左端点开头的表项
            oldDetail = RoadList[oldEndId].Find(MyPredicate2);
            oldDetail.Id = LastRoadId;
            oldDetail.EndPointId = newPoint.Id;
            oldDetail.Length = 0;
            for (int n = 1; n < Roads[LastRoadId].Count; n++)
            {
                roadLength += Common.GetDistance(RoadPoints[Roads[LastRoadId][n - 1]].Longitude,
                    RoadPoints[Roads[LastRoadId][n - 1]].Latitude,
                    RoadPoints[Roads[LastRoadId][n]].Longitude,
                    RoadPoints[Roads[LastRoadId][n]].Latitude);
            }
            oldDetail.Length = roadLength;
            // 在主表中添加右段形成的新路对应的双向表项
            RoadList[newPoint.Id].Add(new RoadDetail(Roads[roadId].Last(), LastRoadId));
            
            // 将道路更改发送到地图
            JObject obj = new JObject();
            obj["modifyRoadId"] = roadId;
            JArray array = new JArray();
            foreach (int i in Roads[roadId])
            {
                array.Add(new JArray {RoadPoints[i].Longitude, RoadPoints[i].Latitude});
            }

            obj["modifyRoadPath"] = array;
            obj["newRoadId"] = LastRoadId;
            array = new JArray();
            foreach (int i in Roads[LastRoadId])
            {
                array.Add(new JArray { RoadPoints[i].Longitude, RoadPoints[i].Latitude });
            }

            obj["newRoadPath"] = array;
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"updateRoad('{obj.ToString(Formatting.None)}')");
        }

        public void AddToRoadList(int startId, int endId)
        {
            // 由道路的起点和终点分别在主道路表中添加数据
            RoadDetail roadDetailStart = new RoadDetail(endId, LastRoadId);
            if (RoadList.ContainsKey(startId))
            {
                RoadList[startId].Add(roadDetailStart);
            }
            else
            {
                RoadList.Add(startId, new List<RoadDetail>() { roadDetailStart });
            }
            RoadDetail roadDetailEnd = new RoadDetail(startId, LastRoadId);
            if (RoadList.ContainsKey(endId))
            {
                RoadList[endId].Add(roadDetailEnd);
            }
            else
            {
                RoadList.Add(endId, new List<RoadDetail>() { roadDetailEnd });
            }
        }

        public void AddRoadSuccess(int roadId)
        {
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"addRoadSuccess('{roadId}')");
        }

        // 后台调用，停止添加道路
        public void EndAddRoad()
        {
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"endAddRoad()");
        }

        // 后台启动，通知js删除道路
        public void DelRoad()
        {
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"delRoad()");
        }

        // js启动，传入被选取删除的道路ID
        public void DelRoadEnd(int roadId)
        {
            Console.WriteLine($@"删除道路，道路ID： {roadId}");
            // 从点集中删除道路上的点
            foreach (int i in Roads[roadId])
            {
                RoadPoints.Remove(i);
            }

            int startId = Roads[roadId].First();
            int endId = Roads[roadId].Last();
            Console.WriteLine(JArray.FromObject(Roads[roadId]));
            Console.WriteLine(JObject.FromObject(RoadList));
            // 从已有路表中将选定路删除
            bool MyPredicate(RoadDetail m) => m.EndPointId == endId;
            // 从主表中取得老路左端点开头的表项
            RoadDetail oldDetail = RoadList[startId].Find(MyPredicate);
            RoadList[startId].Remove(oldDetail);
            if (RoadList[startId].Count == 0) RoadList.Remove(startId);
            bool MyPredicate2(RoadDetail m) => m.EndPointId == startId;
            oldDetail = RoadList[endId].Find(MyPredicate2);
            RoadList[endId].Remove(oldDetail);
            if (RoadList[endId].Count == 0) RoadList.Remove(endId);

            Roads.Remove(roadId);
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"delRoadSuccess('{roadId}')");
        }

        public void EndOperation()
        {
            Task<CefSharp.JavascriptResponse> t = Browser.GetBrowser().MainFrame.EvaluateScriptAsync($"endOperation()");
        }

        public double GetDistance(string msg)
        {
            JObject jObject = JObject.Parse(msg);
            return Common.GetDistance((double) jObject["point1"][0], (double) jObject["point1"][1],
                (double) jObject["point2"][0], (double) jObject["point2"][1]);
        }
    }
}

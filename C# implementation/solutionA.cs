using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using static EasyMap.Common;




namespace EasyMap
{
    //检查点
    public class Checkpoint
    {
        public int PointId { get; set; }
        public double F { get; set; }
        public double G { get; set; }
        public double H { get; set; }
        // 父节点
        public int ParentId { get; set; } = 0;

        public Checkpoint() { }

        public Checkpoint(int id, double G, double H)
        {
            this.PointId = id;
            this.G = G;
            this.H = H;
            this.F = G + H;
        }
    }

    //A*算法寻路
    public class SolutionByA
    {
        // 起点
        public RoadPoint FirstPoint { get; set; } = new RoadPoint();
        // 终点
        public RoadPoint Endpoint { get; set; } = new RoadPoint();
        // 已走列表
        public List<int> CloseList { get; set; } = new List<int>();
        // 最短路径长度
        public double SumLength { get; set; } = 0.0;
        // 最短路径
        public List<int> Path { get; set; } = new List<int>();
        // Open表
        public List<int> OpenList { get; set; } = new List<int>();
        public Dictionary<int, Checkpoint> Points = new Dictionary<int, Checkpoint>();

        //构造方法RoadPoint first, RoadPoint end
        public SolutionByA(RoadPoint first, RoadPoint end)
        {
            this.FirstPoint = first;
            this.Endpoint = end;

            double G = 0;
            double H = GetDistance(first.Longitude, first.Latitude, end.Longitude, end.Latitude);
            Checkpoint point = new Checkpoint(first.Id, G, H);
            OpenList.Add(point.PointId);
            Points.Add(point.PointId, point);

        }


        // f(x)=检查点与目标点的距离，选择小的点
        public void SolutionBy()
        {
            // 起点道路的端点
            while (true)
            {
                Checkpoint nextPoint = null;
                double minF = double.MaxValue;
                // 查找Open表中F值最小的一个
                foreach (int i in OpenList)
                {
                    if (Points[i].F <= minF)
                    {
                        minF = Points[i].F;
                        nextPoint = Points[i];
                    }
                }
                CloseList.Add(nextPoint.PointId);
                OpenList.Remove(nextPoint.PointId);

                // 处理当前点的后继节点
                GetNextCheckPoint(nextPoint.PointId);
                // 搜索成功
                if (OpenList.Contains(Endpoint.Id))
                {
                    Checkpoint point = Points[Endpoint.Id];
                    while (point.ParentId != 0)
                    {
                        int roadId = 0;
                        foreach (RoadDetail roadDetail in RoadList[point.PointId])
                        {
                            if (roadDetail.EndPointId == point.ParentId)
                            {
                                roadId = roadDetail.Id;
                                SumLength += roadDetail.Length;
                            } 
                        }
                        Path.Add(roadId);
                        point = Points[point.ParentId];
                    }
                    Path.Reverse();
                    // 将起始点所在的道路也添加
                    foreach (RoadDetail roadDetail in RoadList[point.PointId])
                    {
                        if (roadDetail.EndPointId == Path[0])
                        {
                            SumLength += roadDetail.Length;
                            Path.Insert(0, roadDetail.Id);
                        }
                    }

                    break;
                }
                // 搜索失败
                if (OpenList.Count == 0)
                {
                    SumLength = double.MaxValue;
                    return;
                }
            }

            DisplayRoad();

        }

        // 检查下一个点
        public void GetNextCheckPoint(int pointId)
        {
            // 检查点下一步可以走的所有点
            List<Checkpoint> nextPoints = new List<Checkpoint>();

            // 获取下一步检查点队列
            foreach (RoadDetail next in RoadList[pointId])
            {
                // 搜索禁忌列表(已经访问的点集)
                bool x = CloseList.Contains(next.EndPointId);
                // 如果该点已经被访问过
                if (x)
                    continue;
                // 如果该点所引出的路又指回上一个点，说明由这个点走向死路
                if (RoadList[next.EndPointId].Count == 1 && RoadList[next.EndPointId][0].EndPointId == pointId) 
                    continue;

                double G = Points[pointId].G + next.Length;
                double H = GetDistance(RoadPoints[next.EndPointId].Longitude, RoadPoints[next.EndPointId].Latitude,
                    Endpoint.Longitude, Endpoint.Latitude);
                // 如果该点未被访问过，加入OpenList并记录其GH值
                if (OpenList.Contains(next.EndPointId) != true)
                {
                    OpenList.Add(next.EndPointId);
                    Checkpoint checkpoint = new Checkpoint(next.EndPointId, G, H);
                    checkpoint.ParentId = pointId;
                    if (Points.ContainsKey(next.EndPointId))
                    {
                        Points[next.EndPointId] = checkpoint;
                    }
                    else
                    {
                        Points.Add(next.EndPointId, checkpoint);
                    }
                }
                // 如果下一个点在Open表中，检查该点所形成的路径是否更好
                else
                {
                    if (G <= Points[next.EndPointId].G)
                    {
                        Points[next.EndPointId].ParentId = pointId;
                        Points[next.EndPointId].G = G;
                        Points[next.EndPointId].H = H;
                        Points[next.EndPointId].F = G + H;
                    }
                }

            }

        }

        // 在控制台显示路径
        public void DisplayRoad()
        {
            System.Console.WriteLine("\nA*算法:\n");
            foreach (int c in Path)
            {
                System.Console.WriteLine(c + "---》");
            }
            System.Console.WriteLine("路程：" + SumLength + "\n");
        }

    }
}

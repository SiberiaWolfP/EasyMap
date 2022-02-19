using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EasyMap.Common;

namespace EasyMap
{
    //基类
    public class SolutionAnt
    {
        // 蚂蚁数量
        public int AntCount { get; set; } = new int();
        // 信息启发式因子,表征信息素重要程度的参数
        public Double Alpha { get; set; } = new Double();
        // 期望启发式因子,表征启发式因子重要程度的参数
        public Double Beta { get; set; } = new Double();
        // 信息素蒸发的参数
        public Double Rho { get; set; } = new Double();
        // 距离数据，不一定是对称矩阵
        public MyDictionary<int, MyDictionary<int, double>> Distance { get; set; } = new MyDictionary<int, MyDictionary<int, double>>();
        // 最大迭代次数
        public int NcMax { get; set; } = new int();
        // 最好的解个数，取最优解列表中个数的数目，可以作为备用方案
        public int BetterPlanCount { get; set; } = new int();

        private List<Ant> AntList = new List<Ant>();
        // 信息素浓度
        public MyDictionary<int, MyDictionary<int, double>> InfoT { get; set; } = new MyDictionary<int, MyDictionary<int, double>>();
        // 起点(道路的端点)
        public RoadPoint FirstPoint { get; set; } = new RoadPoint();
        // 终点(道路的端点)
        public RoadPoint Endpoint { get; set; } = new RoadPoint();
        // 最好的路径
        public List<int> BestWay { get; set; } = new List<int>();
        public double BestWayLength = 0;


        // 构造函数
        // <param name="m">蚂蚁数量</param>
        // <param name="a">信息启发式因子</param>
        // <param name="b">期望启发式因子</param>
        // <param name="p">信息素蒸发的参数</param>
        // <param name="distance">距离数据</param>
        // <param name="NCMax">最大迭代次数</param>
        // <param name="planCount">最优解的个数</param>
        public SolutionAnt(int m, double a, double b, double p, int NCMax, RoadPoint first, RoadPoint end, int firstroadid, int endroadid, int planCount = 10)
        {
            this.AntCount = m;
            this.Alpha = a;
            this.Beta = b;
            this.Rho = p;
            this.NcMax = NCMax;
            this.BetterPlanCount = planCount;
            this.FirstPoint = first;
            this.Endpoint = end;
            AntList = new List<Ant>();

            foreach (KeyValuePair<int, List<RoadDetail>> K in RoadList)
            {
                if (!Distance.ContainsKey(K.Key))
                {
                    MyDictionary<int, double> n = new MyDictionary<int, double>();

                    foreach (RoadDetail r in K.Value)
                    {
                        if (!n.ContainsKey(r.EndPointId))
                        {
                            n.Add(r.EndPointId, r.Length);
                        }
                    }
                    Distance.Add(K.Key, n);
                }

            }
            //初始化信息素矩阵
            foreach (KeyValuePair<int, List<RoadDetail>> K in RoadList)
            {
                if (!InfoT.ContainsKey(K.Key))
                {
                    MyDictionary<int, double> n = new MyDictionary<int, double>();

                    foreach (RoadDetail r in K.Value)
                    {
                        if (!n.ContainsKey(r.EndPointId))
                        {
                            n.Add(r.EndPointId, 0.0);
                        }
                    }
                    InfoT.Add(K.Key, n);
                }

            }
        }

        public void TspSolution()
        {
            #region 初始化计算
            //计算初始信息素的值，可直接指定
            double Cnn = 20;
            double t0 = (double)AntCount / Cnn;//信息素初始化
            foreach (KeyValuePair<int, List<RoadDetail>> K in RoadList)
            {
                foreach (RoadDetail r in K.Value)
                {
                    InfoT[K.Key][r.EndPointId] = t0;
                }

            }
            List<int> allNodes = new List<int>();
            //所有路径节点
            foreach (KeyValuePair<int, List<RoadDetail>> K in RoadList)
            {
                allNodes.Add(K.Key);
            }


            //迭代次数
            int NC = 0;
            #endregion
            while (NC < NcMax)
            {
                //生成蚂蚁及初始访问城市，并设置对应禁忌表和路径列表
                List<Ant> antList = new List<Ant>();
                for (int i = 0; i < AntCount; i++)
                    antList.Add(new Ant(i, allNodes, FirstPoint));
                //所有蚂蚁依次寻找下一个节点，直到本轮完成
                //antList.ForEach(n => n.NextCityUntilFinished(InfoT, Distance, Alpha, Beta, Rho,Endpoint.Id));
                //并行计算
                Parallel.ForEach(antList, n => n.NextCityUntilFinished(InfoT, Distance, Alpha, Beta, Rho, Endpoint.Id));
                //统计最优解
                AntList.AddRange(antList);//先添加
                AntList = AntList.OrderBy(n => n.CpathLength).ToList();//排序
                //取出前面指定的几条最短路径
                if (AntList.Count > BetterPlanCount)
                    AntList.RemoveRange(BetterPlanCount, AntList.Count - BetterPlanCount);
                //AntList.First().TranToPoint(AntList.First().Edge);//转换为point类型
                NC++;
                //更新信息素的值：循环所有路径，依次进行添加
                //先挥发
                foreach (KeyValuePair<int, List<RoadDetail>> K in RoadList)
                {
                    foreach (RoadDetail r in K.Value)
                    {
                        InfoT[K.Key][r.EndPointId] *= (1.0 - Rho);
                    }

                }
                //再增强,循环所有蚂蚁
                foreach (var item in antList)
                {
                    var temp = 1.0 / item.CpathLength;
                    foreach (var edge in item.Edge) InfoT[edge.Key][edge.Value] += temp;
                }
            }
            //最好的路径
            foreach (int p in AntList.First().way)
            {
                BestWay.Add(p);
            }
            BestWayLength = AntList.First().CpathLength;
            DisplayToConsolByAnt();
        }


        //输出路径到控制台
        public void DisplayToConsolByAnt()
        {
            System.Console.WriteLine("\n 蚁群算法:\n");
            foreach (int p in BestWay)
            {
                System.Console.WriteLine(p + "> ---》");
            }
            System.Console.WriteLine("路程" + BestWayLength + "\n");
        }
    }

    // 蚂蚁类
    public class Ant
    {
        // 蚂蚁编号
        public int Id { get; set; } = new int();
        // 当前蚂蚁已经走过的路径节点列表，也就是禁忌表
        // 最后1个就是当前所处的位置
        public List<int> LastFour = new List<int>();
        // 当前蚂蚁已走过的向着终点的一条路径
        public List<int> PathNodes { get; set; } = new List<int>();
        // 当前蚂蚁下一步可供选择的节点列表
        public List<int> selectNodes { get; set; } = new List<int>();
        // 该蚂蚁旅行的总长度
        public Double CpathLength { get; set; } = new Double();
        // 蚂蚁走过的地点
        public List<int> way = new List<int>();
        // 当前蚂蚁走过的边，key为起点,value为终点
        public MyDictionary<int, int> Edge;
        private Random rand;

        // 构造函数
        // <param name="id">蚂蚁编号</param>
        // <param name="allNodes">所有节点名称列表</param>
        // <param name="isFixStart">是否固定起点</param>
        public Ant(int id, List<int> allNodes, RoadPoint firstid)
        {
            this.Id = id;
            this.selectNodes = allNodes;
            this.PathNodes = new List<int>();
            this.PathNodes.Add(firstid.Id);
            this.selectNodes.Remove(firstid.Id);
            this.LastFour.Add(firstid.Id);

            this.CpathLength = 0;
            this.way.Add(firstid.Id);
            rand = new Random();
        }

        public void NextCityUntilFinished(MyDictionary<int, MyDictionary<int, double>> info, MyDictionary<int, MyDictionary<int, double>> distance, double a, double b, double p, int Endpointid)
        {
            Edge = new MyDictionary<int, int>();//经过的边：起点-终点

            while (PathNodes.Last() != Endpointid)
            {
                double sumt = 0;//分母的和值
                int current = PathNodes.Last();
                //依次计算当前点到其他点可选择点的 值
                Dictionary<int, double> dic = new Dictionary<int, double>();
                foreach (KeyValuePair<int, double> k in distance[current])
                {
                    bool en = false;
                    foreach (int la in LastFour)//如果前四步走过该点，
                    {
                        if (k.Key == la)
                            en = true;
                    }
                    if (en)
                        continue;
                    var temp = Math.Pow(info[current][k.Key], a) * Math.Pow(1.0 / distance[current][k.Key], b);
                    sumt += temp;
                    dic.Add(k.Key, temp);
                }
                if (dic.Count == 0)
                {
                    LastFour.Add(PathNodes.Last());
                    PathNodes.Remove(PathNodes.Last());
                    continue;
                }
                //计算各个点的概率
                var ratio = dic.ToDictionary(n => n.Key, n => n.Value / sumt);
                //产生1个随机数，并按概率确定下一个地点
                int nextCity = GetNextCityByRandValue(ratio, rand.NextDouble());
                //修改列表
                this.PathNodes.Add(nextCity);
                LastFour.Add(nextCity);
                this.CpathLength += distance[current][nextCity];

                //信息素增强辅助计算
            }
            PathNodes.Remove(PathNodes.Last());
            //最后1条路径的问题，额外增加，直接 回原点
            this.CpathLength += distance[PathNodes.Last()][Endpointid];

            this.PathNodes.Add(Endpointid);//最后才添加  

            for (int i = 0; i < PathNodes.Count - 1; i++)
            {
                if (!Edge.ContainsKey(PathNodes[i]))
                    Edge.Add(PathNodes[i], PathNodes[i + 1]);
                else
                    Edge[PathNodes[i]] = PathNodes[i + 1];

            }
            TranToPoint(Edge);//路径转化成Point
        }

        // 按照dic中按照顺序的节点的概率值，和随机数rnd的值，确定哪一个为下一个城市
        // <param name="dic"></param>
        // <param name="rnd"></param>
        // <returns></returns>
        int GetNextCityByRandValue(Dictionary<int, double> dic, double rnd)
        {
            double sum = 0;
            foreach (KeyValuePair<int, Double> item in dic)
            {
                sum += item.Value;
                if (rnd < sum) return item.Key;
                else continue;
            }
            throw new Exception("无法选择地点");
        }

        //把蚂蚁的路径转化成List<roadid>形式
        public void TranToPoint(MyDictionary<int, int> E)
        {

            foreach (KeyValuePair<int, int> i in E)
            {
                foreach (RoadDetail r in RoadList[i.Key])
                {
                    if (r.EndPointId == i.Value)
                    {
                        way.Add(r.Id);
                        break;
                    }

                }
            }
        }
    }
}
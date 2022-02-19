using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CefSharp;
using CefSharp.Wpf;
using Newtonsoft.Json.Linq;
using static EasyMap.Common;

namespace EasyMap
{

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// 程序主界面
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            this.Closed += MainWindow_Closed;
            LoadData();

            UiOperation = AsyncOperationManager.CreateOperation(null);
            Common.OpenDialog = new OpenDialogDelegate(OpenDialog);
            Common.UpdateUi = new UpdateUiDelegate(UpdateUi);

            // 根据设置初始化选择框的值
            CheckBoxPointDataSourceAMap.IsChecked = (Properties.Settings.Default.pointDataSource & 0x1) > 0;
            CheckBoxPointDataSourceCustom.IsChecked = (Properties.Settings.Default.pointDataSource & 0x2) > 0;
            CheckBoxRoadDataSourceAMap.IsChecked = (Properties.Settings.Default.roadDataSource & 0x1) > 0;
            CheckBoxRoadDataSourceCustom.IsChecked = (Properties.Settings.Default.pointDataSource & 0x2) > 0;

            // 将浏览器添加进布局左列中
            BsControl = new BrowserControl();
            this.MainGrid.Children.Add(Browser);
            Grid.SetColumn(Browser, 0);

            // 初始化各按钮的处理方法
            BtnMoveToCenter.Click += BsControl.Btn_Click;
            BtnAddPoint.Click += BsControl.Btn_Click;
            BtnAddRoad.Click += BsControl.Btn_Click;
            BtnDelPoint.Click += BsControl.Btn_Click;
            BtnDelRoad.Click += BsControl.Btn_Click;
            BtnSearch.Click += BtnSearch_Click;
            ButtonPickMapStart.Click += BsControl.Btn_Click;
            ButtonPickMapEnd.Click += BsControl.Btn_Click;

            // 初始化输入框的处理方法
            TextBoxStartPoint.TextChanged += BsControl.Text_Changed;
            TextBoxEndPoint.TextChanged += BsControl.Text_Changed;

            // 初始化选择框的处理方法
            CheckBoxPointDataSourceAMap.Checked += BsControl.CheckBox_Checked;
            CheckBoxPointDataSourceCustom.Checked += BsControl.CheckBox_Checked;
            CheckBoxRoadDataSourceAMap.Checked += BsControl.CheckBox_Checked;
            CheckBoxRoadDataSourceCustom.Checked += BsControl.CheckBox_Checked;
            CheckBoxPointDataSourceAMap.Unchecked += BsControl.CheckBox_UnChecked;
            CheckBoxPointDataSourceCustom.Unchecked += BsControl.CheckBox_UnChecked;
            CheckBoxRoadDataSourceAMap.Unchecked += BsControl.CheckBox_UnChecked;
            CheckBoxRoadDataSourceCustom.Unchecked += BsControl.CheckBox_UnChecked;

        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string first = this.TextBoxStartPoint.Text;
            string end = this.TextBoxEndPoint.Text;

            if (first == "" || end == "") return;

            RoadPoint firstpoint = new RoadPoint();
            RoadPoint endpoint = new RoadPoint();
            firstpoint.Longitude = Double.Parse(first.Split(';')[0]);
            firstpoint.Latitude = Double.Parse(first.Split(';')[1]);
            endpoint.Longitude = Double.Parse(end.Split(';')[0]);
            endpoint.Latitude = Double.Parse(end.Split(';')[1]);

            RoadPoint firstClosetPoint = new RoadPoint();
            JObject startClosetPointJObject = Map.SearchClosetPoint(firstpoint.Longitude, firstpoint.Latitude);
            // 最近垂点
            firstClosetPoint.Longitude = (double)startClosetPointJObject["p"][0];
            firstClosetPoint.Latitude = (double)startClosetPointJObject["p"][1];
            // 最近点所在的道路ID
            int startClosetRoadId = (int)startClosetPointJObject["roadId"];
            // 起点到最近点的距离
            double distance1 = (double)startClosetPointJObject["distance"];
            // 最近点所在的小路段的索引
            int startClosetSegmentId1 = (int)startClosetPointJObject["segment"][0];
            int startClosetSegmentId2 = (int)startClosetPointJObject["segment"][1];
            // 获取起点到道路两端的距离
            double startToRoadStart = 0;
            for (int i = 0; i < startClosetSegmentId1; i++)
            {
                startToRoadStart += GetDistance(RoadPoints[Roads[startClosetRoadId][i]].Longitude,
                    RoadPoints[Roads[startClosetRoadId][i]].Latitude,
                    RoadPoints[Roads[startClosetRoadId][i + 1]].Longitude,
                    RoadPoints[Roads[startClosetRoadId][i + 1]].Latitude);
            }
            startToRoadStart += GetDistance(RoadPoints[Roads[startClosetRoadId][startClosetSegmentId1]].Longitude,
                RoadPoints[Roads[startClosetRoadId][startClosetSegmentId1]].Latitude,
                firstClosetPoint.Longitude, firstClosetPoint.Latitude);
            double startToRoadEnd = 0;
            for (int i = startClosetSegmentId2; i < Roads[startClosetRoadId].Count - 1; i++)
            {
                startToRoadStart += GetDistance(RoadPoints[Roads[startClosetRoadId][i]].Longitude,
                    RoadPoints[Roads[startClosetRoadId][i]].Latitude,
                    RoadPoints[Roads[startClosetRoadId][i + 1]].Longitude,
                    RoadPoints[Roads[startClosetRoadId][i + 1]].Latitude);
            }
            startToRoadEnd += GetDistance(RoadPoints[Roads[startClosetRoadId][startClosetSegmentId2]].Longitude,
                RoadPoints[Roads[startClosetRoadId][startClosetSegmentId2]].Latitude,
                firstClosetPoint.Longitude, firstClosetPoint.Latitude);

            RoadPoint endClosetPoint = new RoadPoint();
            JObject endClosetPointJObject = Map.SearchClosetPoint(endpoint.Longitude, endpoint.Latitude);
            // 最近垂点
            endClosetPoint.Longitude = (double)endClosetPointJObject["p"][0];
            endClosetPoint.Latitude = (double)endClosetPointJObject["p"][1];
            // 最近点所在的道路ID
            int endClosetRoadId = (int)endClosetPointJObject["roadId"];
            // 终点到最近点的距离
            double distance2 = (double)endClosetPointJObject["distance"];
            // 最近点所在的小路段的索引
            int endClosetSegmentId1 = (int)endClosetPointJObject["segment"][0];
            int endClosetSegmentId2 = (int)endClosetPointJObject["segment"][1];
            // 获取终点到道路两端的距离
            double endToRoadStart = 0;
            for (int i = 0; i < endClosetSegmentId1; i++)
            {
                endToRoadStart += GetDistance(RoadPoints[Roads[endClosetRoadId][i]].Longitude,
                    RoadPoints[Roads[endClosetRoadId][i]].Latitude,
                    RoadPoints[Roads[endClosetRoadId][i + 1]].Longitude,
                    RoadPoints[Roads[endClosetRoadId][i + 1]].Latitude);
            }
            endToRoadStart += GetDistance(RoadPoints[Roads[endClosetRoadId][endClosetSegmentId1]].Longitude,
                RoadPoints[Roads[endClosetRoadId][endClosetSegmentId1]].Latitude,
                endClosetPoint.Longitude, endClosetPoint.Latitude);
            double endToRoadEnd = 0;
            for (int i = startClosetSegmentId2; i < Roads[endClosetRoadId].Count - 1; i++)
            {
                endToRoadEnd += GetDistance(RoadPoints[Roads[endClosetRoadId][i]].Longitude,
                    RoadPoints[Roads[endClosetRoadId][i]].Latitude,
                    RoadPoints[Roads[endClosetRoadId][i + 1]].Longitude,
                    RoadPoints[Roads[endClosetRoadId][i + 1]].Latitude);
            }
            endToRoadEnd += GetDistance(RoadPoints[Roads[endClosetRoadId][endClosetSegmentId2]].Longitude,
                RoadPoints[Roads[endClosetRoadId][endClosetSegmentId2]].Latitude,
                endClosetPoint.Longitude, endClosetPoint.Latitude);

            // 最短路径经过的道路id
            List<int> path = new List<int>();
            // 最短路径上的点集合，每个点由经纬度决定
            List<List<double>> pathPoints = new List<List<double>>();

            // 如果起终点在一条路附近，则直接添加路径数据，不需要调用算法
            if (startClosetRoadId == endClosetRoadId)
            {
                pathPoints.Add(new List<double>{ firstClosetPoint.Longitude, firstClosetPoint.Latitude });
                for (int i = Math.Min(startClosetSegmentId2, endClosetSegmentId1);
                    i < Math.Min(startClosetSegmentId1, endClosetSegmentId2);
                    i++)
                {
                    pathPoints.Add(new List<double>
                    {
                        RoadPoints[Roads[startClosetRoadId][i]].Longitude,
                        RoadPoints[Roads[startClosetRoadId][i]].Latitude
                    });
                }
                pathPoints.Add(new List<double>{ endClosetPoint.Longitude, endClosetPoint.Latitude});
                LabelTime.Content = "0 秒";
                Map.DisplayPath(pathPoints);
            }
            else
            {
                DateTime time = DateTime.Now;
                try
                {
                    if (RadioButtonA.IsChecked == true)
                    {
                        double minLength = double.MaxValue;
                        // 调用A*算法
                        // A*算法, startClosetRoadId, endClosetRoadId
                        // 由于搜索的起终点在道路中央，简单起见，从起终点所在的道路两端分别搜索，总共需要搜索四次
                        SolutionByA solutionA1 = new SolutionByA(RoadPoints[Roads[startClosetRoadId].First()],
                            RoadPoints[Roads[endClosetRoadId].First()]);
                        solutionA1.SolutionBy();
                        if (solutionA1.SumLength <= minLength)
                        {
                            minLength = solutionA1.SumLength;
                            path = solutionA1.Path;
                        }

                        SolutionByA solutionA2 = new SolutionByA(RoadPoints[Roads[startClosetRoadId].First()],
                            RoadPoints[Roads[endClosetRoadId].Last()]);
                        solutionA2.SolutionBy();
                        if (solutionA2.SumLength <= minLength)
                        {
                            minLength = solutionA2.SumLength;
                            path = solutionA2.Path;
                        }

                        SolutionByA solutionA3 = new SolutionByA(RoadPoints[Roads[startClosetRoadId].Last()],
                            RoadPoints[Roads[endClosetRoadId].First()]);
                        solutionA3.SolutionBy();
                        if (solutionA3.SumLength <= minLength)
                        {
                            minLength = solutionA3.SumLength;
                            path = solutionA3.Path;
                        }

                        SolutionByA solutionA4 = new SolutionByA(RoadPoints[Roads[startClosetRoadId].Last()],
                            RoadPoints[Roads[endClosetRoadId].Last()]);
                        solutionA4.SolutionBy();
                        if (solutionA4.SumLength <= minLength)
                        {
                            path = solutionA4.Path;
                        }
                    }
                    else if (RadioButtonAnt.IsChecked == true)
                    {
                        double minLength = double.MaxValue;
                        // 调用蚁群算法
                        //蚁群算法
                        SolutionAnt solutionAnt1 = new SolutionAnt(500, 0.85, 0.05, 0.2, 200,
                            RoadPoints[Roads[startClosetRoadId].First()], RoadPoints[Roads[endClosetRoadId].First()],
                            startClosetRoadId, endClosetRoadId);
                        solutionAnt1.TspSolution();
                        if (solutionAnt1.BestWayLength <= minLength)
                        {
                            minLength = solutionAnt1.BestWayLength;
                            path = solutionAnt1.BestWay;
                        }

                        SolutionAnt solutionAnt2 = new SolutionAnt(500, 0.85, 0.05, 0.2, 200,
                            RoadPoints[Roads[startClosetRoadId].First()], RoadPoints[Roads[endClosetRoadId].Last()],
                            startClosetRoadId, endClosetRoadId);
                        solutionAnt2.TspSolution();
                        if (solutionAnt2.BestWayLength <= minLength)
                        {
                            minLength = solutionAnt2.BestWayLength;
                            path = solutionAnt2.BestWay;
                        }

                        SolutionAnt solutionAnt3 = new SolutionAnt(500, 0.85, 0.05, 0.2, 200,
                            RoadPoints[Roads[startClosetRoadId].Last()], RoadPoints[Roads[endClosetRoadId].First()],
                            startClosetRoadId, endClosetRoadId);
                        solutionAnt3.TspSolution();
                        if (solutionAnt3.BestWayLength <= minLength)
                        {
                            minLength = solutionAnt3.BestWayLength;
                            path = solutionAnt3.BestWay;
                        }

                        SolutionAnt solutionAnt4 = new SolutionAnt(500, 0.85, 0.05, 0.2, 200,
                            RoadPoints[Roads[startClosetRoadId].Last()], RoadPoints[Roads[endClosetRoadId].Last()],
                            startClosetRoadId, endClosetRoadId);
                        solutionAnt4.TspSolution();
                        if (solutionAnt4.BestWayLength <= minLength)
                        {
                            path = solutionAnt4.BestWay;
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
                
                DateTime time2 = DateTime.Now;
                LabelTime.Content = (time2 - time).TotalSeconds + " 秒";

                if (path.Count == 0)
                {
                    LabelTime.Content = "搜索失败，可能两点间没有通路";
                }

                Console.WriteLine("path:" + JArray.FromObject(path));

                // 将起点所在的半段道路添加到路径中
                if (Roads[path[0]].Contains(Roads[startClosetRoadId].First()))
                {
                    pathPoints.Add(new List<double>() {firstClosetPoint.Longitude, firstClosetPoint.Latitude});
                    for (int i = startClosetSegmentId1; i >= 0; i--)
                    {
                        pathPoints.Add(new List<double>
                        {
                            RoadPoints[Roads[startClosetRoadId][i]].Longitude,
                            RoadPoints[Roads[startClosetRoadId][i]].Latitude,
                        });
                    }
                }
                else
                {
                    pathPoints.Add(new List<double>() { firstClosetPoint.Longitude, firstClosetPoint.Latitude });
                    for (int i = startClosetSegmentId2; i <= Roads[startClosetRoadId].Count - 1; i++)
                    {
                        pathPoints.Add(new List<double>
                        {
                            RoadPoints[Roads[startClosetRoadId][i]].Longitude,
                            RoadPoints[Roads[startClosetRoadId][i]].Latitude,
                        });
                    }
                }
                // 根据最佳路径，将构成每条路的端点ID形成一个经纬度列表，返回给js绘制路线
                // 如果只经过一条大段路
                if (path.Count == 1)
                {
                    if (Roads[path[0]].First() == startClosetRoadId)
                    {
                        foreach (int i in Roads[path[0]])
                        {
                            pathPoints.Add(new List<double>{RoadPoints[i].Longitude, RoadPoints[i].Latitude});
                        }
                    }
                    else
                    {
                        Roads[path[0]].Reverse();
                        foreach (int i in Roads[path[0]])
                        {
                            pathPoints.Add(new List<double> { RoadPoints[i].Longitude, RoadPoints[i].Latitude });
                        }
                        Roads[path[0]].Reverse();
                    }
                }
                else
                {
                    for (int i = 0; i < path.Count; i++)
                    {
                        // 如果处理到了最后一条道路，则需要根据之前的一条道路确定方向
                        if (i == path.Count - 1)
                        {
                            // 如果起点包含在上一段路中
                            if (Roads[path[i - 1]].Contains(Roads[path[i]].First()))
                            {
                                foreach (int j in Roads[path[i]])
                                {
                                    pathPoints.Add(new List<double> { RoadPoints[j].Longitude, RoadPoints[j].Latitude });
                                }

                            }
                            // 如果终点包含在上一段路中
                            else if (Roads[path[i - 1]].Contains(Roads[path[i]].Last()))
                            {
                                Roads[path[i]].Reverse();
                                foreach (int j in Roads[path[i]])
                                {
                                    pathPoints.Add(new List<double> { RoadPoints[j].Longitude, RoadPoints[j].Latitude });
                                }
                                Roads[path[i]].Reverse();
                            }
                            break;
                        }

                        // 如果上一段路的起点包含在下一段路中
                        if (Roads[path[i + 1]].Contains(Roads[path[i]].First()))
                        {
                            Roads[path[i]].Reverse();
                            foreach (int j in Roads[path[i]])
                            {
                                pathPoints.Add(new List<double> { RoadPoints[j].Longitude, RoadPoints[j].Latitude });
                            }

                            Roads[path[i]].Reverse();
                        }
                        // 如果上一段路的终点包含在下一段路中
                        else if (Roads[path[i + 1]].Contains(Roads[path[i]].Last()))
                        {
                            foreach (int j in Roads[path[i]])
                            {
                                pathPoints.Add(new List<double> { RoadPoints[j].Longitude, RoadPoints[j].Latitude });
                            }
                        }
                    }
                }
                // 将终点所在的半段道路添加到路径中
                if (Roads[path.Last()].Contains(Roads[endClosetRoadId].First()))
                {
                    for (int i = 0; i <= endClosetSegmentId1; i++)
                    {
                        pathPoints.Add(new List<double>
                        {
                            RoadPoints[Roads[endClosetRoadId][i]].Longitude,
                            RoadPoints[Roads[endClosetRoadId][i]].Latitude,
                        });
                    }
                    pathPoints.Add(new List<double>() { endClosetPoint.Longitude, endClosetPoint.Latitude });
                }
                else
                {
                    for (int i = Roads[endClosetRoadId].Count - 1; i >= endClosetSegmentId2; i--)
                    {
                        pathPoints.Add(new List<double>
                        {
                            RoadPoints[Roads[endClosetRoadId][i]].Longitude,
                            RoadPoints[Roads[endClosetRoadId][i]].Latitude,
                        });
                    }
                    pathPoints.Add(new List<double>() { endClosetPoint.Longitude, endClosetPoint.Latitude });

                }
            }
            Map.DisplayPath(pathPoints);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            SaveData();
        }

        public void OpenDialog(JObject json)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(Common.OpenDialog, json);
                return;
            }
            PlaceDialog placeDialog = new PlaceDialog
            {
                TextBoxId = {Text = (LastPlaceId + 1).ToString()},
                TextBoxLng = {Text = json["lng"].ToString()},
                TextBoxLat = {Text = json["lat"].ToString()},
                Owner = this
            };
            placeDialog.ShowDialog();
        }

        public void UpdateUi(string objName, string msg)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(Common.UpdateUi, objName, msg);
                return;
            }

            if (objName == "TextBoxStartPoint" || objName == "TextBoxEndPoint")
            {
                TextBox textBox = (TextBox) this.FindName(objName);
                if (textBox != null) textBox.Text = msg;
            }
            else if (objName == "StartImg" || objName == "EndImg")
            {
                Image image = (Image) this.FindName(objName);
                if (image != null) image.Source = new BitmapImage(new Uri(msg, UriKind.Relative));
            }
            else
            {
                Button button = (Button)this.FindName(objName);
                if (button != null) button.Content = msg;
            }

        }
    }
}

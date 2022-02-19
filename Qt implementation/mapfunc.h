#ifndef MAPFUNC_H
#define MAPFUNC_H
#include <iostream>
#include <queue>
#include <algorithm>
#include <map>
#include <QObject>
#include <QGuiApplication>
#include <QDebug>
#include <QJsonObject>
#include <QJsonDocument>
#include <QJsonArray>
#include <math.h>
#include <QSqlDatabase>
#include <QSqlError>
#include <QSqlQuery>
#include <vector>
#include <math.h>
#include "recommend.h"

using namespace std;

//无向图的邻接表数据结构

const int MAX = 5000;

struct ENode      //邻接表的中间节点
{
    int adjvex;   //对应索引
    double length;  //该道路长度
    ENode* next;
};

typedef struct VNode //邻接表顶点
{
    int nodeId = NULL;
    bool isEntity = false;        //该点是景点还是路径拐点，如为景点为true
    double lng = NULL;
    double lat = NULL;           //经纬度
    QString nodeName = nullptr;     //景点名称
    QString nodeDesc = nullptr;     //景点介绍
    bool active = NULL;  //指示该节点是否被使用
    ENode* firstarc = nullptr; //指向第一个中间节点
}AdjList[MAX];

class MapFunc : public QObject
{
    Q_OBJECT
private:
    AdjList adjList;          //邻接表数组
    int vexNum = 0;              //节点数量
    int arcNum;              //连边数量
    int parseJson(QString strJson, QJsonDocument &document);
    double distance(double an, double aa, double bn, double ba);    //计算两点间的距离a(n, a), b(n, a)
    QSqlDatabase database;
    void databaseInit();
    // 迪杰斯特拉算法求某个点到图中其他点的最短距离
    void Dijkstra(int n, unsigned int start, unsigned int end, vector<double> &distance, vector<int> &preNode);
public:
    MapFunc();

public slots:
    void addNode(QVariant node);     //界面调用，向图中添加顶点
    void addSide(QVariant side);    //界面调用，向图中添加边
    void getJson();                  //界面调用，求取图中信息，用于绘图
    void letSearchShortest(QVariant Nodes);                  //界面调用，开始进行遍历
    void letRecommend(QVariant start);

signals:
    void sendJson(QVariant json);   //向界面发送含有图信息的json，方便绘图
    void sendPathJson(QVariant path, QVariant distance);   //向界面发送搜索后的路径
};

#endif // MAPFUNC_H

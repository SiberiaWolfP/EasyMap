#include "mapfunc.h"
#include "ga.h"

int MapFunc::parseJson(QString strJson, QJsonDocument &document)
{
    QByteArray byteArray = strJson.toUtf8();
    QJsonParseError jsonError;
    document = QJsonDocument::fromJson(byteArray, &jsonError);
    if (!document.isNull() && (jsonError.error == QJsonParseError::NoError)) {
        if (document.isObject()) return 1;
        else return 2;
    }
    // 若JSON格式有误
    else return 0;
}

double MapFunc::distance(double an, double aa, double bn, double ba)
{
    return sqrt(pow(bn - an, 2) + pow(ba - aa, 2));
}

void MapFunc::databaseInit()
{
    if (QSqlDatabase::contains("qt_sql_default_connection"))
    {
        database = QSqlDatabase::database("qt_sql_default_connection");
    }
    else
    {
        database = QSqlDatabase::addDatabase("QSQLITE");
        database.setDatabaseName("MapData.db");
    }
}

// 角度转弧度
double rad(double d)
{
    const double PI = 3.1415926535898;
    return d * PI / 180.0;
}

// 传入两个经纬度，计算之间的大致直线距离
double CalcDistance(float fLati1, float fLong1, float fLati2, float fLong2)
{
    const float EARTH_RADIUS = 6378.137;

    double radLat1 = rad(fLati1);
    double radLat2 = rad(fLati2);
    double a = radLat1 - radLat2;
    double b = rad(fLong1) - rad(fLong2);
    double s = 2 * asin(sqrt(pow(sin(a/2),2) + cos(radLat1)*cos(radLat2)*pow(sin(b/2),2)));
    s = s * EARTH_RADIUS;
    s = s * 10000000 / 10000;
    return s;
}

// 迪杰斯特拉算法原设计用于求某点到所有点的最短距离，若只求某点到另一个点的最短距离，只需算法求到另一个点时停止即可
void MapFunc::Dijkstra(int n, unsigned int start, unsigned int end, vector<double> &distance, vector<int> &preNode)
{
    /**
     *  G为图；数组d为源点到达各点的最短路径长度，s为起点
        Dijkstra(G, d[], s)
        {
             初始化;
             for(循环n次)
             {
                  u = 使d[u]最小的还未被访问的顶点的标号;
                  记u已被访问;
                  for(从u出发能到达的所有顶点v)
                  {
                       if(v未被访问 && 以u为中介点使s到顶点v的最短距离d[v]更优)
                       {
                            优化d[v];
                       }
                  }
             }
        }
     */
    vector<bool> visted(static_cast<unsigned int>(n));
    fill(distance.begin(), distance.end(), DBL_MAX);    // 将距离数组全填为无穷大
    for (int i = 0; i < n; i++) {
        preNode[static_cast<unsigned int>(i)] = i;
    }
    distance[start] = 0;                                // 起点到自身的距离为0
    for (int i = 0; i < n; i++) {
        int u = -1;
        double MIN = DBL_MAX;                           // 初始时最小值设置为最大，以使第一个点即能进入比较
        // 在图中寻找还未访问的点中，距离起始点最近的点
        for (unsigned int j = 0; j < static_cast<unsigned int>(n); j++) {
            if (visted[j] == false && distance[j] < MIN) {
                u = static_cast<int>(j);
                MIN = distance[j];
            }
        }
        // 如果u == -1证明其他点与起点不连通
        if (u == -1) {
            return;
        }
        // 将该轮访问的点纳入已访问
        visted[static_cast<unsigned int>(u)] = true;
        // 从这个点出发，看经过这个点访问该点能直接访问的点，是否存在比不经过该点访问的最短路径
        ENode *p = adjList[u].firstarc;
        while (p != nullptr) {
            unsigned int v = static_cast<unsigned int>(p->adjvex);
            if (visted[v] == false && distance[v] > distance[static_cast<unsigned int>(u)] + p->length) {
                distance[v] = distance[static_cast<unsigned int>(u)] + CalcDistance(adjList[u].lat, adjList[u].lng, adjList[v].lat, adjList[v].lng);
                preNode[v] = u;
                // 如果迪杰斯特拉算法求到了设置的结束点，就可以结束了，以提高程序性能
                if (v == end) {
                    return;
                }
            }
            p = p->next;
        }
    }

}

MapFunc::MapFunc()
{
    // 重新启动程序后从数据库恢复数据
    databaseInit();
    if (!database.open()) {
        qDebug() << database.lastError();
    } else {
        QSqlQuery sql_query;
        QSqlQuery sql_query2;
        QString select_all_sql = "select * from mapdata";
        sql_query.prepare(select_all_sql);
        if (!sql_query.exec()) {
            qDebug() << sql_query.lastError();
        } else {
            while (sql_query.next()) {
                int id = sql_query.value(0).toInt();
                adjList[id].nodeId = id;
                adjList[id].isEntity = sql_query.value(1).toBool();
                adjList[id].active = true;
                adjList[id].lng = sql_query.value(2).toDouble();
                adjList[id].lat = sql_query.value(3).toDouble();
                adjList[id].nodeName = sql_query.value(4).toString();
                adjList[id].nodeDesc = sql_query.value(5).toString();
                vexNum++;
                QString select_sql = "select adjvex, next from enodelist where nodeId = " + QString::number(id);
                sql_query2.prepare(select_sql);
                if (!sql_query2.exec()) {
                    qDebug() << sql_query2.lastError();
                } else {
                    while (sql_query2.next()) {
                        ENode* temp = new ENode();
                        temp->adjvex = sql_query2.value(0).toInt();
                        temp->next = adjList[id].firstarc;
                        adjList[id].firstarc = temp;
                    }
                }
            }
            qDebug() << QString::fromLocal8Bit("程序启动，已由数据库恢复历史数据");
        }
    }
    database.close();
}

void MapFunc::addNode(QVariant node)
{
    //解析界面传过来的json
    /**
     * JSON格式：
     * {
     *      "lng": xxx,
     *      "lat": xxx,
     *      "nodeName": xxx,
     *      "nodeDesc": xxx
     * }
     */
    QString strJson = node.toString();
    qDebug() << QString::fromLocal8Bit("添加景点JSON:") << strJson;
    QJsonDocument document;
    QJsonObject obj;
    if (parseJson(strJson, document) == 1) {
        obj = document.object();
        for (int i = 0; i < MAX; i++) {
            if (adjList[i].active) continue;
            adjList[i].active = true;
            adjList[i].nodeId = i;
            adjList[i].firstarc = nullptr;
            adjList[i].isEntity = true;
            adjList[i].lng = obj.value("lng").toDouble();
            adjList[i].lat = obj.value("lat").toDouble();
            adjList[i].nodeName = obj.value("nodeName").toString();
            adjList[i].nodeDesc = obj.value("nodeDesc").toString();
            vexNum++;

            // 插入数据库
            if (!database.open()) {
                qDebug() << database.lastError();
            } else {
                QSqlQuery sql_query;
                QString insert_sql = "insert into mapdata values (?,?,?,?,?,?)";
                sql_query.prepare(insert_sql);
                sql_query.addBindValue(adjList[i].nodeId);
                sql_query.addBindValue(adjList[i].isEntity);
                sql_query.addBindValue(adjList[i].lng);
                sql_query.addBindValue(adjList[i].lat);
                sql_query.addBindValue(adjList[i].nodeName);
                sql_query.addBindValue(adjList[i].nodeDesc);
                if (!sql_query.exec()) {
                    qDebug() << sql_query.lastError();
                } else {
                    qDebug() << QString::fromLocal8Bit("插入景点数据库操作成功");
                }
            }
            break;
        }
    }
    database.close();
}

void MapFunc::addSide(QVariant side)
{
    //向图中添加边，如果一个道路转点和一个景点相距过近，将转点合并进景点
    /**
     * JSON格式：
     * [
     *      {
     *          "P": xxx,
     *          "Q": xxx,
     *          "lng": xxx,
     *          "lat": xxx
     *      },
     *      ...
     * ]
     */
    QString strJson = side.toString();
    qDebug() << QString::fromLocal8Bit("添加道路JSON:") << strJson;
    QJsonDocument document;
    QJsonArray array;
    if (parseJson(strJson, document) == 2) {
        array = document.array();
        int nSize = array.size();
        // 第一遍，把道路转点作为顶点节点存入，与已存在点相距过近的合并
        for (int i = 0; i < nSize - 1; i++) {
            // 在已有点中寻找是否有点离待联系点过近，如过近，直接使用已有点
            int pointOne = -1, pointTwo = -1;
            QJsonObject objOne = array.at(i).toObject();
            for (int j = 0; j < MAX; j++) {
                if (!adjList[j].active) continue;
                if (distance(adjList[j].lng, adjList[j].lat, objOne.value("Q").toDouble(), objOne.value("P").toDouble()) < 0.0001) {
                    pointOne = adjList[j].nodeId;
                    break;
                }
            }
            // 如已有点中没有过近点, 将该点作为新点插入
            if (pointOne == -1) {
                for (int i = 0; i < MAX; i++) {
                    if (adjList[i].active) continue;
                    adjList[i].active = true;
                    adjList[i].nodeId = i;
                    adjList[i].firstarc = nullptr;
                    adjList[i].isEntity = false;
                    adjList[i].lng = objOne.value("Q").toDouble();
                    adjList[i].lat = objOne.value("P").toDouble();
                    vexNum++;

                    // 在插入数据结构的同时插入数据库保存
                    if (!database.open()) {
                        qDebug() << database.lastError();
                    } else {
                        QSqlQuery sql_query;
                        QString insert_sql = "insert into mapdata values (?,?,?,?,?,?)";
                        sql_query.prepare(insert_sql);
                        sql_query.addBindValue(adjList[i].nodeId);
                        sql_query.addBindValue(adjList[i].isEntity);
                        sql_query.addBindValue(adjList[i].lng);
                        sql_query.addBindValue(adjList[i].lat);
                        sql_query.addBindValue(adjList[i].nodeName);
                        sql_query.addBindValue(adjList[i].nodeDesc);
                        if (!sql_query.exec()) {
                            qDebug() << sql_query.lastError();
                        } else {
                            qDebug() << QString::fromLocal8Bit("插入景点数据库操作成功");
                        }
                    }

                    pointOne = i;
                    break;
                }
            }

            QJsonObject objTwo = array.at(i + 1).toObject();
            for (int j = 0; j < MAX; j++) {
                if (!adjList[j].active) continue;
                if (distance(adjList[j].lng, adjList[j].lat, objTwo.value("Q").toDouble(), objTwo.value("P").toDouble()) < 0.0001) {
                    pointTwo = adjList[j].nodeId;
                    break;
                }
            }
            // 如已有点中没有过近点
            if (pointTwo == -1) {
                for (int i = 0; i < MAX; i++) {
                    if (adjList[i].active) continue;
                    adjList[i].active = true;
                    adjList[i].nodeId = i;
                    adjList[i].firstarc = nullptr;
                    adjList[i].isEntity = false;
                    adjList[i].lng = objTwo.value("Q").toDouble();
                    adjList[i].lat = objTwo.value("P").toDouble();
                    vexNum++;

                    if (!database.open()) {
                        qDebug() << database.lastError();
                    } else {
                        QSqlQuery sql_query;
                        QString insert_sql = "insert into mapdata values (?,?,?,?,?,?)";
                        sql_query.prepare(insert_sql);
                        sql_query.addBindValue(adjList[i].nodeId);
                        sql_query.addBindValue(adjList[i].isEntity);
                        sql_query.addBindValue(adjList[i].lng);
                        sql_query.addBindValue(adjList[i].lat);
                        sql_query.addBindValue(adjList[i].nodeName);
                        sql_query.addBindValue(adjList[i].nodeDesc);
                        if (!sql_query.exec()) {
                            qDebug() << sql_query.lastError();
                        } else {
                            qDebug() << QString::fromLocal8Bit("插入景点数据库操作成功");
                        }
                    }

                    pointTwo = i;
                    break;
                }
            }
            // 若两点判断为了一点（有合并点的情况），不再给自身添加边
            if (pointOne == pointTwo) continue;

            // 正式添加边
            ENode* temp = new ENode();
            temp->adjvex = pointTwo;
            temp->length = distance(adjList[pointOne].lng, adjList[pointOne].lat, adjList[pointTwo].lng, adjList[pointTwo].lat);
            temp->next = adjList[pointOne].firstarc;
            adjList[pointOne].firstarc = temp;

            if (!database.open()) {
                qDebug() << database.lastError();
            } else {
                QSqlQuery sql_query;
                QString insert_sql = "insert into enodelist values (?,?,?,?)";
                sql_query.prepare(insert_sql);
                sql_query.addBindValue(pointOne);
                sql_query.addBindValue(pointTwo);
                sql_query.addBindValue(temp->length);
                sql_query.addBindValue(adjList[pointOne].firstarc->adjvex);
                if (!sql_query.exec()) {
                    qDebug() << sql_query.lastError();
                } else {
                    qDebug() << QString::fromLocal8Bit("插入边数据库操作成功");
                }
            }

            temp = new ENode();
            temp->adjvex = pointOne;
            temp->length = distance(adjList[pointOne].lng, adjList[pointOne].lat, adjList[pointTwo].lng, adjList[pointTwo].lat);
            temp->next = adjList[pointTwo].firstarc;
            adjList[pointTwo].firstarc = temp;

            if (!database.open()) {
                qDebug() << database.lastError();
            } else {
                QSqlQuery sql_query;
                QString insert_sql = "insert into enodelist values (?,?,?,?)";
                sql_query.prepare(insert_sql);
                sql_query.addBindValue(pointTwo);
                sql_query.addBindValue(pointOne);
                sql_query.addBindValue(temp->length);
                sql_query.addBindValue(adjList[pointTwo].firstarc->adjvex);
                if (!sql_query.exec()) {
                    qDebug() << sql_query.lastError();
                } else {
                    qDebug() << QString::fromLocal8Bit("插入边数据库操作成功");
                }
            }
        }
    }
    database.close();
}

//将图的信息发送给界面，包括图每个节点的索引、值和边的信息，JSON结构如下：
/*
 * {
 *  "node": {
 *          nodeId: {
 *              "lng": xxx,
 *              "lat": xxx,
 *              "nodeName": xxx,
 *              "nodeDesc": xxx
 *          },
 *          ...
 *       },
 *  "side":
 *       [
 *          {nodeId: nodeId},
 *          ...
 *       ]
 * }
 * */
void MapFunc::getJson()
{
    QJsonObject json;
    QJsonObject node;
    QJsonArray side;

    // 向JSON中添加点信息
    for (int i = 0; i < MAX; i++) {
        if (!adjList[i].active) continue;
        QJsonObject nodeItem;
        nodeItem.insert("lng", adjList[i].lng);
        nodeItem.insert("lat", adjList[i].lat);
        nodeItem.insert("nodeName", adjList[i].nodeName);
        nodeItem.insert("nodeDesc", adjList[i].nodeDesc);
        nodeItem.insert("isEntity", adjList[i].isEntity);
        node.insert(QString::number(adjList[i].nodeId), nodeItem);
    }
    json.insert("node", node);

    // 向JSON中添加边信息
    for (int i = 0; i < MAX; ++i) {
        if (!adjList[i].active) continue;
        ENode *p = adjList[i].firstarc;
        continu:
        while (p) {
            int nSize = side.size();
            for (int j = 0; j < nSize; j++) {
                QJsonObject obj = side.at(j).toObject();
                if (obj.contains(QString::number(i))) {
                    if (obj.value(QString::number(i)).toString() == QString::number(p->adjvex)) {
                        p = p->next;
                        // 如果想添加的边已经有了，就不用再添加，移动到下一个边结点进行下一条边的添加
                        goto continu;
                    }
                }
                if (obj.contains(QString::number(p->adjvex))) {
                    if (obj.value(QString::number(p->adjvex)).toString() == QString::number(i)) {
                        p = p->next;
                        goto continu;
                    }
                }
            }
            QJsonObject obj;
            // 当P不为空时，第一个边结点不会进入循环，直接添加一条边,从第二个开始，要在添加时查看已有边，防止重复添加
            obj.insert(QString::number(i), QString::number(p->adjvex));
            side.append(obj);
            p = p->next;
        }
    }
    json.insert("side", side);

    qDebug() << QString::fromLocal8Bit("JSON已由后台送出：") << json;
    QJsonDocument document;
    document.setObject(json);
    QByteArray byteArray = document.toJson(QJsonDocument::Compact);
    QString strJson(byteArray);
    QVariant var(strJson);

    emit sendJson(var);
}

void MapFunc::letSearchShortest(QVariant Nodes)
{
    // 解析数据
    QString strJson = Nodes.toString();
    qDebug() << QString::fromLocal8Bit("后台接受前端搜索路径JSON:") << strJson;
    QJsonDocument document;
    QJsonArray array;
    if (parseJson(strJson, document) == 2) {
        array = document.array();
        // pointOne为起点ID， pointTwo为终点ID
        int pointOne = array.at(0).toString().toInt();
        int pointTwo = array.at(1).toString().toInt();
        // 使用两个变长数组分别存储距离信息和路径信息
        vector<double> distance(static_cast<unsigned int>(vexNum));
        vector<int> preNode(static_cast<unsigned int>(vexNum));
        // 在迪杰斯特拉算法中寻找最优解
        Dijkstra(vexNum, static_cast<unsigned int>(pointOne), static_cast<unsigned int>(pointTwo), distance, preNode);
        vector<int> path;
        int next = pointTwo;
        // 通过标号遍历的方法来得到路径
        path.push_back(pointTwo);
        while (true) {
            path.push_back(preNode[next]);
            if (preNode[next] != pointOne) {
                next = preNode[next];
            }
            else break;
        }
        // 镜像路径数组，使得顺序正常
        reverse(path.begin(), path.end());
        QJsonArray array;
        for (int i = 0; i < path.size(); i++) {
            array.append(path[i]);
        }
        // 转换成可以发送到前端的数据
        QJsonDocument document;
        document.setArray(array);
        QByteArray byteArray = document.toJson(QJsonDocument::Compact);
        QString strJson(byteArray);
        QVariant var(strJson);
        QVariant distancevar(distance[pointTwo]);
        qDebug() << QString::fromLocal8Bit("搜索JSON已由后台送出：") + strJson;
        emit sendPathJson(var, distancevar);
    }
}

void MapFunc::letRecommend(QVariant start)
{
    QString strJson = start.toString();
    qDebug() << QString::fromLocal8Bit("后台接受前端搜索路径JSON:") << strJson;
    QJsonDocument document;
    QJsonArray array;
    int entityNode = 0;
    // 简化矩阵和未简化矩阵间的节点标号对应
    int * correspondence = new int[vexNum];
    for (int i = 0; i < vexNum; i++) {
        if (adjList[i].isEntity) {
            correspondence[i] = entityNode;
            entityNode++;
        }
    }
    int * paths = new int[entityNode + 1];
    // 初始化邻接矩阵
    double ** sides = new double* [entityNode];
    for(int i=0; i<vexNum; i++)
    {
        sides[i] = new double[entityNode];
    }
    for (int i = 0; i < entityNode; i++) {
        for (int j = 0; j < entityNode; j++) {
            sides[i][j] = DBL_MAX;
        }
    }

    if (parseJson(strJson, document) == 2) {
        array = document.array();
        int starter = array.at(0).toString().toInt();
        try {
            // 推荐路线问题结合本程序实际可动态规划
            // 景点之间转折点众多但实质上可宏观的看为直接相连
            // 所以可以将转折点全部隐藏，简化图为数十个点
            // 使用迪杰斯特拉算法求出两景点间最短距离，用于简化图
            for (int i = 0; i < vexNum; i++) {
                int pointOne = i;
                if (!adjList[i].isEntity) continue;
                for (int j = 0; j < vexNum; j++) {
                    if (i == j) continue;
                    if (!adjList[j].isEntity) continue;
                    int pointTwo = j;
                    // 使用两个变长数组分别存储距离信息和路径信息
                    vector<double> distance(static_cast<unsigned int>(vexNum));
                    vector<int> preNode(static_cast<unsigned int>(vexNum));
                    // 在迪杰斯特拉算法中寻找最优解
                    Dijkstra(vexNum, static_cast<unsigned int>(pointOne), static_cast<unsigned int>(pointTwo), distance, preNode);
                    sides[correspondence[i]][correspondence[j]] = distance[j];
                }
            }
            // 启动算法，寻找最短回路
            // 由于算法默认从0节点开始，所以将启动点与原0点交换
            if (starter != 0) {
                double temparray;
                for (int i = 0; i < entityNode; i++) {
                    temparray = sides[starter][i];
                    sides[starter][i] = sides[0][i];
                    sides[0][i] = temparray;
                }
                for (int i = 0; i < entityNode; i++) {
                    temparray = sides[i][0];
                    sides[i][0] = sides[i][starter];
                    sides[i][starter] = temparray;
                }
            }
            cout << "开始寻找最佳路径" << endl;
            recommend rec(sides, paths, entityNode);
            cout << paths[0];
            // 将标号调整正常
            cout << "最终路线：" << endl;
            for (int j = 0; j < entityNode + 1; j++) {
                cout << "sss";
                if (paths[j] == 0) {
                    paths[j] = starter;
                }
                else if (paths[j] == starter) {
                    paths[j] = 0;
                }
                cout << paths[j] << "->";
            }
            delete [] paths;
            delete [] correspondence;
            delete [] sides;
        } catch (exception e) {
            qDebug() << e.what();
            delete [] paths;
            delete [] correspondence;
            delete [] sides;
        }
    }
}

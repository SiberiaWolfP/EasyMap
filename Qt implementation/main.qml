import QtQuick 2.9
import QtQuick.Window 2.2
import QtWebEngine 1.8
import QtQuick.Controls 2.5
import QtWebChannel 1.0
import QtQuick.Dialogs 1.2
import MapFunc 1.0

Window {
    id: root
    visible: true
    width: 1080
    height: 800
    title: qsTr("武汉大学校园导航")

    // 全局变量，以JSON保存图结构
    property var json_dict
    property int amount_node: 0

    WebEngineView {
        id: webview
        anchors.fill: parent
        z: 1
        url:"qrc:///map.html"
        webChannel : WebChannel {
            id: channel
            registeredObjects: [moveCenterBtn, addSideBtn, addNodeBtn, logMessageRec, dispNode, dispAll, searchShortest, recommend]
        }
    }

    //在qml中将C++类实例化，才能调用方法和响应信号
    MapFunc {
        id: func
        //每次涉及改变图的操作都要在最后调用sendjson更新前端图的缓存，以同步前后端
        onSendJson: {
            console.log("前端接收到JSON：" + json)
            json_dict = JSON.parse(json)
            amount_node = 0
            //在储存图结构的同时储存图的点数量用于遍历
            for (var o in json_dict["node"]) {
                amount_node++
            }
        }
        onSendPathJson: {
            searchShortest.pathJsonGetted(path, distance)
        }
    }

    MessageDialog {
        id: webviewDialog
        icon: StandardIcon.Question
        title: "消息提示框"
        text: "简短消息提示"
        detailedText: "详细消息提示"
        standardButtons: StandardButton.No | StandardButton.Yes
        onYes: console.log("copied")
        onNo: console.log("didn't copy")
        Component.onCompleted: visible = false
    }

    Button {
        id: addSideBtn
        WebChannel.id: "addSideBtn"
        width: 100
        height: 60
        z: 2
        anchors.right: webview.right
        anchors.top: webview.top
        anchors.rightMargin: 10
        anchors.topMargin: 10
        text: qsTr("添加道路")
        font.pixelSize: 18
        font.family: "Microsoft Yahei"
        hoverEnabled: true

        signal sendsignal()
        onClicked: {
            sendsignal()
            logMessageRec.addMessage("点击鼠标左键添加一条道路，点击右键完成添加")
        }
        property var new_side
        function changeAddSide(newSide) {
            console.log(newSide)
            //
            new_side = JSON.parse(newSide)
            var component = Qt.createComponent("messagedialog.qml")
            if (component.status === Component.Ready) {
                component.createObject(root).open()
            }
        }
        function acceptAddSide() {
            console.log("接受了添加道路，道路信息：")
            console.log(JSON.stringify(new_side))
            var json = JSON.stringify(new_side)
            func.addSide(json)
        }
    }

    Button {
        id: addNodeBtn
        WebChannel.id: "addNodeBtn"
        width: 100
        height: 60
        z: 2
        anchors.right: addSideBtn.right
        anchors.top: addSideBtn.bottom
        anchors.topMargin: 10
        text: qsTr("添加景点")
        font.pixelSize: 18
        font.family: "Microsoft Yahei"
        hoverEnabled: true
        signal sendsignal()
        onClicked: {
            sendsignal()
            logMessageRec.addMessage("点击鼠标左键添加一个景点，选定位置后将填写景点细节")
        }
        property var new_node
        function changeAddNode(newNode) {
            console.log(newNode)
            // {"P":xxx,"Q":xxx,"lng":xxx,"lat":xxx}
            // Q.lng 经度； lat.P 纬度
            new_node = JSON.parse(newNode)
            var component = Qt.createComponent("addnodedialog.qml")
            if (component.status === Component.Ready) {
                component.createObject(root).open()
            }
        }
        function acceptAddNode(nodeName, nodeDesc) {
            console.log("接受了添加景点，景点信息：")
            console.log("景点坐标：经度："+ new_node['Q'] + "；纬度：" + new_node['P'])
            console.log("景点名称：" + nodeName)
            console.log("景点描述：" + nodeDesc)
            var newNode = {}
            newNode["lng"] = new_node["Q"]
            newNode["lat"] = new_node["P"]
            newNode["nodeName"] = nodeName
            newNode["nodeDesc"] = nodeDesc
            var json = JSON.stringify(newNode).toString()
            func.addNode(json)
        }
    }

    Button {
        id: dispNode
        WebChannel.id: "dispNode"
        width: 100
        height: 60
        z: 2
        anchors.right: addNodeBtn.right
        anchors.top: addNodeBtn.bottom
        anchors.topMargin: 10
        text: qsTr("显示所有景点")
        font.pixelSize: 14
        font.family: "Microsoft Yahei"
        hoverEnabled: true
        signal sendsignal(var json)
        onClicked: {
            logMessageRec.clearMap()
            func.getJson()
            sendsignal(json_dict["node"])
            logMessageRec.addMessage("点击景点查看详情")
        }
    }

    Button {
        id: dispAll
        WebChannel.id: "dispAll"
        width: 100
        height: 60
        z: 2
        anchors.right: dispNode.right
        anchors.top: dispNode.bottom
        anchors.topMargin: 10
        text: qsTr("显示所有道路")
        font.pixelSize: 14
        font.family: "Microsoft Yahei"
        hoverEnabled: true
        signal sendsignal(var json)
        onClicked: {
            func.getJson()
            sendsignal(json_dict)
            logMessageRec.addMessage("点击景点查看详情")
        }
    }

    Button {
        // 搜索两点间最短路径的按钮
        id: searchShortest
        WebChannel.id: "searchShortest"
        width: 100
        height: 60
        z: 2
        anchors.right: dispAll.right
        anchors.top: dispAll.bottom
        anchors.topMargin: 10
        text: qsTr("查询两点路径")
        font.pixelSize: 14
        font.family: "Microsoft Yahei"
        hoverEnabled: true
        signal sendsignal(var json)
        signal sendPath(var path, var json)
        onClicked: {
            // 按钮被点击后向浏览器发送请求
            // 先清理地图
            logMessageRec.clearMap()
            // 再从后端获取图信息
            func.getJson()
            // 将景点部分传到浏览器，以在地图上绘制所有景点
            sendsignal(json_dict["node"])
            logMessageRec.addMessage("先后点击两个景点，查询从前一点到后一点的最短路径")
        }
        function nodeGetted(clickedNode) {
            logMessageRec.addLog("最短路径查询的两点是：" + clickedNode)
            func.letSearchShortest(clickedNode)
        }
        function pathJsonGetted(path, distance) {
            logMessageRec.clearMap()
            logMessageRec.addMessage("这两个景点之间的路线距离为：" + distance.toString() + "米")
            func.getJson()
            logMessageRec.addLog("前端接收到搜索后的路径：" + path)
            sendPath(JSON.parse(path), json_dict)
        }
    }

    Button {
        // 搜索两点间最短路径的按钮
        id: recommend
        WebChannel.id: "recommend"
        width: 100
        height: 60
        z: 2
        anchors.right: searchShortest.right
        anchors.top: searchShortest.bottom
        anchors.topMargin: 10
        text: qsTr("游玩全部景点")
        font.pixelSize: 14
        font.family: "Microsoft Yahei"
        hoverEnabled: true
        signal sendsignal(var json)
        signal sendPath(var path, var json)
        onClicked: {
            // 按钮被点击后向浏览器发送请求
            // 先清理地图
            logMessageRec.clearMap()
            // 再从后端获取图信息
            func.getJson()
            // 将景点部分传到浏览器，以在地图上绘制所有景点
            sendsignal(json_dict["node"])
            logMessageRec.addMessage("选择一个景点，从该景点出发游玩全部景点")
        }
        function clickedANode(node) {
            logMessageRec.addLog("选择的出发点是：" + node)
            func.letRecommend(node)
        }
        function recommendGetted(path, distance) {
            logMessageRec.clearMap()
            logMessageRec.addMessage("游览路线总长度为：" + distance.toString() + "米")
            func.getJson()
            logMessageRec.addLog("前端接收到的推荐路径：" + path)
            sendPath(JSON.parse(path), json_dict)
        }
    }

    Rectangle {
        width: 70
        height: 70
        radius: 50
        z: 2
        anchors.right: webview.right
        anchors.bottom: webview.bottom
        anchors.rightMargin: 10
        anchors.bottomMargin: 10

        Text {
            anchors.centerIn: parent
            text: qsTr("回到\n中心")
            font.pixelSize: 20
            font.family: "Microsoft Yahei"
        }

        MouseArea {
            id: moveCenterBtn
            WebChannel.id: "moveCenterBtn"
            anchors.fill: parent
            hoverEnabled: true
            signal sendsignal()
            onClicked: {
                sendsignal()
            }

            onEntered: {
                parent.border.width = 1
            }

            onExited: {
                parent.border.width = 0
            }
        }
    }

    Rectangle {
        id: logMessageRec
        WebChannel.id: "logMessageRec"
        height: 38
        z: 2
        width: logMessage.implicitWidth + 10
        anchors.left: webview.left
        anchors.bottom: webview.bottom
        anchors.leftMargin: 10
        anchors.bottomMargin: 10
        visible: false
        Text {
            id: logMessage
            anchors.centerIn: parent
            anchors.left: parent.left
            anchors.leftMargin: 5
            text: qsTr("")
            font.pixelSize: 18
            font.family: "Microsoft Yahei"
        }
        signal clearMap()
        function addMessage(message) {
            logMessage.text = message
            if (logMessage.implicitWidth == 0) {
                logMessageRec.visible = false
            } else {
                logMessageRec.visible = true
                console.log(message)
            }
        }
        function addLog(message) {
            console.log(message)
        }
    }
}

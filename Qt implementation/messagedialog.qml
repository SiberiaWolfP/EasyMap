import QtQuick 2.0
//Qt5.6之前版本Popop包含在Qt Labs Controls模块中，之后版本Popop包含在Qt Quick Controls模块中
import QtQuick.Controls 2.5

Popup {
    id: root_dialog
    width: parent.width
    height: parent.height
    modal: false
    focus: true
    //设置窗口关闭方式为按“Esc”键关闭
    closePolicy: Popup.OnEscape
    //设置窗口的背景控件，不设置的话Popup的边框会显示出来
    background: rect

    Rectangle {
        id: rect
        x: parent.width/2 - width/2
        y: parent.height - height
        width: 400
        height: 200
        border.width: 2
        opacity: 1


        Rectangle{
            width: parent.width-4
            height: 2
            anchors.top: parent.top
            anchors.topMargin: 40
            anchors.left: parent.left
            anchors.leftMargin: 2
            radius: 8
        }

        //设置标题栏区域为拖拽区域
        Text {
            id: dialogTitle
            width: parent.width
            height: 40
            anchors.top: parent.top
            text: qsTr("确认操作")
            font.family: "Microsoft Yahei"
            horizontalAlignment: Text.AlignHCenter
            verticalAlignment: Text.AlignVCenter

            MouseArea {
                property point clickPoint: "0,0"

                anchors.fill: parent
                acceptedButtons: Qt.LeftButton
                onPressed: {
                    clickPoint  = Qt.point(mouse.x, mouse.y)
                }
                //标题栏按下并且拖动时
                onPositionChanged: {
                    var offset = Qt.point(mouse.x - clickPoint.x, mouse.y - clickPoint.y)
                    setDlgPoint(offset.x, offset.y)
                }
            }
        }

        Label {
            id: dialogMessage
            anchors.left: rect.left
            anchors.top: dialogTitle.bottom
            anchors.topMargin: 20
            width: rect.width
            height: 50
            horizontalAlignment: Text.AlignHCenter
            text: qsTr("确认添加这条道路吗？")
            font.family: "Microsoft Yahei"
        }

        Button {
            id: dialogAccept
            anchors.left: rect.left
            anchors.bottom: rect.bottom
            anchors.leftMargin: 50
            anchors.bottomMargin: 20
            width: 120
            height: 40
            text: qsTr("确认")
            font.family: "Microsoft Yahei"
            onClicked: {
                logMessageRec.clearMap()
                logMessageRec.addMessage("")
                addSideBtn.acceptAddSide()
                root_dialog.close()
                root_dialog.destroy()
            }
        }
        Button {
            id: dialogCancel
            anchors.right: rect.right
            anchors.bottom: rect.bottom
            anchors.rightMargin: 50
            anchors.bottomMargin: 20
            width: 120
            height: 40
            text: qsTr("取消")
            font.family: "Microsoft Yahei"
            onClicked: {
                logMessageRec.clearMap()
                logMessageRec.addMessage("")
                root_dialog.close()
                root_dialog.destroy()
            }
        }
    }

    function setDlgPoint(dlgX ,dlgY)
    {
        //设置窗口拖拽不能超过父窗口
        if(rect.x + dlgX < 0)
        {
            rect.x = 0
        }
        else if(rect.x + dlgX > rect.parent.width - rect.width)
        {
            rect.x = rect.parent.width - rect.width
        }
        else
        {
            rect.x = rect.x + dlgX
        }
        if(rect.y + dlgY < 0)
        {
            rect.y = 0
        }
        else if(rect.y + dlgY > rect.parent.height - rect.height)
        {
            rect.y = rect.parent.height - rect.height
        }
        else
        {
            rect.y = rect.y + dlgY
        }
    }

}


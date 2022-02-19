// 全局变量

// 地图
var map;
var simpleMarker;
var svgMarker;
var mouseTool;
var clickListener;
// 在添加地点后暂存地点marker，等待反馈是否添加成功
var markerCache;
// 道路暂存
var roadCache;
// 地点覆盖物组
var markerGroup;
// 道路覆盖物组
var roadGroup;
var isMouseHoverPolyLine;
// 折线绘制过程中与其他道路的交叉点
var crossPoints = {};
// 折线绘制过程中交叉的道路
var crossRoads = {};
// 折线绘制过程中随时更新绘制了多少个折点
var allPointsNumber = 0;
var pickMarkers = {};
// 最短路径
var shortestPath;

// 初始化部分

map = new AMap.Map('container',
    {
        resizeEnable: true,
        expandZoomRange: true,
        zooms: [3, 20],
        zoom: 15,
        center: [114.364829, 30.537861],     // 武汉大学坐标
        viewMode: '2D',  //设置地图模式
        lang: 'zh_cn',  //设置地图语言类型
        defaultCursor: 'default'
    });
map.on('complete',
    function() {
        server.MapLoaded();
    });
map.on('rightclick', rightClick);
mouseTool = new AMap.MouseTool(map);
mouseTool.on('draw', drawEnd);
map.addControl(new AMap.Scale());
map.addControl(new AMap.ToolBar());
markerGroup = new AMap.OverlayGroup();
roadGroup = new AMap.OverlayGroup();
markerGroup.setMap(map);
roadGroup.setMap(map);
AMapUI.loadUI(['overlay/SimpleMarker', 'overlay/SvgMarker'],
    function (SimpleMarker, SvgMarker) {
        simpleMarker = SimpleMarker;
        svgMarker = SvgMarker;
    });
//    map.on('click', clickMap);
//    map.on('rightclick', rightClick);


// 函数定义部分

// js调用C#方法示例
function toProgram() {
    server.Show("js to program").then(function(res) {
        server.Debug("a");
    });
}

function show(msg)
{
    server.Debug("program to js: " + msg);
    return "success";
}

function initMap(data) {
    roadGroup.clearOverlays();
    var json = JSON.parse(data);
    //创建一个shape实例
    var shape = new svgMarker.Shape.WaterDrop({
        height: 20, //高度
        //width: **, //不指定时会维持默认的宽高比
        fillColor: '#3D93FD', //填充色
        strokeWidth: 1, //描边宽度
        strokeColor: 'white' //描边颜色
    });
    let thisArg = json['Places'];
    for (var i in thisArg) {
        if (Object.prototype.hasOwnProperty.call(thisArg, i)) {
            //利用该shape构建SvgMarker
            var marker = new svgMarker(
                //第一个参数传入shape实例
                shape,
                //第二个参数为SimpleMarker的构造参数（iconStyle除外）
                {
                    showPositionPoint: false, //显示定位点
                    map: map,
                    position: [thisArg[i]['Longitude'], thisArg[i]['Latitude']],
                    extData: { id: thisArg[i]['Id'] }
                }
            );
            marker.setLabel({
                content: thisArg[i]['Name'],
                direction: "right"
            });
            markerGroup.addOverlay(marker);
        }
    }
    // 向地图添加道路
    thisArg = json['Roads'];
    for (var roadId in thisArg) {
        if (Object.prototype.hasOwnProperty.call(thisArg, roadId)) {
            var path = [];
            let places = thisArg[Number.parseInt(roadId)];
            for (var j in places) {
                if (Object.prototype.hasOwnProperty.call(places, j)) {
                    path.push([json['RoadPoints'][places[j].toString()]["Longitude"],
                        json['RoadPoints'][places[j].toString()]["Latitude"]]);
                }
            }
            var polyLine = new AMap.Polyline({
                path: path,
                bubble: false,
                strokeColor: '#' + ('000000' + (Number.parseInt(roadId) * 100).toString(16)).substr(-6),
//                strokeColor: '#FFFFFF',
                strokeOpacity: 1,
//                isOutline: true,
                outlineColor: '#DED8CD',
                borderWeight: 2,
                strokeWeight: 10,
                lineJoin: 'round',
                map: map,
                lineCap: 'round',
                extData: {id: Number.parseInt(roadId)}
            });
//            polyLine.on('mouseover', mouseoverPolyLine);
//            polyLine.on('mouseout', mouseoutPolyLine);
//            polyLine.on('click', clickPolyLine);
            roadGroup.addOverlay(polyLine);
        }
    }
    clickListener = AMap.event.addListener(roadGroup, 'mouseover', mouseoverPolyLine);
    clickListener = AMap.event.addListener(roadGroup, 'mouseout', mouseoutPolyLine);
    clickListener = AMap.event.addListener(roadGroup, 'click', clickPolyLine);
}

// 使地图回到中心
function moveToCenter() {
    map.panTo([114.364829, 30.537861]);
}

function addPloyLine(msg) {
    var json = JSON.parse(msg);
    var polyLine = new AMap.Polyline({
        path: json['path'],
        bubble: false,
        strokeColor: '#' + ('000000' + (json['id'] * 100).toString(16)).substr(-6),
//        strokeColor: '#FFFFFF',
        strokeOpacity: 1,
        //                isOutline: true,
        outlineColor: '#DED8CD',
        borderWeight: 2,
        strokeWeight: 10,
        lineJoin: 'round',
        map: map,
        lineCap: 'round',
        extData: { id: json['id'] }
    });
    roadGroup.addOverlay(polyLine);
    clickListener = AMap.event.addListener(roadGroup, 'mouseover', mouseoverPolyLine);
    clickListener = AMap.event.addListener(roadGroup, 'mouseout', mouseoutPolyLine);
    clickListener = AMap.event.addListener(roadGroup, 'click', clickPolyLine);
    roadGroup.setOptions({ cursor: 'crosshair' });
}

function displayPath(path) {
    if (shortestPath != undefined) {
        shortestPath.setMap(null);
    }
    var json = JSON.parse(path);
    var polyLine = new AMap.Polyline({
        path: json,
        map: map,
        strokeWeight: 8,
        strokeColor: 'blue',
        strokeOpacity: 1,
        lineJoin: 'round',
        lineCap: 'square',
        showDir: true
    });
    shortestPath = polyLine;
    map.add(shortestPath);
}

// 后台启动，开始添加地点或道路，type为添加物类型
function startAdd(type) {
    server.Debug("JS收到JSON,方法startAdd,内容: " + type);
    var json = JSON.parse(type);
    if (json['class'] === 'point') {
        // 防止重复绑定
        removeListener();
        clickListener = AMap.event.addListener(map, 'click', function (e) { addMarker(json['type'], e) });
        //        map.on('click', function (e) {addMarker(json['type'], e)});
    }
    else if (json['class'] === 'road') {
        removeListener();
        clickListener = AMap.event.addListener(roadGroup, 'mouseover', mouseoverPolyLine);
        clickListener = AMap.event.addListener(roadGroup, 'mouseout', mouseoutPolyLine);
        clickListener = AMap.event.addListener(roadGroup, 'click', clickPolyLine);
        clickListener = AMap.event.addListener(map, 'click', clickMap);
//        roadGroup.on('mouseover', mouseoverPolyLine);
//        roadGroup.on('mouseout', mouseoutPolyLine);
//        roadGroup.on('click', clickPolyLine);
        roadGroup.setOptions({ cursor: 'crosshair' });
//        map.on('click', clickMap);
        allPointsNumber = 0;
        crossPoints = {};
        draw('polyline');
    }
}

function removeListener() {
    if (clickListener) {
        AMap.event.removeListener(clickListener);
    }
}

// 后台启动，结束添加，并解绑鼠标事件
function endAddPoint() {
    if (clickListener) {
        AMap.event.removeListener(clickListener);
    }
}

// 后台调用，停止添加道路
function endAddRoad() {
    // 关闭鼠标工具，保留所绘制覆盖物
    mouseTool.close(false);
    removeListener();
    roadGroup.setOptions({ cursor: 'default' });
}

// 添加地点成功，后台点击对话框OK按键触发
function addPointSuccess(msg) {
    var json = JSON.parse(msg);
    markerCache.setExtData({ id: json['Id'] });
    markerCache.setLabel({
        content: json['Name'],
        direction: "right"
    });

}

function addRoadSuccess(roadId) {
//    roadCache.setMap(map);
//    roadCache.setExtData({ id: roadId });
//    roadCache.on('mouseover', mouseoverPolyLine);
//    roadCache.on('mouseout', mouseoutPolyLine);
//    roadCache.on('click', clickPolyLine);
//    roadCache.setOptions({ cursor: 'crosshair' });
//    roadGroup.addOverlay(roadCache);
    allPointsNumber = 0;
    crossPoints = {};
}

// 添加地点失败，可由后台点击对话框取消按钮触发
function addPointFail() {
    markerCache.setMap(null);
}

function addMarker(type, e) {
    server.Debug('mouse click;' + ' position: ' + e.lnglat.getLng() + ',' + e.lnglat.getLat());
    var icon, marker;
    // 添加普通地点
    if (type === 'normal') {
        //创建一个shape实例
        var shape = new svgMarker.Shape.WaterDrop({
            height: 20, //高度
            //width: **, //不指定时会维持默认的宽高比
            fillColor: '#3D93FD', //填充色
            strokeWidth: 1, //描边宽度
            strokeColor: 'white' //描边颜色
        });
        //利用该shape构建SvgMarker
        var marker = new svgMarker(
            //第一个参数传入shape实例
            shape,
            //第二个参数为SimpleMarker的构造参数（iconStyle除外）
            {
                showPositionPoint: false, //显示定位点
                map: map,
                position: e.lnglat,
            });
//        marker.setLabel({
//            content: "测试",
//            direction: "right"
//        });
//        icon = new AMap.Icon({
//            size: new AMap.Size(20, 26),
//            image: 'poi-marker.png',
//            imageOffset: new AMap.Pixel(-108, -2),
//            imageSize: new AMap.Size(336, 202)
//        });
//        marker = new AMap.Marker({
//            position: new AMap.LngLat(114.364829, 30.537861), // 经纬度对象，如 new AMap.LngLat(116.39, 39.9); 也可以是经纬度构成的一维数组[116.39, 39.9]
//            icon: icon,
//            offset: new AMap.Pixel(-10, -25)
//        });
        map.add(marker);
        markerCache = marker;
        var json = {};
        json['lng'] = e.lnglat.getLng();
        json['lat'] = e.lnglat.getLat();
        var str = JSON.stringify(json);
        server.AddPlaceEnd(str);
    }
    // 添加路径搜索起点
    else if (type === 'start') {
        if (pickMarkers.hasOwnProperty('start')) {
            pickMarkers['start'].setMap(null);
        }
        //创建一个shape实例
        var shape = new svgMarker.Shape.WaterDrop({
            height: 30, //高度
            //width: **, //不指定时会维持默认的宽高比
            fillColor: '#3D93FD', //填充色
            strokeWidth: 1, //描边宽度
            strokeColor: 'white' //描边颜色
        });
        //利用该shape构建SvgMarker
        var marker = new svgMarker(
            //第一个参数传入shape实例
            shape,
            //第二个参数为SimpleMarker的构造参数（iconStyle除外）
            {
                showPositionPoint: false, //显示定位点
                map: map,
                position: e.lnglat,
                iconTheme: 'numv2',
                iconLabel: '起'
            });
        map.add(marker);
        pickMarkers['start'] = marker;
        server.EndPick('start', JSON.stringify(marker.getPosition()));
        if (clickListener) {
            AMap.event.removeListener(clickListener);
        }
    }
    // 添加路径搜索终点
    else if (type === 'end') {
        if (pickMarkers.hasOwnProperty('end')) {
            pickMarkers['end'].setMap(null);
        }
        //创建一个shape实例
        var shape = new svgMarker.Shape.WaterDrop({
            height: 30, //高度
            //width: **, //不指定时会维持默认的宽高比
            fillColor: '#F34234', //填充色
            strokeWidth: 1, //描边宽度
            strokeColor: 'white' //描边颜色
        });
        //利用该shape构建SvgMarker
        var marker = new svgMarker(
            //第一个参数传入shape实例
            shape,
            //第二个参数为SimpleMarker的构造参数（iconStyle除外）
            {
                showPositionPoint: false, //显示定位点
                map: map,
                position: e.lnglat,
                iconTheme: 'numv2',
                iconLabel: '终'
            });
        map.add(marker);
        pickMarkers['end'] = marker;
        server.EndPick('end', JSON.stringify(marker.getPosition()));
        if (clickListener) {
            AMap.event.removeListener(clickListener);
        }
    }
    
}

// 鼠标工具绘制完成触发
function drawEnd(e) {
    e.obj.setMap(null);
    // 高德鼠标工具绘制的折线样式不全，清除后重新绘制
    var polyline = new AMap.Polyline({
        strokeColor: '#FFFFFF',
        strokeOpacity: 1,
        isOutline: false,
        outlineColor: '#DED8CD',
        borderWeight: 2,
        strokeWeight: 10,
        lineJoin: 'round',
        path: e.obj.getPath(),
        lineCap: 'round'
    });
    roadCache = polyline;
    var str = JSON.stringify(e.obj.getPath());
    var str2 = JSON.stringify(crossPoints);
    server.AddRoadEnd(str, str2);
    //    mouseTool.close(true);
}

function searchClosetPoint(lng, lat) {
    var point = [lng, lat];
    var result = {};
    var lines = roadGroup.getOverlays();
    var distance = Infinity;
    var closetRoadId;
    var closetRoadIndex;
    for (var lineIndex in lines) {
        let d = AMap.GeometryUtil.distanceToLine(point, lines[lineIndex].getPath());
        if (d <= distance) {
            distance = d;
            closetRoadId = lines[lineIndex].getExtData().id;
            closetRoadIndex = lineIndex;
        }
    }
    result["p"] = AMap.GeometryUtil.closestOnLine(point, lines[closetRoadIndex].getPath());
    result["distance"] = distance;
    result["roadId"] = closetRoadId;
    var roadPath = lines[closetRoadIndex].getPath();
    for (var i = 1; i < roadPath.length; i++) {
        if (AMap.GeometryUtil.isPointOnSegment(result["p"], roadPath[i - 1], roadPath[i], 1)) {
            result["segment"] = [i - 1, i];
        }
    }
//    console.debug(result);
    return JSON.stringify(result);
}

function delRoad() {
    removeListener();
    roadGroup.setOptions({ cursor: 'hand' });
    clickListener = AMap.event.addListener(roadGroup, 'click', function(e) { clickPolyLine(e, "del") });
}

function delRoadSuccess() {
    roadCache.setMap(null);
    roadGroup.removeOverlay(roadCache);
}

function endOperation() {
    crossPoints = {};
    allPointsNumber = 0;
    pickMarkers = {};
    // 最短路径
    shortestPath = null;
    removeListener();
    mouseTool.close(false);
    roadGroup.setOptions({ cursor: 'default' });
}

function setFeatures(pointFeature, roadFeature) {
    var features = ['bg', 'point', 'road', 'building'];
    switch (pointFeature) {
        case '0':
        case '2':
            var i = features.indexOf('point');
            features.splice(i, 1);
            break;
    }
    switch (roadFeature) {
        case '0':
        case '2':
            var i = features.indexOf('road');
            features.splice(i, 1);
            break;
    }
    map.setFeatures(features);
}

function draw(type) {
    switch (type) {
    case 'polyline': {
        mouseTool.polyline({
            strokeColor: '#FFFFFF',
            strokeOpacity: 1,
            isOutline: true,
            outlineColor: '#DED8CD',
            borderWeight: 2,
            strokeWeight: 10,
            lineJoin: 'round',
            lineCap: 'round'
            //同Polyline的Option设置
        });
        break;
    }
    }
}

// 事件处理部分

// 绘制折线时单击了地图，确认了折线的一点
function clickMap(e) {
    allPointsNumber++;
    server.Debug("clickMap");
};

function rightClick(e) {
    server.Debug("rightClick");
    server.SearchClosetPoint(e.lnglat.getLng(), e.lnglat.getLat());
}

function mouseoverPolyLine(e) {
//    server.Debug(e.lnglat.toString());
    server.Debug(e.target.getExtData().id.toString());
//    document.getElementById('container').style.cssText += 'cursor:crosshair !important;';
    isMouseHoverPolyLine = true;
}

function mouseoutPolyLine(e) {
    server.Debug(e.lnglat.toString());
    isMouseHoverPolyLine = false;
}

// 绘制折线时点击到了其他折线，表明新路与旧路间有交叉点
function clickPolyLine(e, type) {
    var roadId = e.target.getExtData().id;
    if (type === 'del') {
        server.DelRoadEnd(roadId);
        roadCache = e.target;
    } else {
        crossPoints[allPointsNumber.toString()] = roadId;
        crossRoads[roadId.toString()] = e.target;
        server.Debug(e.lnglat.toString());
    }
    
}

function closestOnLine(msg) {
    console.debug(msg);
    var json = JSON.parse(msg);
    var result = {};
    result["p"] = AMap.GeometryUtil.closestOnLine(json['p'], json['line']);
    for (var i = 1; i < json['line'].length; i++) {
        console.debug('distance:' + AMap.GeometryUtil.distanceToSegment(json['p'], json['line'][i - 1], json['line'][i]));
        if (AMap.GeometryUtil.isPointOnSegment(json['p'], json['line'][i - 1], json['line'][i])) {
            result["segment"] = [i - 1, i];
        }
    }
    return JSON.stringify(result);
}

function updateRoad(msg) {
    var json = JSON.parse(msg);
    crossRoads[json['modifyRoadId'].toString()].setPath(json['modifyRoadPath']);
    var polyLine = new AMap.Polyline({
        path: json['newRoadPath'],
        bubble: false,
        strokeColor: '#' + ('000000' + (json['newRoadId'] * 100).toString(16)).substr(-6),
//        strokeColor: '#FFFFFF',
        strokeOpacity: 1,
        //                isOutline: true,
        outlineColor: '#DED8CD',
        borderWeight: 2,
        strokeWeight: 10,
        lineJoin: 'round',
        map: map,
        lineCap: 'round',
        extData: { id: json['newRoadId'] }
    });
    roadGroup.addOverlay(polyLine);
    clickListener = AMap.event.addListener(roadGroup, 'mouseover', mouseoverPolyLine);
    clickListener = AMap.event.addListener(roadGroup, 'mouseout', mouseoutPolyLine);
    clickListener = AMap.event.addListener(roadGroup, 'click', clickPolyLine);
    roadGroup.setOptions({ cursor: 'crosshair' });
}
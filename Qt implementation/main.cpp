#include <QGuiApplication>
#include <QQmlApplicationEngine>
#include <QtWebEngine>
#include "mapfunc.h"

int main(int argc, char *argv[])
{
    QCoreApplication::setAttribute(Qt::AA_EnableHighDpiScaling);

    QGuiApplication app(argc, argv);
    QtWebEngine::initialize();
    qmlRegisterType<MapFunc>("MapFunc",1,0,"MapFunc");
    QQmlApplicationEngine engine;
    engine.load(QUrl(QStringLiteral("qrc:/main.qml")));
    if (engine.rootObjects().isEmpty())
        return -1;

    return app.exec();
}

@echo off
chcp 65001 >nul
echo ========================================
echo py12306 购票助手启动脚本
echo ========================================
echo.
echo 请选择运行模式：
echo 1. 测试模式（检查配置）
echo 2. 正式运行
echo 3. Web管理模式
echo 4. 退出
echo.
set /p choice=请输入选项 (1-4): 

if "%choice%"=="1" (
    echo.
    echo 正在启动测试模式...
    conda run -n py12306 python main.py -t
) else if "%choice%"=="2" (
    echo.
    echo 正在启动购票程序...
    echo 提示：按 Ctrl+C 可以停止程序
    conda run -n py12306 python main.py
) else if "%choice%"=="3" (
    echo.
    echo 正在启动 Web 管理模式...
    echo Web 地址: http://127.0.0.1:8008
    echo 默认账号: admin / password
    conda run -n py12306 python main.py
) else if "%choice%"=="4" (
    exit
) else (
    echo 无效选项！
    pause
)

pause

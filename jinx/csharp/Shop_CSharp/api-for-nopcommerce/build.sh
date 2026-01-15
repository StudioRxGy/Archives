#!/bin/bash

# nopCommerce API Plugin 构建脚本
# 适用于 Linux/macOS

echo "=== nopCommerce API Plugin 构建脚本 ==="
echo ""

# 检查.NET SDK版本
echo "检查.NET SDK版本..."
DOTNET_VERSION=$(dotnet --version)
echo "检测到 .NET SDK 版本: $DOTNET_VERSION"

# 检查nopCommerce源代码目录
echo "检查nopCommerce源代码目录..."
NOPCOMMERCE_PATH="../nopCommerce"
if [ -d "$NOPCOMMERCE_PATH" ]; then
    echo "nopCommerce源代码目录存在: $NOPCOMMERCE_PATH"
else
    echo "警告: nopCommerce源代码目录不存在: $NOPCOMMERCE_PATH"
    echo "请确保nopCommerce源代码在同一目录级别"
fi

echo ""

# 清理之前的构建
echo "清理之前的构建..."
if [ -d "bin" ]; then
    rm -rf bin
fi
if [ -d "obj" ]; then
    rm -rf obj
fi
echo "清理完成"

echo ""

# 恢复NuGet包
echo "恢复NuGet包..."
dotnet restore
if [ $? -eq 0 ]; then
    echo "包恢复成功"
else
    echo "包恢复失败"
    exit 1
fi

echo ""

# 构建项目
echo "构建项目..."
dotnet build --configuration Release
if [ $? -eq 0 ]; then
    echo "构建成功"
else
    echo "构建失败"
    exit 1
fi

echo ""

# 运行测试
echo "运行测试..."
dotnet test --configuration Release --no-build
if [ $? -eq 0 ]; then
    echo "测试通过"
else
    echo "测试失败"
    exit 1
fi

echo ""

# 显示构建结果
echo "=== 构建完成 ==="
echo "输出目录: bin/Release"
echo "插件DLL: bin/Release/net9.0/Nop.Plugin.Api.dll"

echo ""
echo "下一步:"
echo "1. 将插件DLL复制到nopCommerce的Plugins目录"
echo "2. 在nopCommerce管理面板中安装插件"
echo "3. 配置API设置和权限"

echo ""
echo "构建脚本执行完成!" 
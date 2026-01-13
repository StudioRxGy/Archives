# Trading API Automation

AST交易平台自动化交易脚本

## 功能特性

- 支持批量执行买入订单
- 支持批量执行卖出订单
- 交互式命令行界面
- 自动生成唯一订单ID
- 请求间延迟控制
- 详细的执行状态显示

## 安装依赖

```bash
pip install -r requirements.txt
```

## 使用方法

```bash
python main.py
```

按照提示选择操作类型和执行次数即可。

## 项目结构

```
.
├── main.py                 # 主入口脚本
├── requirements.txt        # Python依赖
├── README.md              # 项目说明
└── src/                   # 源代码目录
    ├── __init__.py        # 包初始化
    ├── config.py          # 配置模块
    ├── models.py          # 数据模型
    ├── user_interface.py  # 用户界面
    ├── http_client.py     # HTTP客户端
    ├── order_generator.py # 订单生成器
    └── order_executor.py  # 订单执行器
```

## 配置说明

主要配置在 `src/config.py` 中：

- API地址和端点
- 请求头和认证信息
- 订单参数配置
- 请求延迟设置
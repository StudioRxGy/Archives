#!/usr/bin/env python3
"""
Trading API Automation Script
自动化交易脚本主入口
"""

from src.user_interface import UserInterface
from src.order_executor import OrderExecutor
from src.http_client import HTTPClient


def main():
    """程序主入口"""
    try:
        # 初始化组件
        http_client = HTTPClient()
        order_executor = OrderExecutor(http_client)
        ui = UserInterface()
        
        print("=== AST交易平台自动化脚本 ===")
        
        # 获取用户选择
        operation = ui.get_operation_choice()
        count = ui.get_execution_count()
        
        # 显示摘要并确认
        if not ui.show_summary(operation, count):
            print("操作已取消")
            return
        
        # 执行订单
        if operation == "buy":
            order_executor.execute_buy_orders(count)
        elif operation == "sell":
            order_executor.execute_sell_orders(count)
        
        print("所有订单执行完成！")
        
    except KeyboardInterrupt:
        print("\n操作被用户中断")
    except Exception as e:
        print(f"发生错误: {e}")


if __name__ == "__main__":
    main()
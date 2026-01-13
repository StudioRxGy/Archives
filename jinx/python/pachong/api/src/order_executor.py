"""
订单执行器模块 - 执行订单请求的核心逻辑
"""

import time
from typing import List
from .http_client import HTTPClient
from .order_generator import OrderGenerator
from .models import OrderResult
from .config import REQUEST_DELAY


class OrderExecutor:
    """订单执行器类"""
    
    def __init__(self, http_client: HTTPClient):
        """初始化订单执行器"""
        self.http_client = http_client
        self.order_generator = OrderGenerator()
    
    def execute_buy_orders(self, count: int):
        """执行指定次数的买入订单"""
        print(f"\n开始执行 {count} 次买入订单...")
        
        results = []
        for i in range(count):
            print(f"\n执行第 {i + 1} 次买入订单:")
            
            # 生成买入订单
            order = self.order_generator.generate_buy_order()
            
            # 执行订单
            result = self._execute_single_order(order)
            results.append(result)
            
            # 显示结果
            print(f"  结果: {result}")
            
            # 添加延迟（除了最后一次）
            if i < count - 1:
                time.sleep(REQUEST_DELAY)
        
        self._show_summary(results, "买入")
    
    def execute_sell_orders(self, count: int):
        """执行指定次数的卖出订单"""
        print(f"\n开始执行 {count} 次卖出订单...")
        
        results = []
        for i in range(count):
            print(f"\n执行第 {i + 1} 次卖出订单:")
            
            # 生成卖出订单
            order = self.order_generator.generate_sell_order()
            
            # 执行订单
            result = self._execute_single_order(order)
            results.append(result)
            
            # 显示结果
            print(f"  结果: {result}")
            
            # 添加延迟（除了最后一次）
            if i < count - 1:
                time.sleep(REQUEST_DELAY)
        
        self._show_summary(results, "卖出")
    
    def _execute_single_order(self, order_data) -> OrderResult:
        """执行单个订单请求"""
        # 转换为表单数据
        form_data = order_data.to_form_data()
        
        # 发送HTTP请求
        return self.http_client.post_order(form_data)
    
    def _show_summary(self, results: List[OrderResult], operation_type: str):
        """显示执行摘要"""
        total = len(results)
        success_count = sum(1 for r in results if r.success)
        failure_count = total - success_count
        
        print(f"\n=== {operation_type}订单执行摘要 ===")
        print(f"总计: {total}")
        print(f"成功: {success_count}")
        print(f"失败: {failure_count}")
        
        if failure_count > 0:
            print("\n失败详情:")
            for i, result in enumerate(results):
                if not result.success:
                    print(f"  第{i + 1}次: {result.error_message}")
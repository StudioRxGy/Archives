"""
订单生成器模块 - 生成订单参数
"""

import time
from .models import OrderData
from .config import ORDER_CONFIG


class OrderGenerator:
    """订单生成器类"""
    
    @staticmethod
    def generate_buy_order() -> OrderData:
        """生成买入订单数据"""
        return OrderData(
            side="BUY_OPEN",
            type=ORDER_CONFIG["type"],
            price_type=ORDER_CONFIG["price_type"],
            trigger_price=ORDER_CONFIG["trigger_price"],
            leverage=ORDER_CONFIG["leverage"],
            quantity=ORDER_CONFIG["buy_quantity"],
            symbol_id=ORDER_CONFIG["symbol_id"],
            client_order_id=OrderGenerator.generate_client_order_id(),
            exchange_id=ORDER_CONFIG["exchange_id"],
            order_side="BUY",
            is_cross=ORDER_CONFIG["is_cross"],
            time_in_force=ORDER_CONFIG["time_in_force"],
            deduction=ORDER_CONFIG["deduction"]
        )
    
    @staticmethod
    def generate_sell_order() -> OrderData:
        """生成卖出订单数据"""
        return OrderData(
            side="SELL_OPEN",
            type=ORDER_CONFIG["type"],
            price_type=ORDER_CONFIG["price_type"],
            trigger_price=ORDER_CONFIG["trigger_price"],
            leverage=ORDER_CONFIG["leverage"],
            quantity=ORDER_CONFIG["sell_quantity"],
            symbol_id=ORDER_CONFIG["symbol_id"],
            client_order_id=OrderGenerator.generate_client_order_id(),
            exchange_id=ORDER_CONFIG["exchange_id"],
            order_side="SELL",
            is_cross=ORDER_CONFIG["is_cross"],
            time_in_force=ORDER_CONFIG["time_in_force"],
            deduction=ORDER_CONFIG["deduction"]
        )
    
    @staticmethod
    def generate_client_order_id() -> str:
        """生成唯一的客户端订单ID"""
        # 使用当前时间戳（毫秒）作为唯一ID
        timestamp_ms = int(time.time() * 1000)
        return f"auto_{timestamp_ms}"
"""
数据模型 - 定义订单数据结构
"""

from dataclasses import dataclass
from typing import Union


@dataclass
class OrderData:
    """订单数据结构"""
    side: str  # BUY_OPEN 或 SELL_OPEN
    type: str  # LIMIT
    price_type: str  # MARKET_PRICE
    trigger_price: str  # 空字符串
    leverage: int  # 400
    quantity: float  # 3.00 或 4.00
    symbol_id: str  # BTCUSDT_PERP
    client_order_id: str  # 时间戳生成的唯一ID
    exchange_id: int  # 888
    order_side: str  # BUY 或 SELL
    is_cross: bool  # True
    time_in_force: str  # IOC
    deduction: str  # score
    
    def to_form_data(self) -> dict:
        """转换为表单数据格式"""
        return {
            'side': self.side,
            'type': self.type,
            'price_type': self.price_type,
            'trigger_price': self.trigger_price,
            'leverage': str(self.leverage),
            'quantity': str(self.quantity),
            'symbol_id': self.symbol_id,
            'client_order_id': self.client_order_id,
            'exchange_id': str(self.exchange_id),
            'order_side': self.order_side,
            'is_cross': str(self.is_cross).lower(),
            'time_in_force': self.time_in_force,
            'deduction': self.deduction
        }


@dataclass
class OrderResult:
    """订单执行结果"""
    success: bool
    status_code: int
    response_data: Union[dict, str]
    error_message: str = ""
    
    def __str__(self) -> str:
        if self.success:
            return f"成功 (状态码: {self.status_code})"
        else:
            return f"失败 (状态码: {self.status_code}) - {self.error_message}"
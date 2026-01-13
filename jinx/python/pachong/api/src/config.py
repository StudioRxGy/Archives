"""
配置模块 - 定义API配置和常量
"""

from dataclasses import dataclass, field
from typing import Dict


@dataclass
class APIConfig:
    """API配置类"""
    base_url: str = "https://www.ast1001.com"
    endpoint: str = "/api/contract/order/create"
    c_token: str = "LoPMadD6q1VLvHcIDJC4OffUz8Ifi7qN"
    
    headers: Dict[str, str] = field(default_factory=lambda: {
        'accept': 'application/json, text/plain, */*',
        'accept-language': 'zh-hk',
        'content-type': 'application/x-www-form-urlencoded',
        'origin': 'https://www.ast1001.com',
        'referer': 'https://www.ast1001.com/zh-hk/futures/BTCUSDT_PERP',
        'timezone': 'GMT+0800',
        'user-agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36',
        'x-requested-with': 'XMLHttpRequest'
    })
    
    cookies: str = (
        "device=fb892b5b376336fbfffb6541f42dc041; unit=USD; "
        "c_token=LoPMadD6q1VLvHcIDJC4OffUz8Ifi7qN; "
        "lang=zh-hk; timezone=GMT%2B0800"
    )
    
    @property
    def full_url(self) -> str:
        """获取完整的API URL"""
        return f"{self.base_url}{self.endpoint}"


# 全局配置实例
api_config = APIConfig()

# 请求延迟配置（秒）
REQUEST_DELAY = 1.0

# 订单配置
ORDER_CONFIG = {
    "leverage": 400,
    "symbol_id": "BTCUSDT_PERP",
    "exchange_id": 888,
    "is_cross": True,
    "time_in_force": "IOC",
    "deduction": "score",
    "type": "LIMIT",
    "price_type": "MARKET_PRICE",
    "trigger_price": "",
    "buy_quantity": 3.00,
    "sell_quantity": 4.00
}
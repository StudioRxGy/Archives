"""
HTTP客户端模块 - 处理HTTP请求发送
"""

import requests
from typing import Dict, Any
from .config import api_config
from .models import OrderResult


class HTTPClient:
    """HTTP客户端类"""
    
    def __init__(self):
        """初始化HTTP客户端"""
        self.session = requests.Session()
        self._setup_headers()
    
    def _setup_headers(self):
        """设置请求头和cookies"""
        # 设置默认请求头
        self.session.headers.update(api_config.headers)
        
        # 设置cookies
        # 将cookie字符串解析为字典
        cookie_dict = {}
        for cookie in api_config.cookies.split('; '):
            if '=' in cookie:
                key, value = cookie.split('=', 1)
                cookie_dict[key] = value
        
        self.session.cookies.update(cookie_dict)
    
    def post_order(self, data: Dict[str, Any]) -> OrderResult:
        """发送POST请求"""
        try:
            response = self.session.post(
                api_config.full_url,
                data=data,
                timeout=30
            )
            
            # 尝试解析JSON响应
            try:
                response_data = response.json()
            except ValueError:
                response_data = response.text
            
            # 判断请求是否成功
            success = response.status_code == 200
            error_message = ""
            
            if not success:
                error_message = f"HTTP {response.status_code}"
                if isinstance(response_data, dict) and 'message' in response_data:
                    error_message += f": {response_data['message']}"
            
            return OrderResult(
                success=success,
                status_code=response.status_code,
                response_data=response_data,
                error_message=error_message
            )
            
        except requests.exceptions.Timeout:
            return OrderResult(
                success=False,
                status_code=0,
                response_data="",
                error_message="请求超时"
            )
        except requests.exceptions.ConnectionError:
            return OrderResult(
                success=False,
                status_code=0,
                response_data="",
                error_message="连接失败"
            )
        except Exception as e:
            return OrderResult(
                success=False,
                status_code=0,
                response_data="",
                error_message=str(e)
            )
# -*- coding: utf-8 -*-
"""测试启动流程"""
import sys
print("Step 1: 开始导入模块...")

try:
    from py12306.config import Config
    print("Step 2: Config 导入成功")
    
    config = Config()
    print("Step 3: Config 初始化成功")
    print(f"  - Web启用: {config.WEB_ENABLE}")
    print(f"  - Web端口: {config.WEB_PORT}")
    
    from py12306.app import App
    print("Step 4: App 导入成功")
    
    from py12306.web.web import Web
    print("Step 5: Web 导入成功")
    
    print("\n所有模块导入成功！")
    print("尝试启动 Web 服务...")
    
    # 尝试启动 Web
    Web.run()
    print("Web 服务已启动！")
    
    import time
    print("保持运行 30 秒...")
    time.sleep(30)
    
except Exception as e:
    print(f"\n错误: {e}")
    import traceback
    traceback.print_exc()

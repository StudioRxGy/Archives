"""
用户界面模块 - 处理用户输入和显示输出
"""


class UserInterface:
    """用户界面类"""
    
    def get_operation_choice(self) -> str:
        """获取用户选择的操作类型（买入/卖出）"""
        while True:
            print("\n请选择操作类型:")
            print("1. 买入 (BUY)")
            print("2. 卖出 (SELL)")
            
            choice = input("请输入选择 (1 或 2): ").strip()
            
            if choice == "1":
                return "buy"
            elif choice == "2":
                return "sell"
            else:
                print("无效选择，请输入 1 或 2")
    
    def get_execution_count(self) -> int:
        """获取用户输入的执行次数"""
        while True:
            try:
                count_str = input("请输入执行次数: ").strip()
                
                if not count_str:
                    print("执行次数不能为空，请重新输入")
                    continue
                
                count = int(count_str)
                
                if count <= 0:
                    print("执行次数必须大于0，请重新输入")
                    continue
                
                return count
                
            except ValueError:
                print("请输入有效的数字")
    
    def show_summary(self, operation: str, count: int) -> bool:
        """显示操作摘要并获取用户确认"""
        operation_text = "买入" if operation == "buy" else "卖出"
        
        print(f"\n=== 操作摘要 ===")
        print(f"操作类型: {operation_text}")
        print(f"执行次数: {count}")
        print(f"交易对: BTCUSDT_PERP")
        
        while True:
            confirm = input("\n确认执行? (y/n): ").strip().lower()
            
            if confirm in ['y', 'yes', '是']:
                return True
            elif confirm in ['n', 'no', '否']:
                return False
            else:
                print("请输入 y 或 n")
    
    def show_progress(self, current: int, total: int, status: str):
        """显示执行进度"""
        print(f"[{current}/{total}] {status}")
    
    def show_error(self, message: str):
        """显示错误信息"""
        print(f"错误: {message}")
    
    def show_success(self, message: str):
        """显示成功信息"""
        print(f"成功: {message}")
from selenium.webdriver.common.by import By
from selenium.webdriver.support.expected_conditions import element_to_be_clickable
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.common.exceptions import TimeoutException, WebDriverException
from basepage.base import Page
from config import setting
from tools.element_check_tool import ElementPack
import time


search = ElementPack(element_path=setting.UI_YAML_PATH)

class Astral(Page):
    def login(self, username, password):
        """登录流程（含完整错误处理）"""
        url = "https://www.ast1001.com/zh-cn/login"
        print(f"[DEBUG] ==== 进入 login 方法 ====")
        print(f"[DEBUG] 目标 URL: {url}")

        try:
            print(f"[DEBUG] 正在调用 open_url...")
            self.open_url(url)
            print(f"[DEBUG] open_url 调用完成")

            # 定位元素并操作
            print(f"[DEBUG] 开始定位元素...")

            tyan = search['同意按钮']
            jieshou = search['接受cookie']
            yxdl = search['切换邮箱登录']
            zhsrk = search['账号输入框']
            mmsrk = search['密码输入框']
            dlbutton = search['登录按钮']
            print(f"[DEBUG] 元素定位完成")

            print(f"[DEBUG] 点击同意按钮")
            self.click(tyan)
            print(f"[DEBUG] 点击接受cookie按钮")
            self.click(jieshou)
            print(f"[DEBUG] 点击邮箱登录按钮")
            self.click(yxdl)

            print(f"[DEBUG] 输入账号: {username}")
            self.text_input(zhsrk, username)
            print(f"[DEBUG] 输入密码: {password}")
            self.text_input(mmsrk, password)
            print(f"[DEBUG] 点击登录按钮")
            self.click(dlbutton)
            time.sleep(2)


        except TimeoutException as e:
            print(f"[ERROR] 操作超时: {str(e)}")
            return "fail"
        except WebDriverException as e:
            print(f"[ERROR] 浏览器错误: {str(e)}")
            return "fail"
        except Exception as e:
            print(f"[ERROR] 未知异常: {str(e)}")
            return "fail"



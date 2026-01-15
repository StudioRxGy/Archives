from selenium.webdriver.common.by import By
from selenium.webdriver.support.expected_conditions import element_to_be_clickable
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.common.exceptions import TimeoutException, WebDriverException
from basepage.base import Page
from config import setting
from tools.element_check_tool import ElementPack
import time
import configparser
import os

current_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.abspath(os.path.join(current_dir, os.pardir, os.pardir, os.pardir))

def get_current_environment():
    """从pytest.ini获取当前环境配置"""
    pytest_ini_path = os.path.join(project_root, 'pytest.ini')
    config = configparser.ConfigParser()
    config.read(pytest_ini_path, encoding='utf-8')

    default_env = 'environment'
    try:
        env = config.get('pytest', 'environment')
        print(f"[INFO] 当前执行环境: {env}")
        return env
    except (configparser.NoSectionError, configparser.NoOptionError):
        print(f"[WARNING] pytest.ini中未配置环境，使用默认环境: {default_env}")
        return default_env

def get_environment_config(env_name):
    """从Config.ini获取指定环境的配置"""
    config_ini_path = os.path.join(project_root, 'config', 'Config.ini')
    config = configparser.ConfigParser()
    config.read(config_ini_path, encoding='utf-8')

    try:
        host = config.get(env_name, 'host')
        token = config.get(env_name, 'token')
        print(f"[INFO] 加载环境配置: {env_name} | Host: {host}")
        return {'host': host, 'token': token}
    except (configparser.NoSectionError, configparser.NoOptionError) as e:
        raise RuntimeError(f"Config.ini中找不到环境配置 '{env_name}': {str(e)}")

# 获取当前环境配置
current_env = get_current_environment()
env_config = get_environment_config(current_env)

search = ElementPack(element_path=setting.UI_YAML_PATH)

class Astral(Page):
    def login(self, username, password):
        # 使用动态获取的环境配置
        base_url = env_config['host']
        login_path = "/zh-cn/login"

        # 确保URL格式正确
        if not base_url.endswith('/') and not login_path.startswith('/'):
            url = base_url + '/' + login_path
        else:
            url = base_url + login_path

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
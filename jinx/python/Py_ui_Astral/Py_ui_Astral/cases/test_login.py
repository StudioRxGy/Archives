from http import client
import unittest
from selenium.webdriver.common.by import By
from selenium.webdriver.support.wait import WebDriverWait
from selenium import webdriver
from time import sleep
from datetime import datetime
import os
from parameterized import parameterized
import json
from selenium.webdriver.chrome.service import Service
from webdriver_manager.chrome import ChromeDriverManager


def get_file(filename):

    filepath = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'data', f"{filename}.json")


    if not os.path.exists(filepath):
        print(f"File does not exist at path: {filepath}")
        return []

    try:

        with open(filepath, "r", encoding="utf-8") as file:
            data = json.load(file)
            
            test_data = [(item.get("user"), item.get("pwd"), item.get("expect"), item.get("notes")) for item in data]
            return test_data
            
    except FileNotFoundError as e:
        print(f"File not found error occurred while loading the file: {e}")
        return []
    except json.JSONDecodeError as e:
        print(f"JSON decode error occurred while loading the file: {e}")
        return []
    except Exception as e:
        print(f"An unexpected error occurred while loading the file: {e}")
        return []


class TestLogin(unittest.TestCase):
    driver = None

    @classmethod
    def setUpClass(cls) -> None:
        # 安装并启动 Chrome 驱动
        cls.driver = webdriver.Chrome(service=Service(ChromeDriverManager().install()))
        # 打开登录页面
        cls.driver.get('https://www.ast1001.com/login')
        cls.driver.maximize_window()
        cls.driver.implicitly_wait(3)

    @classmethod
    def tearDownClass(cls) -> None:
        # 关闭浏览器
        sleep(3)
        cls.driver.quit()

    def get_element_text(self, selector, timeout=3):
        try:
            # 查找元素并返回其文本
            element = WebDriverWait(self.driver, timeout).until(
                lambda driver: driver.find_element(By.CSS_SELECTOR, selector))
            return element.text
        except Exception:
            return None

    def save_screenshot(self, name="screenshot"):
        png_dir = os.path.join(current_dir, './report/png')
        if not os.path.exists(png_dir):
            os.makedirs(png_dir)
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        path = os.path.join(png_dir, f"{name}_{timestamp}.png")
        self.driver.get_screenshot_as_file(path)
        print(f"Screenshot saved at: {path}")
        return path

    @parameterized.expand(get_file('login'))
    def test_01(self, user, pwd, expect, notes):
        driver = self.driver
        sleep(2)
        driver.refresh()

        # 确保输入框为空再输入新的值
        username_input = driver.find_element(By.XPATH, "//*[contains(@class, 'jss42')]/div[2]/div[1]/span[1]/input[1]")
        password_input = driver.find_element(By.XPATH, "//*[contains(@class, 'ant-input-password')]/input[1]")

        username_input.clear()  # 清除用户名输入框
        username_input.send_keys(user)

        password_input.clear()  # 清除密码输入框
        password_input.send_keys(pwd)

        driver.find_element(By.XPATH, "//*[contains(@class, 'jss42')]/div[4]").click()

        text = self.get_element_text("#layui-layer1 > div") or \
               self.get_element_text("#member_name-error") or \
               self.get_element_text("#member_password-error")

        if text:
            self.assertIn(expect, text, notes)
        else:
            self.save_screenshot("test_failure")
            raise AssertionError("未找到匹配的提示信息")


if __name__ == '__main__':
    unittest.main()

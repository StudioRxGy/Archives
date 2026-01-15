# -*- coding: utf-8 -*-

import pytest
import csv
from pathlib import Path
from config import setting
from basepage.pages.Astralx.astral import Astral
from tools.element_check_tool import ElementPack

def load_csv_data(csv_path_setting: str):
    """通用CSV加载函数，接收setting中的路径配置项"""
    try:
        # 直接获取配置中的路径
        csv_path = Path(csv_path_setting)
        
        # 调试输出实际路径
        print(f"[DEBUG] 当前加载的CSV路径: {csv_path}")

        # 验证路径是否存在
        if not csv_path.exists():
            raise FileNotFoundError(
                f"CSV文件不存在于: {csv_path}\n"
                f"请检查setting.py中的配置项:\n"
                f"当前配置值: {csv_path_setting}"
            )

        # 读取CSV文件
        with open(csv_path, "r", encoding="utf-8") as f:
            return list(csv.DictReader(f))

    except Exception as e:
        print(f"[ERROR] 加载CSV失败: {str(e)}")
        raise

@pytest.mark.usefixtures("init_session")
class TestUI:
    @pytest.mark.parametrize(
        "data",
        load_csv_data(setting.UI_LOGIN_CSV_FILE),
        ids=lambda d: f"登录_{d['test_case']}"
    )
    def test_login(self, init_session, data):
        username = data["username"]
        password = data["password"]
        expected_result = "pass" \
            if data["descr"] == "登录成功" \
            else "fail"

        result = Astral(init_session).login(
            username=username,
            password=password
        )

        assert result == expected_result, f"登录验证失败: 用例 {data['test_case']}"
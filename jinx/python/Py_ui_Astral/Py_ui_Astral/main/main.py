import os
import unittest
from time import strftime
from tool.htmlTestReport.HTMLTestReportCN import HTMLTestRunner

current_dir = os.path.dirname(os.path.abspath(__file__))

cases_dir = os.path.join(current_dir, '../cases')

suite = unittest.TestLoader().discover(cases_dir, pattern="test_*.py")

report_dir = os.path.join(current_dir, '../report/html')
if not os.path.exists(report_dir):
    os.makedirs(report_dir)

report_path = os.path.join(report_dir, 'test_{}.html'.format(strftime('%Y_%m_%d %H_%M_%S')))

with open(report_path, 'wb') as f:
    HTMLTestRunner(
        stream=f,
        title="测试报告",
        description="所有测试用例",
        tester="吴振浩"
    ).run(suite)

print(f"测试报告已生成：{report_path}")

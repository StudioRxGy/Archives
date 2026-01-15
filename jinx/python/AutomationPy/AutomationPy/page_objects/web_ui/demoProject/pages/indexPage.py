from page_objects.web_ui.demoProject.elements.indexPageElements import IndexPageElements
from page_objects.web_ui.demoProject.pages.searchPage import SearchPage

class IndexPage:
    def __init__(self,browserOperator):
        self._browserOperator=browserOperator
        self._indexPageElements=IndexPageElements()
        self._browserOperator.explicit_wait_page_title(self._indexPageElements.title)
        self._browserOperator.get_screenshot('indexPage')

    def _input_search_kw(self,kw):
        # 增强输入方法：添加等待和备用方案
        try:
            # 等待搜索输入框可见并可交互
            self._browserOperator.waitForElementVisible(self._indexPageElements.search_input, 10)
            self._browserOperator.waitForElementClickable(self._indexPageElements.search_input, 10)
            
            # 使用改进后的sendText方法
            self._browserOperator.sendText(self._indexPageElements.search_input, kw)
            self._browserOperator.get_screenshot('input_search_kw')
            
        except Exception as e:
            print(f"输入搜索关键词失败: {e}")
            # 备用方案：使用JavaScript直接设置值
            element = self._browserOperator.findElement(self._indexPageElements.search_input)
            self._browserOperator.executeScript(f"arguments[0].value = '{kw}';", element)
            self._browserOperator.get_screenshot('input_search_kw_js_fallback')

    def _click_search_button(self):
        # 确保搜索按钮可点击
        self._browserOperator.waitForElementClickable(self._indexPageElements.search_button, 10)
        self._browserOperator.click(self._indexPageElements.search_button)
        self._browserOperator.get_screenshot('click_search_button')

    def search_kw(self, kw):
        self._input_search_kw(kw)
        self._click_search_button()
        if kw.strip():
            return SearchPage(self._browserOperator,kw+'_百度搜索')
        return IndexPage(self._browserOperator)

    def getElements(self):
        return self._indexPageElements
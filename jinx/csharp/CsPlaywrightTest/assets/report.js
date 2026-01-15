// 页面加载完成后初始化
document.addEventListener('DOMContentLoaded', function() {
    initializeNavigation();
    initializeFilters();
    initializeModals();
    initializeScrollSpy();
});

// 初始化导航
function initializeNavigation() {
    const navLinks = document.querySelectorAll('.nav-link');
    
    navLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            
            // 移除所有活动状态
            navLinks.forEach(l => l.classList.remove('active'));
            
            // 添加当前活动状态
            this.classList.add('active');
            
            // 滚动到目标部分
            const targetId = this.getAttribute('href').substring(1);
            const targetElement = document.getElementById(targetId);
            
            if (targetElement) {
                targetElement.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });
}

// 初始化过滤器
function initializeFilters() {
    const filterBtns = document.querySelectorAll('.filter-btn');
    const resultRows = document.querySelectorAll('.result-row');
    
    filterBtns.forEach(btn => {
        btn.addEventListener('click', function() {
            // 移除所有活动状态
            filterBtns.forEach(b => b.classList.remove('active'));
            
            // 添加当前活动状态
            this.classList.add('active');
            
            const filter = this.getAttribute('data-filter');
            
            // 过滤结果
            resultRows.forEach(row => {
                if (filter === 'all' || row.getAttribute('data-status') === filter) {
                    row.classList.remove('hidden');
                } else {
                    row.classList.add('hidden');
                }
            });
        });
    });
}

// 初始化模态框
function initializeModals() {
    const screenshotModal = document.getElementById('screenshotModal');
    const testDetailsModal = document.getElementById('testDetailsModal');
    
    // 关闭按钮事件
    document.querySelectorAll('.close').forEach(closeBtn => {
        closeBtn.addEventListener('click', function() {
            screenshotModal.style.display = 'none';
            testDetailsModal.style.display = 'none';
        });
    });
    
    // 点击模态框外部关闭
    window.addEventListener('click', function(e) {
        if (e.target === screenshotModal) {
            screenshotModal.style.display = 'none';
        }
        if (e.target === testDetailsModal) {
            testDetailsModal.style.display = 'none';
        }
    });
}

// 初始化滚动监听
function initializeScrollSpy() {
    const sections = document.querySelectorAll('.report-section');
    const navLinks = document.querySelectorAll('.nav-link');
    
    window.addEventListener('scroll', function() {
        let current = '';
        
        sections.forEach(section => {
            const sectionTop = section.offsetTop - 100;
            const sectionHeight = section.clientHeight;
            
            if (window.pageYOffset >= sectionTop && 
                window.pageYOffset < sectionTop + sectionHeight) {
                current = section.getAttribute('id');
            }
        });
        
        navLinks.forEach(link => {
            link.classList.remove('active');
            if (link.getAttribute('href') === '#' + current) {
                link.classList.add('active');
            }
        });
    });
}

// 打开截图模态框
function openScreenshot(imageSrc) {
    const modal = document.getElementById('screenshotModal');
    const modalImage = document.getElementById('modalImage');
    
    modalImage.src = imageSrc;
    modal.style.display = 'block';
}

// 显示测试详情
function showTestDetails(testName) {
    const modal = document.getElementById('testDetailsModal');
    const content = document.getElementById('testDetailsContent');
    
    if (typeof testDetailsData !== 'undefined' && testDetailsData[testName]) {
        const details = testDetailsData[testName];
        content.innerHTML = formatTestDetails(details);
    } else {
        content.innerHTML = '<h2>测试详情: ' + testName + '</h2><p>详细信息不可用</p>';
    }
    
    modal.style.display = 'block';
}

// 格式化测试详情
function formatTestDetails(details) {
    let html = '<h2>' + details.testName + '</h2>';
    html += '<div class="test-detail-grid">';
    html += '<div><strong>状态:</strong> ' + details.status + '</div>';
    html += '<div><strong>开始时间:</strong> ' + details.startTime + '</div>';
    html += '<div><strong>结束时间:</strong> ' + details.endTime + '</div>';
    html += '<div><strong>执行时间:</strong> ' + details.duration + '</div>';
    html += '</div>';
    
    if (details.errorMessage) {
        html += '<h3>错误信息</h3>';
        html += '<pre>' + details.errorMessage + '</pre>';
    }
    
    if (details.stackTrace) {
        html += '<h3>堆栈跟踪</h3>';
        html += '<pre>' + details.stackTrace + '</pre>';
    }
    
    return html;
}

// 格式化JSON数据
function formatJson(obj) {
    return JSON.stringify(obj, null, 2);
}

// Chart.js 配置
if (typeof Chart !== 'undefined') {
    Chart.defaults.font.family = 'Segoe UI, Tahoma, Geneva, Verdana, sans-serif';
    Chart.defaults.color = '#2c3e50';
}

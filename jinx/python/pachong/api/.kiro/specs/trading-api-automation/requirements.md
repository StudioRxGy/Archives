# Requirements Document

## Introduction

简单的Python自动化交易脚本，能够执行指定次数的买入和卖出订单到AST交易平台。

## Glossary

- **Trading_Script**: 自动化交易脚本
- **Order_Executor**: 订单执行器

## Requirements

### Requirement 1

**User Story:** 作为交易者，我希望能够选择执行买入订单的次数，以便批量执行交易。

#### Acceptance Criteria

1. WHEN 用户指定买入次数 THEN THE Trading_Script SHALL 执行相应次数的买入订单请求
2. WHEN 发送买入订单 THEN THE Trading_Script SHALL 使用提供的curl命令中的所有参数和请求头
3. WHEN 买入订单完成 THEN THE Trading_Script SHALL 显示每次请求的结果状态
4. THE Trading_Script SHALL 在每次请求之间添加适当的延迟

### Requirement 2

**User Story:** 作为交易者，我希望能够选择执行卖出订单的次数，以便批量执行交易。

#### Acceptance Criteria

1. WHEN 用户指定卖出次数 THEN THE Trading_Script SHALL 执行相应次数的卖出订单请求
2. WHEN 发送卖出订单 THEN THE Trading_Script SHALL 使用提供的curl命令中的所有参数和请求头
3. WHEN 卖出订单完成 THEN THE Trading_Script SHALL 显示每次请求的结果状态
4. THE Trading_Script SHALL 自动更新每次请求的client_order_id以避免重复

### Requirement 3

**User Story:** 作为用户，我希望脚本提供简单的命令行界面，以便容易使用。

#### Acceptance Criteria

1. WHEN 脚本启动 THEN THE Trading_Script SHALL 提示用户选择操作类型（买入或卖出）
2. WHEN 用户选择操作类型 THEN THE Trading_Script SHALL 提示用户输入执行次数
3. WHEN 用户输入无效数据 THEN THE Trading_Script SHALL 显示错误信息并重新提示
4. THE Trading_Script SHALL 在执行前显示操作摘要供用户确认
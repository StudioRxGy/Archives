-- 返佣
SELECT
  order_type,
  executed_token_fee,
  token_fee_token_id,
  agent_user_id,
  agent_level,
  agent_token_fee_rate_snapshot,
  agent_token_fee
FROM
  `gts_agent`.`gts_agent_user_order`
WHERE
  order_id = '2014700401976261376';

-- 返佣结算
SELECT
  agent_user_id,
  create_at,
  update_at,
  agent_level,
  token_id,
  token_fee_amount,
  status,
  real_settle_time
FROM
  gts_agent.gts_agent_settle_record
WHERE
  agent_user_id = '97838347'
  and agent_level = '100';

-- 当前代理下记录的交易记录数量
SELECT
  *
FROM
  gts_agent.gts_agent_user_order
WHERE
  agent_user_id = '97838855'
  AND order_type = '2'
  AND executed_token_fee > 0
GROUP BY
  order_id;

-- 用户信息
SELECT
  user_id,
  email,
  mobile,
  language
FROM
  `gts_server`.`gts_user`
WHERE user_id = '97832197'
  -- email = "fylv4@jinx.cc";

-- 本周注册人数
SELECT
  *
FROM
  `gts_server`.`gts_user`
WHERE
  created >= UNIX_TIMESTAMP(CURRENT_DATE - INTERVAL WEEKDAY(CURRENT_DATE) DAY) * 1000
  AND created < UNIX_TIMESTAMP(CURRENT_DATE - INTERVAL WEEKDAY(CURRENT_DATE) DAY + INTERVAL 7 DAY) * 1000;

-- 用户数量
SELECT
  COUNT(*)
FROM
  `gts_server`.`gts_user`;

-- 合约流水
SELECT
  *
FROM
  `bos_shard`.`bos_balance_flow`
WHERE
  `account_id` = '1955754041164187651'
  AND business_subject = '2'
  AND created_at >= DATE_SUB(CURRENT_DATE(), INTERVAL 30 DAY);

-- 汇率
SELECT
  *
FROM
  bos_clear.snap_bitc_rate
WHERE
  token_id = 'USD'
  AND dt = '2025-08-20';

-- 超级返佣上级关系
SELECT
  *
FROM
  `gts_activity`.`gts_referral_relation`
WHERE
  user_id = '97838817';

-- 超级返佣下级关系
SELECT
  *
FROM
  `gts_activity`.`gts_referral_relation`
WHERE
  referral_user_id = '97838285';

-- 代理用户订单信息
SELECT
  *
FROM
  gts_agent.gts_agent_user_order
WHERE
  order_status = '4'
  and order_type = 1;

-- 直属下级现货交易额统计信息
WITH
  sum1 AS (
    SELECT
      COALESCE(SUM(executed_amount), 0) AS s1
    FROM
      gts_agent.gts_agent_user_order
    WHERE
      agent_user_id = '97838258'
      AND order_status = '2'
      AND commission_flag = '1'
      AND order_type = '1'
      AND direct_flag = '1'
  ),
  sum2 AS (
    SELECT
      COALESCE(SUM(executed_amount), 0) AS s2
    FROM
      gts_agent.gts_agent_user_order
    WHERE
      agent_user_id = '97838258'
      AND order_status = '4'
      AND commission_flag = '1'
      AND order_type = '1'
      AND direct_flag = '1'
  )
SELECT
  s1 + s2 AS total_sum
FROM
  sum1,
  sum2;

-- 直属下级合约交易额统计信息
WITH
  sum1 AS (
    SELECT
      COALESCE(SUM(executed_amount_with_multiplier), 0) AS s1
    FROM
      gts_agent.gts_agent_user_order
    WHERE
      agent_user_id = '97838258'
      AND order_status = '2'
      AND commission_flag = '1'
      AND order_type = '2'
      AND direct_flag = '1'
  ),
  sum2 AS (
    SELECT
      COALESCE(SUM(executed_amount_with_multiplier), 0) AS s2
    FROM
      gts_agent.gts_agent_user_order
    WHERE
      agent_user_id = '97838258'
      AND order_status = '4'
      AND commission_flag = '1'
      AND order_type = '2'
      AND direct_flag = '1'
  )
SELECT
  s1 + s2 AS total_sum
FROM
  sum1,
  sum2;

-- 抽奖
INSERT INTO
  `gts_activity`.`gts_activity_lottery_registration_list` (
    `number`,
    `nick_name`,
    `uid`,
    `prize_state`,
    `gift`,
    `awards`,
    `get_gift_state`,
    `open_time`,
    `is_virtual_user`,
    `create_time`
  )
VALUES
  (4, 'flzxtest1@ast1001.com', 97838825, 1, NULL, - 1, 1, '2025-09-21 16:00:00', 2, '2025-09-21 16:00:00');

-- gts后台账号
INSERT INTO
  gts_admin.com_admin_user (
    username,
    default_language,
    email,
    org_id,
    org_name,
    saas_org_id,
    area_code,
    telephone,
    PASSWORD,
    STATUS,
    created_at,
    created_ip,
    deleted,
    `position`,
    account_type,
    real_name,
    bind_ga,
    ga_key,
    bind_phone
  )
VALUES
  (
    'root@5.com',
    'zh_CN',
    'root@5.com',
    10001,
    'j',
    1,
    '85',
    '777777',
    '930e32b4ec7a2d91f01392063aba0874',
    1,
    '2024-12-31 13:15:50.896',
    '127.0.0.1',
    0,
    '',
    1,
    'root@5.com',
    1,
    'JE47P3QQ43LTIET7',
    1
  );

-- 福利中心的个人参加的任务
SELECT
  *
FROM
  gts_activity.gts_welfare_task_hold
WHERE
  user_id = '97838856';

-- 验证码
SELECT
  *
FROM
  gts_security.gts_verify_code_log
WHERE
  receiver like '%fl%';

-- 抽奖人数
SELECT
  *
FROM
  gts_activity.gts_activity_lottery_registration_list
WHERE
  number = '1';

-- 内部转账手动清算
UPDATE `bos_clear`.`clear_point`
SET
  clear_point_time = CURRENT_TIMESTAMP
WHERE
  id = 1;

-- KYC
INSERT INTO
  `gts_server`.`gts_user_kyc_apply` (
    `id`,
    `user_id`,
    `org_id`,
    `kyc_level`,
    `data_secret`,
    `verify_status`,
    `verify_message`,
    `verify_reason_id`,
    `created`,
    `updated`,
    `basic_update`,
    `basic_kyc_update`,
    `basic_previa_nationality`,
    `basic_previa_country_code`,
    `basic_previa_first_name`,
    `basic_previa_last_name`,
    `basic_previa_document`,
    `basic_previa_id_number`,
    `basic_date_of_birth`,
    `basic_document_number`,
    `basic_expiry_date`,
    `basic_first_name`,
    `basic_last_name`,
    `basic_issuing_country`,
    `basic_issuing_date`,
    `basic_sub_type`,
    `basic_type`,
    `basic_score`,
    `basic_return_type`,
    `basic_id_back_href`,
    `basic_id_face_href`,
    `sites_update`,
    `sites_kyc_update`,
    `sites_previa_nationality`,
    `sites_previa_country_code`,
    `sites_previa_residential_address`,
    `sites_previa_postal_code`,
    `sites_previa_city`,
    `sites_previa_document`,
    `sites_previa_document_img`,
    `sites_returntype`,
    `sites_score`,
    `verify_reason_custom`,
    `basic_face_image_href`
  )
VALUES
  (
    2108914399024578590,
    97831001,
    10001,
    40,
    NULL,
    2,
    NULL,
    NULL,
    1766138187231,
    1766138408249,
    1766138408249,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    0,
    NULL,
    '2003-12-25',
    'Mock-6MB38H53ZG',
    '2026-12-10',
    'John',
    'Mock-Doe',
    'ALB',
    NULL,
    'ID_CARD',
    'ID_CARD',
    NULL,
    'approved',
    'plain/10001/97838662/1766138407904-97838662.jpeg',
    'plain/10001/97838662/1766138407737-97838662.jpeg',
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    '',
    'plain/10001/97838662/1766138408105-97838662.jpeg'
  );

-- 代理信息
SELECT
  *
FROM
  gts_agent.gts_agent_info
WHERE
  user_id = "97838039";

-- 代理数量
SELECT
  COUNT(*)
FROM
  gts_agent.gts_agent_info;

-- 代理层级
SELECT -- COUNT(*)
  *
FROM
  gts_agent.gts_agent_info
WHERE
  l1_agent_id = "1955253766115564800";

-- 代理下级
SELECT
  *
FROM
  gts_agent.gts_agent_user_relation
WHERE
  agent_user_id = "97838954"
  AND bind_version = 1;

-- BD下的代理
SELECT
  *
FROM
  gts_agent.gts_agent_info
WHERE -- bd_name = "jinxBD组长" OR
  bd_name = "BD组员1-jinx";

-- 验证码
SELECT
  *
FROM
  gts_security.gts_verify_code_log;

-- 代理绑定关系
SELECT
  COUNT(*)
FROM
  gts_agent_user_relation
WHERE
  agent_user_id = '97838292' AND bind_version = 1  AND user_type = '1'

-- 现货订单表
select
  *
from
  bos_server.bos_order
  WHERE updated_at BETWEEN '2026-01-21 00:00:00' AND '2026-01-21 23:59:59';
  -- 合约订单表
select
  *
from
  bos_future.bos_order
  WHERE updated_at BETWEEN '2026-01-21 00:00:00' AND '2026-01-21 23:59:59';
  -- 标准合约订单表
select
  *
from
  bos_st_future.bos_order
   WHERE updated_at BETWEEN '2026-01-21 00:00:00' AND '2026-01-21 23:59:59';


-- 标准合约
SELECT
  sum(token_fee) AS fee
FROM
  bos_st_future.bos_trade_detail
WHERE
  match_time BETWEEN '2026-01-26 00:00:00' AND '2026-01-26 23:59:59';

-- 现货
SELECT
  sum(
    CASE
      WHEN side = 1 THEN token_fee
      WHEN side = 0 THEN token_fee * price
      ELSE 0
    END
  )
FROM
  bos_server.bos_trade_detail
WHERE
  match_time BETWEEN '2026-01-23 00:00:00' AND '2026-01-23 23:59:59';

-- 永续合约
SELECT
  sum(token_fee)
FROM
  bos_future.bos_trade_detail
WHERE
  match_time BETWEEN '2026-01-23 00:00:00' AND '2026-01-23 23:59:59';

-- 当天交易的总手续费
SELECT
  (SELECT sum(token_fee)
   FROM bos_st_future.bos_trade_detail
   WHERE match_time BETWEEN '2026-01-23 00:00:00' AND '2026-01-23 23:59:59')
  +
  (SELECT sum(
    CASE
      WHEN side = 1 THEN token_fee
      WHEN side = 0 THEN token_fee * price
      ELSE 0
    END
  )
   FROM bos_server.bos_trade_detail
   WHERE match_time BETWEEN '2026-01-26 00:00:00' AND '2026-01-26 23:59:59')
  +
  (SELECT sum(token_fee)
   FROM bos_future.bos_trade_detail
   WHERE match_time BETWEEN '2026-01-26 00:00:00' AND '2026-01-26 23:59:59')
  AS total_fee;

SELECT
    sum(executed_token_fee)-sum(executed_token_fee * token_fee_token_rate)
FROM
    gts_agent.gts_agent_user_order
where
    update_at BETWEEN '2026-01-24 00:00:00' AND '2026-01-24 23:59:59'
  and agent_user_id = '97838669'
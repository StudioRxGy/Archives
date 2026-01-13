

# 数据库表说明

## 账号相关

### 用户表 

gts_security下的gts_user

gts_server 下的 gts_user

用户登录记录sql

```sql
SELECT * 
FROM `gts_server`.`gts_login_log`
WHERE `user_id` = '97832149'
```  
### 注册验证码
```sql
SELECT receiver,code
FROM `gts_security`.`gts_verify_code_log`
WHERE receiver = 'jinx@test.com'
```

### kyc认证信息记录表

gts_server 下的 gts_user_kyc_apply

### 用户等级数据

gts_server 下的 gts_user_level

### 用户审核表

gts_server 下的 gts_user_verify

### gts用户表

gts_admin下的com_admin_user

```sql
INSERT INTO gts_admin.com_admin_user
(id, username, default_language, email, org_id, org_name, saas_org_id, area_code, telephone, password, status, created_at, created_ip, deleted, `position`, account_type, real_name, bind_ga, ga_key, bind_phone)
VALUES(7260424709637120999, 'jinx@test.com', 'zh_CN', 'jinx@test.com', 10001, 'j', 1, '853', '777777777', 'd2fabab1d2feffcf95ee8e1240c86169', 1, '2024-12-31 13:15:50.896', '127.0.0.1', 0, '', 1, 'root', 1, 'JE47P3QQ43LTIET7', 1);
```

密码的加密方式：用户名密码的MD5加密BitCXTradingPlatform
# 部署和运维指南

## 概述

本文档提供了企业级自动化测试框架的完整部署和运维指南，包括 CI/CD 管道配置、Docker 容器化部署、Kubernetes 集群部署以及监控和维护最佳实践。

## 目录

1. [CI/CD 管道部署](#cicd-管道部署)
2. [Docker 容器化部署](#docker-容器化部署)
3. [Kubernetes 集群部署](#kubernetes-集群部署)
4. [监控和日志](#监控和日志)
5. [故障排除](#故障排除)
6. [维护和更新](#维护和更新)

## CI/CD 管道部署

### GitHub Actions 配置

#### 前置条件

1. **仓库设置**
   ```bash
   # 确保仓库包含以下文件
   .github/workflows/ci-cd.yml
   docker/Dockerfile
   docker/Dockerfile.test-runner
   k8s/*.yaml
   ```

2. **Secrets 配置**
   在 GitHub 仓库设置中配置以下 Secrets：
   ```
   DOCKER_USERNAME          # Docker Hub 用户名
   DOCKER_PASSWORD          # Docker Hub 密码
   KUBE_CONFIG             # Kubernetes 配置文件 (base64 编码)
   SONAR_TOKEN             # SonarCloud 令牌
   ```

3. **环境配置**
   ```yaml
   # 在仓库设置中创建环境
   environments:
     - test
     - staging  
     - production
   ```

#### 管道触发

```bash
# 自动触发
git push origin main          # 触发完整 CI/CD 管道
git push origin develop       # 触发 CI 管道

# 手动触发
gh workflow run ci-cd.yml -f environment=staging
```

#### 管道阶段说明

1. **代码质量检查**
   - 代码格式验证
   - 静态代码分析
   - 安全扫描

2. **自动化测试**
   - 单元测试
   - API 测试
   - UI 测试（多浏览器）

3. **构建和打包**
   - Docker 镜像构建
   - NuGet 包生成
   - 版本标记

4. **部署**
   - Kubernetes 部署
   - 冒烟测试
   - 部署验证

### Azure DevOps 配置

#### 前置条件

1. **服务连接**
   ```yaml
   # 创建以下服务连接
   - Docker Hub (Docker Registry)
   - Kubernetes Cluster
   - SonarCloud
   ```

2. **变量组**
   ```yaml
   # 创建变量组: automation-framework-vars
   variables:
     dockerRegistry: 'your-registry.azurecr.io'
     sonarCloudOrganization: 'your-org'
     sonarCloudProjectKey: 'automation-framework'
   ```

#### 管道执行

```bash
# 触发管道
az pipelines run --name "Enterprise Automation Framework CI/CD"

# 查看管道状态
az pipelines runs list --pipeline-name "Enterprise Automation Framework CI/CD"
```

## Docker 容器化部署

### 本地开发环境

#### 快速启动

```bash
# 克隆仓库
git clone <repository-url>
cd enterprise-automation-framework

# 构建和启动服务
docker-compose up -d

# 查看服务状态
docker-compose ps

# 查看日志
docker-compose logs -f automation-framework
```

#### 服务配置

```bash
# 启动特定服务组合
docker-compose --profile testing up -d        # 测试服务
docker-compose --profile ui-testing up -d     # UI 测试
docker-compose --profile api-testing up -d    # API 测试
docker-compose --profile reporting up -d      # 报告服务
docker-compose --profile monitoring up -d     # 监控服务
```

#### 测试执行

```bash
# 运行单元测试
docker-compose run --rm test-runner

# 运行 UI 测试 (Chromium)
docker-compose run --rm ui-test-chromium

# 运行 UI 测试 (Firefox)
docker-compose run --rm ui-test-firefox

# 运行 API 测试
docker-compose run --rm api-test-runner
```

### 生产环境部署

#### 环境配置

```bash
# 生产环境配置
cp docker/docker-compose.prod.yml docker-compose.override.yml

# 配置环境变量
cat > .env << EOF
COMPOSE_PROJECT_NAME=automation-framework-prod
DOCKER_REGISTRY=your-registry.com
IMAGE_TAG=latest
ENVIRONMENT=production
EOF
```

#### 部署步骤

```bash
# 1. 拉取最新镜像
docker-compose pull

# 2. 启动服务
docker-compose up -d

# 3. 验证部署
docker-compose exec automation-framework dotnet --info

# 4. 运行健康检查
curl http://localhost:8080/health
```

## Kubernetes 集群部署

### 集群准备

#### 前置条件

```bash
# 1. 确保 kubectl 已配置
kubectl cluster-info

# 2. 创建命名空间
kubectl apply -f k8s/namespace.yaml

# 3. 创建 Secrets
kubectl create secret docker-registry docker-registry-secret \
  --docker-server=your-registry.com \
  --docker-username=your-username \
  --docker-password=your-password \
  --namespace=automation-framework
```

#### 存储配置

```bash
# 创建持久卷声明
kubectl apply -f k8s/pvc.yaml

# 验证 PVC 状态
kubectl get pvc -n automation-framework
```

### 应用部署

#### 配置部署

```bash
# 1. 应用配置
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secret.yaml

# 2. 部署应用
export VERSION=$(date +%Y.%m.%d)-$(git rev-parse --short HEAD)
export DEPLOYMENT_TIMESTAMP=$(date -u +%Y-%m-%dT%H:%M:%SZ)

# 替换镜像标签
sed -i "s|IMAGE_TAG|${VERSION}|g" k8s/deployment.yaml
sed -i "s|\${VERSION}|${VERSION}|g" k8s/deployment.yaml
sed -i "s|\${DEPLOYMENT_TIMESTAMP}|${DEPLOYMENT_TIMESTAMP}|g" k8s/deployment.yaml

kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s/hpa.yaml
```

#### 部署验证

```bash
# 检查部署状态
kubectl get deployments -n automation-framework
kubectl get pods -n automation-framework
kubectl get services -n automation-framework

# 查看应用日志
kubectl logs -f deployment/automation-framework -n automation-framework

# 端口转发测试
kubectl port-forward service/automation-framework-service 8080:80 -n automation-framework
```

### 测试作业执行

#### UI 测试作业

```bash
# 创建 UI 测试作业
kubectl apply -f - <<EOF
apiVersion: batch/v1
kind: Job
metadata:
  name: ui-test-$(date +%s)
  namespace: automation-framework
spec:
  template:
    spec:
      containers:
      - name: ui-test-runner
        image: your-registry.com/enterprise-automation-test-runner:${VERSION}
        env:
        - name: TEST_CATEGORY
          value: "UI"
        - name: BROWSER
          value: "chromium"
      restartPolicy: Never
EOF

# 监控作业状态
kubectl get jobs -n automation-framework -w
```

#### API 测试作业

```bash
# 创建 API 测试作业
kubectl apply -f - <<EOF
apiVersion: batch/v1
kind: Job
metadata:
  name: api-test-$(date +%s)
  namespace: automation-framework
spec:
  template:
    spec:
      containers:
      - name: api-test-runner
        image: your-registry.com/enterprise-automation-test-runner:${VERSION}
        env:
        - name: TEST_CATEGORY
          value: "API"
      restartPolicy: Never
EOF
```

## 监控和日志

### 应用监控

#### Prometheus 配置

```yaml
# monitoring/prometheus.yml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'automation-framework'
    static_configs:
      - targets: ['automation-framework-service:8080']
    metrics_path: '/metrics'
    scrape_interval: 30s
```

#### Grafana 仪表板

```bash
# 导入预配置的仪表板
kubectl apply -f monitoring/grafana/dashboards/automation-framework-dashboard.json
```

### 日志管理

#### 集中日志收集

```bash
# 使用 Fluentd 收集日志
kubectl apply -f - <<EOF
apiVersion: apps/v1
kind: DaemonSet
metadata:
  name: fluentd
  namespace: automation-framework
spec:
  selector:
    matchLabels:
      name: fluentd
  template:
    metadata:
      labels:
        name: fluentd
    spec:
      containers:
      - name: fluentd
        image: fluent/fluentd-kubernetes-daemonset:v1-debian-elasticsearch
        env:
        - name: FLUENT_ELASTICSEARCH_HOST
          value: "elasticsearch.logging.svc.cluster.local"
        - name: FLUENT_ELASTICSEARCH_PORT
          value: "9200"
        volumeMounts:
        - name: varlog
          mountPath: /var/log
        - name: varlibdockercontainers
          mountPath: /var/lib/docker/containers
          readOnly: true
      volumes:
      - name: varlog
        hostPath:
          path: /var/log
      - name: varlibdockercontainers
        hostPath:
          path: /var/lib/docker/containers
EOF
```

#### 日志查询

```bash
# 查看应用日志
kubectl logs -f deployment/automation-framework -n automation-framework

# 查看测试执行日志
kubectl logs job/ui-test-job -n automation-framework

# 使用标签过滤日志
kubectl logs -l app=automation-framework -n automation-framework --tail=100
```

## 故障排除

### 常见问题

#### 1. 容器启动失败

```bash
# 检查容器状态
kubectl describe pod <pod-name> -n automation-framework

# 查看容器日志
kubectl logs <pod-name> -n automation-framework

# 进入容器调试
kubectl exec -it <pod-name> -n automation-framework -- /bin/bash
```

#### 2. 测试执行失败

```bash
# 检查测试配置
kubectl get configmap automation-framework-config -n automation-framework -o yaml

# 查看测试日志
kubectl logs job/<test-job-name> -n automation-framework

# 检查存储卷
kubectl get pvc -n automation-framework
kubectl describe pvc reports-pvc -n automation-framework
```

#### 3. 网络连接问题

```bash
# 检查服务状态
kubectl get svc -n automation-framework

# 测试服务连接
kubectl run debug --image=busybox -it --rm --restart=Never -- nslookup automation-framework-service.automation-framework.svc.cluster.local

# 检查网络策略
kubectl get networkpolicy -n automation-framework
```

### 性能调优

#### 资源优化

```yaml
# 调整资源限制
resources:
  requests:
    memory: "1Gi"
    cpu: "500m"
  limits:
    memory: "2Gi"
    cpu: "1000m"
```

#### 并发控制

```yaml
# 调整并行测试数量
spec:
  parallelism: 3
  completions: 3
```

## 维护和更新

### 定期维护任务

#### 1. 清理旧资源

```bash
# 清理完成的作业
kubectl delete jobs --field-selector status.successful=1 -n automation-framework

# 清理旧的 Pod
kubectl delete pods --field-selector status.phase=Succeeded -n automation-framework

# 清理旧的镜像
docker system prune -a
```

#### 2. 备份数据

```bash
# 备份测试报告
kubectl cp automation-framework/<pod-name>:/app/reports ./backup/reports-$(date +%Y%m%d)

# 备份配置
kubectl get configmap automation-framework-config -n automation-framework -o yaml > backup/config-$(date +%Y%m%d).yaml
```

#### 3. 更新应用

```bash
# 滚动更新
kubectl set image deployment/automation-framework automation-framework=your-registry.com/automation-framework:new-version -n automation-framework

# 监控更新状态
kubectl rollout status deployment/automation-framework -n automation-framework

# 回滚更新（如需要）
kubectl rollout undo deployment/automation-framework -n automation-framework
```

### 安全更新

#### 1. 镜像安全扫描

```bash
# 使用 Trivy 扫描镜像
trivy image your-registry.com/automation-framework:latest

# 使用 Clair 扫描
clairctl analyze your-registry.com/automation-framework:latest
```

#### 2. 依赖更新

```bash
# 更新 .NET 依赖
dotnet list package --outdated
dotnet add package <package-name> --version <new-version>

# 更新 Playwright
dotnet add package Microsoft.Playwright --version <latest-version>
```

### 监控告警

#### 配置告警规则

```yaml
# monitoring/alert_rules.yml
groups:
- name: automation-framework
  rules:
  - alert: HighTestFailureRate
    expr: (test_failures / test_total) > 0.1
    for: 5m
    labels:
      severity: warning
    annotations:
      summary: "High test failure rate detected"
      
  - alert: PodCrashLooping
    expr: rate(kube_pod_container_status_restarts_total[15m]) > 0
    for: 5m
    labels:
      severity: critical
    annotations:
      summary: "Pod is crash looping"
```

#### 通知配置

```yaml
# 配置 Slack 通知
route:
  group_by: ['alertname']
  group_wait: 10s
  group_interval: 10s
  repeat_interval: 1h
  receiver: 'slack-notifications'

receivers:
- name: 'slack-notifications'
  slack_configs:
  - api_url: 'YOUR_SLACK_WEBHOOK_URL'
    channel: '#automation-alerts'
    title: 'Automation Framework Alert'
    text: '{{ range .Alerts }}{{ .Annotations.summary }}{{ end }}'
```

## 最佳实践

### 1. 安全最佳实践

- 使用非 root 用户运行容器
- 定期更新基础镜像
- 扫描镜像漏洞
- 使用 Secrets 管理敏感信息
- 配置网络策略限制流量

### 2. 性能最佳实践

- 合理配置资源限制
- 使用 HPA 自动扩缩容
- 优化镜像大小
- 使用多阶段构建
- 配置适当的健康检查

### 3. 运维最佳实践

- 实施蓝绿部署
- 配置完整的监控和告警
- 定期备份重要数据
- 建立灾难恢复计划
- 维护详细的运维文档

## 联系支持

如需技术支持，请联系：
- 邮箱：automation-support@company.com
- 内部文档：https://wiki.company.com/automation-framework
- 问题跟踪：https://jira.company.com/projects/AUTO
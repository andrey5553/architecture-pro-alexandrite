# Jaeger в Minikube с сервисами

## Описание
Развертывание Jaeger в Minikube с двумя сервисами, которые:
1. Взаимодействуют между собой
2. Отправляют трейсы в Jaeger

## Требования
- Minikube
- kubectl
- Docker

## Установка

### 1. Запуск Minikube 
```bash
minikube start --addons=ingress 
```
Ingress нужен для вызовов

### 2. Установка cert-manager
```bash
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.3/cert-manager.yaml
```

### 3. Развертывание Jaeger
```bash
kubectl create namespace observability
kubectl create -f https://github.com/jaegertracing/jaeger-operator/releases/download/v1.51.0/jaeger-operator.yaml -n observability # надо подождать 3 минуты
kubectl apply -f k8s/jaeger-instance.yaml
```

done!

### 4. Сборка и деплой сервисов
```bash
# Сборка образов
./k8s/deploy.sh # порядка 5 минут идет билд и запуск, плюс настройка podes

# Проверяем состояние под (должны быть running)
kubectl get pods

# Развертывание
kubectl apply -f k8s/services.yaml
```

## Проверка работы

### Доступ к Jaeger UI
```bash
kubectl port-forward svc/simplest-query 16686:16686 > /dev/null 2>&1 & # дабы не блокировать консоль, логируем в пустоту
```
Откройте в браузере: http://localhost:16686

### Тестирование сервисов
```bash
# В одном терминале запустите проброс порта чтобы было проще работать с service-a
kubectl port-forward svc/service-a 8080:8080
# Вызов service-a, который вызывает service-b
http://localhost:8080/api/call-random
```
Далее идем http://localhost:16686/ и в сервисах выбираем ServiceA , смотрим трассировку
вот результат
[jaeger minikube](../Task3/png/главное%20окно%20jaeger%20и%20список%20трасс%20для%20k8s.png)
[вызов service-a и трассировка до service-b](../Task3/png/результат%20обращения%20serviceA%20к%20serviceB%20-%20k8s.png)

## Структура проекта
- `services/service-a/` - Исходный код service-a
- `services/service-b/` - Исходный код service-b  
- `k8s/deploy.sh` - Деплой сервисов
- `k8s/services.yaml` - Конфигурация Kubernetes для сервисов
- `jaeger-instance.yaml` - Конфигурация Jaeger

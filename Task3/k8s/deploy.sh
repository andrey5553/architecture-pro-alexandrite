#!/bin/bash

minikube status || minikube start

minikube addons enable jaeger

eval $(minikube docker-env)

echo "Building ServiceA image..."
cd service-a
docker build -t service-a:latest .
cd ..

echo "Building ServiceB image..."
cd service-b
docker build -t service-b:latest .
cd ..

echo "Deploying to Kubernetes..."
kubectl apply -f services.yaml

echo "Waiting for pods to be ready..."
kubectl wait --for=condition=ready pod -l app=service-a --timeout=60s
kubectl wait --for=condition=ready pod -l app=service-b --timeout=60s

echo "ServiceA URL: $(minikube service service-a --url)"
echo "Jaeger UI URL: $(minikube service jaeger-query --url)"

echo "Deployment complete!"

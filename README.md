# Distribuirani sistem za nadzor temperature

## Članovi tima
* Branislav Marković (Ingestion, Infrastruktura)
* Nikola Savić (SensorClient, ConsensusService)
* Uroš Milinović (Docker, Kubernetes, NotificationService, Sensor Dashboard)

## Uputstvo za pokretanje
1. Klonirajte repozitorijum.
2. Pokrenite bazu pomoću `docker-compose up -d`.
3. Running with Kubernetes 

##
1. Start Minikube.

```bash
minikube start
```

2. Load Docker images into Minikube.

```bash
minikube image load ingestion-service:latest
minikube image load consensus-service:latest
minikube image load notification-service:latest
minikube image load sensortelemetrysystem-sensor-1:latest
```

3. Deploy the application.

```bash
kubectl apply -f k8s/database-storage.yaml
kubectl apply -f k8s/db-deployment.yaml
kubectl apply -f k8s/services-deployment.yaml
kubectl apply -f k8s/sensor-client-deployment.yaml
kubectl apply -f k8s/ingress.yaml
```

4. Forward the required services.

```bash
kubectl port-forward service/ingestion-service 5128:5128
kubectl port-forward service/notification-service 5112:5112
```

5. Run the Sensor Dashboard.

```bash
dotnet run --project src/SensorDashboard
```

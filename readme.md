Outside:
Prometheus: http://localhost:9090
Grafana: http://localhost:3000
Jaeger: http://localhost:16686

In Grafana for sources I need to add host.docker.internal
Prometheus: http://host.docker.internal:9090
Jaeger: http://host.docker.internal:16686
Loki: http://host.docker.internal:3100

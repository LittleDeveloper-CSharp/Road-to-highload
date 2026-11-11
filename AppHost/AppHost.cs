using AppHost.OpenTelemetryCollector;

var builder = DistributedApplication.CreateBuilder(args);
var obs = Path.Combine(builder.AppHostDirectory, "observability");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("shop-api-db")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(); // UI к Postgres из Aspire Dashboard

var appDb = postgres.AddDatabase("appdb");

// --- Observability: Prometheus + Grafana + OTel Collector + postgres_exporter ---
var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "v3.2.1")
    .WithBindMount(Path.Combine(obs, "prometheus"), "/etc/prometheus", isReadOnly: true)
    .WithArgs("--web.enable-otlp-receiver", "--config.file=/etc/prometheus/prometheus.yml")
    .WithHttpEndpoint(targetPort: 9090, name: "http")
    .WithUrlForEndpoint("http", u => u.DisplayText = "Prometheus");

var grafana = builder.AddContainer("grafana", "grafana/grafana", "11.5.2")
    .WithBindMount(Path.Combine(obs, "grafana", "config"), "/etc/grafana", isReadOnly: true)
    .WithBindMount(Path.Combine(obs, "grafana", "dashboards"), "/var/lib/grafana/dashboards", isReadOnly: true)
    .WithEnvironment("PROMETHEUS_ENDPOINT", prometheus.GetEndpoint("http"))
    .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "http")
    .WithUrlForEndpoint("http", u => u.DisplayText = "Grafana")
    .WaitFor(prometheus);

// Метрики Postgres → Prometheus (scrape job `postgres` в prometheus.yml).
builder.AddContainer("postgres-exporter", "prometheuscommunity/postgres-exporter", "v0.17.1")
    .WithHttpEndpoint(targetPort: 9187, name: "http")
    .WithUrlForEndpoint("http", u =>
    {
        u.DisplayText = "postgres_exporter";
        u.DisplayLocation = UrlDisplayLocation.DetailsOnly;
    })
    .WithEnvironment("DATA_SOURCE_URI", ReferenceExpression.Create(
        $"{postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.HostAndPort)}/postgres?sslmode=disable"))
    .WithEnvironment("DATA_SOURCE_USER", postgres.Resource.UserNameReference)
    .WithEnvironment("DATA_SOURCE_PASS", postgres.Resource.PasswordParameter)
    .WaitFor(postgres);

builder.AddOpenTelemetryCollector("otel-collector", Path.Combine(obs, "otelcollector", "config.yaml"))
    .WithEnvironment("PROMETHEUS_ENDPOINT", $"{prometheus.GetEndpoint("http")}/api/v1/otlp")
    .WaitFor(prometheus);

builder.AddProject<Projects.Shop_Api>("shop-api")
    .WithReference(appDb)
    .WaitFor(appDb)
    .WithEnvironment("GRAFANA_URL", grafana.GetEndpoint("http"));

builder.Build().Run();

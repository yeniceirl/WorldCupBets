# Dokploy Observability

Use a standalone Aspire Dashboard in Dokploy for hobby-friendly logs, metrics, and traces. This is intentionally separate from CI/CD because the dashboard is infrastructure, not application code.

## Quick Path

1. Create a new Dokploy compose app for Aspire Dashboard.
2. Use `deploy/aspire-dashboard.compose.yml` as the compose template.
3. Put the backend and Aspire Dashboard on the same Dokploy/Docker network.
4. Configure the backend app with the OTLP environment variables below.
5. Redeploy Aspire Dashboard first, then redeploy the backend.

## Backend Variables

Set these in the backend Dokploy app:

| Variable | Value |
|----------|-------|
| `OTEL_SERVICE_NAME` | `WorldCupBets.WebApi` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://aspire-dashboard:18889` |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` |

The backend already enables the OTLP exporter when `OTEL_EXPORTER_OTLP_ENDPOINT` is configured.

## Connectivity

`http://aspire-dashboard:18889` only works when the backend container can resolve the `aspire-dashboard` service name. In Dokploy, that means both services must share a Docker network.

If Dokploy gives the dashboard a different internal hostname, use that hostname instead:

```text
OTEL_EXPORTER_OTLP_ENDPOINT=http://<dokploy-internal-dashboard-host>:18889
```

## Dashboard Service

The compose file runs:

| Setting | Value |
|---------|-------|
| Image | `mcr.microsoft.com/dotnet/aspire-dashboard:latest` |
| UI port | `18888` |
| OTLP/gRPC internal port | `18889` |
| OTLP/HTTP internal port | `18890` |

Expose only the UI through Dokploy. Keep OTLP ports internal so only app containers can send telemetry.

## Login

The dashboard is token-protected by default. Get the login token from the Aspire Dashboard container logs in Dokploy.

Do not set `ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true` on a public deployment.

## Limits

Aspire Dashboard standalone is a lightweight diagnostic tool:

- Telemetry is stored in memory.
- Data disappears when the dashboard restarts.
- It is great for hobby deploys and short-term debugging.
- It is not a replacement for production observability like Grafana, Prometheus, Tempo, Loki, or Application Insights.

That tradeoff is intentional for this project: useful visibility without overengineering.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace LoadTests.NBomber.Scenarios;

/// <summary>
/// Smoke: login seed user → create order → checkout against Shop.Api.
/// Requires LOADTEST_BASE_URL (e.g. http://localhost:XXXX from Aspire dashboard).
/// Falls back to httpbin GET only when LOADTEST_BASE_URL is unset (scaffold debug).
/// </summary>
public static class HttpSmokeScenario
{
    private static readonly Guid SeedProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string SeedEmail = "demo@local.test";
    private const string SeedPassword = "Demo123!";

    public static ScenarioProps Create()
    {
        var baseUrl = Environment.GetEnvironmentVariable("LOADTEST_BASE_URL");
        var rate = int.TryParse(Environment.GetEnvironmentVariable("LOADTEST_RATE"), out var r) ? r : 2;
        var durationSec = int.TryParse(Environment.GetEnvironmentVariable("LOADTEST_DURATION_SEC"), out var d) ? d : 15;

        if (string.IsNullOrWhiteSpace(baseUrl))
            return CreateHttpbinFallback(rate, durationSec);

        var root = baseUrl.TrimEnd('/');
        var email = Environment.GetEnvironmentVariable("LOADTEST_EMAIL") ?? SeedEmail;
        var password = Environment.GetEnvironmentVariable("LOADTEST_PASSWORD") ?? SeedPassword;
        var productId = Guid.TryParse(Environment.GetEnvironmentVariable("LOADTEST_PRODUCT_ID"), out var pid)
            ? pid
            : SeedProductId;

        var httpClient = Http.CreateDefaultClient();

        return Scenario.Create("checkout_smoke", async context =>
            {
                // 1) Login
                using var loginReq = new HttpRequestMessage(HttpMethod.Post, $"{root}/auth/login")
                {
                    Content = JsonContent.Create(new { email, password })
                };
                using var loginRes = await httpClient.SendAsync(loginReq);
                if (!loginRes.IsSuccessStatusCode)
                    return Response.Fail(statusCode: ((int)loginRes.StatusCode).ToString(), message: "login failed");

                await using var loginStream = await loginRes.Content.ReadAsStreamAsync();
                using var loginDoc = await JsonDocument.ParseAsync(loginStream);
                if (!loginDoc.RootElement.TryGetProperty("access_token", out var tokenEl))
                    return Response.Fail(statusCode: "login", message: "access_token missing");
                var token = tokenEl.GetString();
                if (string.IsNullOrEmpty(token))
                    return Response.Fail(statusCode: "login", message: "empty token");

                // 2) Create order
                using var orderReq = new HttpRequestMessage(HttpMethod.Post, $"{root}/orders")
                {
                    Content = JsonContent.Create(new { productId, quantity = 1 })
                };
                orderReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var orderRes = await httpClient.SendAsync(orderReq);
                if (!orderRes.IsSuccessStatusCode)
                    return Response.Fail(statusCode: ((int)orderRes.StatusCode).ToString(), message: "create order failed");

                await using var orderStream = await orderRes.Content.ReadAsStreamAsync();
                using var orderDoc = await JsonDocument.ParseAsync(orderStream);
                if (!orderDoc.RootElement.TryGetProperty("id", out var idEl))
                    return Response.Fail(statusCode: "order", message: "order id missing");
                var orderId = idEl.GetGuid();

                // 3) Checkout
                var checkoutRequest = Http.CreateRequest("POST", $"{root}/orders/{orderId}/checkout")
                    .WithHeader("Authorization", $"Bearer {token}");
                return await Http.Send(httpClient, checkoutRequest);
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(3))
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: rate,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromSeconds(durationSec)));
    }

    private static ScenarioProps CreateHttpbinFallback(int rate, int durationSec)
    {
        var httpClient = Http.CreateDefaultClient();
        var path = Environment.GetEnvironmentVariable("LOADTEST_PATH") ?? "/get";
        var url = $"https://httpbin.org{path}";

        return Scenario.Create("http_smoke_fallback", async _ =>
            {
                var request = Http.CreateRequest("GET", url);
                return await Http.Send(httpClient, request);
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(3))
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: rate,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromSeconds(durationSec)));
    }
}

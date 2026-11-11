using LoadTests.NBomber.Scenarios;
using NBomber.CSharp;
using NBomber.Http;

var scenario = HttpSmokeScenario.Create();

NBomberRunner
    .RegisterScenarios(scenario)
    .WithWorkerPlugins(new HttpMetricsPlugin([HttpVersion.Version1]))
    .Run();

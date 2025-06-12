using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Criar um único Meter com o nome "Contoso.Web"
var contosoMeter = new Meter("Contoso.Web");

// Registrar no DI o Meter e a classe de métricas
builder.Services.AddSingleton(contosoMeter);
builder.Services.AddSingleton<ContosoMetrics>();

// Configurar OpenTelemetry para coletar métricas desse Meter e expor Prometheus
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("Contoso.Web")
            .AddAspNetCoreInstrumentation()
            .AddPrometheusExporter();
    });

var app = builder.Build();

// Endpoint para expor métricas para o Prometheus
app.UseOpenTelemetryPrometheusScrapingEndpoint();

// Middleware para adicionar tags customizadas (exemplo utm_medium)
app.Use(async (context, next) =>
{
    var tagsFeature = context.Features.Get<IHttpMetricsTagsFeature>();
    if (tagsFeature != null)
    {
        var source = context.Request.Query["utm_medium"].ToString() switch
        {
            "" => "none",
            "social" => "social",
            "email" => "email",
            "organic" => "organic",
            _ => "other"
        };
        tagsFeature.Tags.Add(new KeyValuePair<string, object?>("mkt_medium", source));
    }
    await next.Invoke();
});

// Endpoint para registrar venda
app.MapPost("/complete-sale", (SaleModel model, ContosoMetrics metrics) =>
{
    metrics.ProductSold(model.ProductName, model.QuantitySold, model.Age, model.Location);
    return Results.Ok("Venda registrada!");
});

// Endpoint padrão
app.MapGet("/", () => "Hello World!");

app.Run();


// Modelo da venda
record SaleModel(string ProductName, int QuantitySold, int Age, string Location);


// Classe responsável pelas métricas
public class ContosoMetrics
{
    private readonly Counter<int> _productSoldCounter;
    private readonly Counter<int> _salesByLocationCounter;
    private readonly Dictionary<string, Counter<int>> _ageRangeCounters;

    private readonly string[] ageRanges = new string[]
    {
        "0-10",
        "11-20",
        "21-30",
        "31-40",
        "41-50",
        "51-60",
        "61-70",
        "71-80",
        "81+"
    };

    public ContosoMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Contoso.Web");
        _productSoldCounter = meter.CreateCounter<int>("contoso_product_sold_total");
        _salesByLocationCounter = meter.CreateCounter<int>("contoso_sales_by_location_total");

        _ageRangeCounters = new Dictionary<string, Counter<int>>();
        foreach(var range in ageRanges)
        {
            _ageRangeCounters[range] = meter.CreateCounter<int>($"contoso_user_age_{range.Replace("-", "_")}_total");
        }
    }

    public void ProductSold(string productName, int quantity, int age, string location)
    {
        _productSoldCounter.Add(quantity, new KeyValuePair<string, object?>("product_name", productName));
        _salesByLocationCounter.Add(quantity, new KeyValuePair<string, object?>("location", location));

        string ageRange = GetAgeRange(age);
        _ageRangeCounters[ageRange].Add(quantity, new KeyValuePair<string, object?>("product_name", productName));
    }

    private string GetAgeRange(int age)
    {
        if (age <= 10) return "0-10";
        if (age <= 20) return "11-20";
        if (age <= 30) return "21-30";
        if (age <= 40) return "31-40";
        if (age <= 50) return "41-50";
        if (age <= 60) return "51-60";
        if (age <= 70) return "61-70";
        if (age <= 80) return "71-80";
        return "81+";
    }
}

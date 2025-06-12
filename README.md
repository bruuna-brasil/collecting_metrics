Esta documentação tem como objetivo demonstrar o funcionamento da coleta e visualização de métricas em tempo real no projeto implementado com base no tutorial fornecido. O sistema permite registrar vendas via API, capturar métricas personalizadas e visualizar os dados no Grafana integrado ao Prometheus.

## 1. Visão Geral da Implementação

O projeto foi configurado utilizando **ASP.NET Core** com suporte a métricas via **OpenTelemetry**, exportando os dados para **Prometheus** e exibindo em um painel do **Grafana**. A rota `/complete-sale` recebe uma solicitação POST com informações de venda, registra a métrica personalizada `contoso.product.sold` e disponibiliza os dados para monitoramento.

### Estrutura da Requisição POST
```json
{
  "productName": "Calça",
  "quantitySold": 17,
  "age": 10,
  "location": "Avaré"
}
```

**URL de envio:**  
`http://localhost:5143/complete-sale`

## 2. Configuração do Projeto

As seguintes dependências foram adicionadas ao projeto:

- `OpenTelemetry.Exporter.Prometheus.AspNetCore`
- `OpenTelemetry.Extensions.Hosting`

Foi criada uma classe `ContosoMetrics` responsável por registrar as métricas personalizadas com base nas vendas realizadas.

### Registro da Classe no Program.cs
```csharp
builder.Services.AddSingleton<ContosoMetrics>();
```

### Uso na Rota `/complete-sale`
```csharp
// Endpoint para registrar venda
app.MapPost("/complete-sale", (SaleModel model, ContosoMetrics metrics) =>
{
    metrics.ProductSold(model.ProductName, model.QuantitySold, model.Age, model.Location);
    return Results.Ok("Venda registrada!");
});
```

## 3. Visualização das Métricas no Grafana

Para visualizar os dados no Grafana, seguimos as etapas abaixo:

1. O OpenTelemetry expõe o endpoint `/metrics` com os dados agregados.
2. O Prometheus faz o scraping desse endpoint a cada 5 segundos.
3. Os dados são armazenados e consultados pelo Grafana.
4. Um painel pré-configurado foi importado para visualização.

### Endpoint de Métricas Exposto
`http://localhost:5143/metrics`

Exemplo de saída:
```
# TYPE contoso_sales_by_location_total counter
contoso_sales_by_location_total{otel_scope_name="Contoso.Web",location="São Paulo"} 15 1749652605544
contoso_sales_by_location_total{otel_scope_name="Contoso.Web",location="Sorocaba"} 10 1749652605544
```

## 4. Demonstração dos Gráficos em Funcionamento

https://www.loom.com/share/02b5053b681447fdbeb715bdd4661594?sid=1e70d16a-68ab-480f-af90-d25b950b2ac0

## 5. Considerações Finais

O projeto foi implementado com sucesso e está apto para:

- Registrar vendas através de uma API REST;
- Coletar métricas personalizadas automaticamente;
- Exportar essas métricas para análise em tempo real;
- Visualizar os dados em um painel do Grafana integrado ao Prometheus.

Essa arquitetura pode ser estendida para incluir mais métricas, tags personalizadas e alertas automatizados, permitindo um monitoramento eficiente e proativo do sistema.

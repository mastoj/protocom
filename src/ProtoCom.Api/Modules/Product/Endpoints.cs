using System.Collections.Concurrent;
using Carter;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using Proto;
using Proto.Cluster;
using ProtoCom.Api.Modules.Cart;
using ProtoCom.Api.Modules.Product;

namespace Modules.Product;

public class Endpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/product", HandlePost);
        app.MapGet("/product/{productId}", HandleGet);
    }

    public async Task HandlePost(HttpResponse res, [FromServices] ActorSystem actorSystem, [FromBody] AddProductRequest body)
    {
        var grain = actorSystem
            .Cluster()
            .GetProductGrain(body.Product.Id);
        Console.WriteLine("==> Adding product: " + body.Product.Price);
        await grain.AddProduct(body, CancellationToken.None);
        res.StatusCode = 200;
    }

    public async Task<IResult> HandleGet(HttpResponse res, [FromServices] ActorSystem actorSystem, string productId)
    {
        var grain = actorSystem
            .Cluster()
            .GetProductGrain(productId);
        var response = await grain.GetProduct(new GetProductRequest() { ProductId = productId }, CancellationToken.None);

        if(response == null || response.ProductStatus == ProductStatus.MissingProduct)
        {
            return Results.NotFound();
        }
        return Results.Ok(response.Product);
    }
}

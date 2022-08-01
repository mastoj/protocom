using System.Collections.Concurrent;
using Carter;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using Proto;
using Proto.Cluster;
using ProtoCom.Api.Modules.Cart;
using ProtoCom.Api.Modules.Product;

namespace Modules.Product;

public class ProductGrain : ProductGrainBase
{

    private static ConcurrentDictionary<string, ProtoCom.Api.Modules.Cart.Product> _productDb = new();

    public ProductGrain(IContext context, ClusterIdentity clusterIdentity) : base(context)
    {
        Console.WriteLine("===> Started product grain: " + clusterIdentity.Identity);
        // Product = MissingProduct;
    }

    public override Task OnReceive()
    {
        switch(Context.Message) {
            case ReceiveTimeout _:
                Context.PoisonAsync(Context.Self);
                break;
            default: break;
        }
        return Task.CompletedTask;
    } 


    public override Task AddProduct(AddProductRequest request)
    {
        _productDb.AddOrUpdate(request.Product.Id, request.Product, (key, oldValue) => request.Product);
        return Task.CompletedTask;
    }

    public override Task OnStopped()
    {
        Console.WriteLine("==> Stopped: " + Context.ClusterIdentity()?.Identity);
        return base.OnStopped();
    }

    private static Random _random = new();
    public override Task<GetProductResponse> GetProduct(GetProductRequest request)
    {
        if(_random.NextInt64(100) > 70)
            throw new Exception("KILL");
            // Context.Stop(Context.Self);
        var product = _productDb!.GetValueOrDefault(request.ProductId, null);
        return Task.FromResult(new GetProductResponse() {
            Product = product,
            ProductStatus = product is null ? ProductStatus.MissingProduct : ProductStatus.Success
        });
    }
}



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

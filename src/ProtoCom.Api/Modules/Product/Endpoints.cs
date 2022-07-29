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

    public ProductGrain(IContext context) : base(context)
    {

        // Product = MissingProduct;
    }

    public ProtoCom.Api.Modules.Cart.Product Product { get; set; }

    public override Task AddProduct(AddProductRequest request)
    {
        Product = request.Product;
        return Task.CompletedTask;
    }

    public override Task<GetProductResponse> GetProduct(GetProductRequest request)
    {
        return Task.FromResult(new GetProductResponse() {
            Product = Product,
            ProductStatus = Product is null ? ProductStatus.MissingProduct : ProductStatus.Success
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

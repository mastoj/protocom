using System.Collections.Concurrent;
using Proto;
using Proto.Cluster;
using ProtoCom.Contracts;

public class ProductGrain : ProductGrainBase
{

    private static ConcurrentDictionary<string, Product> _productDb = 
        new(Enumerable.Range(0, 100)
            .ToDictionary(i => i.ToString(), i => new Product { Id = i.ToString(), Name = $"Product {i}", Price = 100 + i }));

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

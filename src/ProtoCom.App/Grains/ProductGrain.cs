using System.Collections.Concurrent;
using Marten;
using Proto;
using Proto.Cluster;
using ProtoCom.Contracts;

public class ProductGrain : ProductGrainBase
{
    private static Random _random = new();
    private IDocumentStore _documentStore;

    // private static ConcurrentDictionary<string, Product> _productDb = 
    //     new(Enumerable.Range(0, 100)
    //         .ToDictionary(i => i.ToString(), i => new Product { Id = i.ToString(), Name = $"Product {i}", Price = 100 + i }));
    private Product? _product;

    public ProductGrain(IContext context, ClusterIdentity clusterIdentity, IDocumentStore documentStore) : base(context)
    {
        Console.WriteLine("===> Started product grain: " + clusterIdentity.Identity);
        _documentStore = documentStore;
        // var session = documentStore.LightweightSession();
        // _product = session.Query<Product>().FirstOrDefault(p => p.Id == clusterIdentity.Identity);
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


    public override async Task AddProduct(AddProductRequest request)
    {
        // _productDb.AddOrUpdate(request.Product.Id, request.Product, (key, oldValue) => request.Product);
        using var session = _documentStore.LightweightSession();
        session.Store(request.Product);
        _product = request.Product;
        await session.SaveChangesAsync();
        // return Task.CompletedTask;
    }

    public override Task OnStopped()
    {
        Console.WriteLine("==> Stopped: " + Context.ClusterIdentity()?.Identity);
        return base.OnStopped();
    }

    public override Task<GetProductResponse> GetProduct(GetProductRequest request)
    {
        if(_random.NextInt64(100) > 70)
            throw new Exception("KILL");
            // Context.Stop(Context.Self);
        // var product = _productDb!.GetValueOrDefault(request.ProductId, null);
        // using var session = _documentStore.LightweightSession();
        // var product = session.Query<Product>().FirstOrDefault(p => p.Id == request.ProductId);
        if(_product is null) {
            var session = _documentStore.LightweightSession();
            _product = session.Query<Product>().FirstOrDefault(p => p.Id == request.ProductId);
        }
        return Task.FromResult(new GetProductResponse() {
            Product = _product,
            ProductStatus = _product is null ? ProductStatus.MissingProduct : ProductStatus.Success
        });
    }
}

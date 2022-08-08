
using Marten;
using Modules.Cart;
using Polly;
using Polly.Retry;
using Proto;
using Proto.Cluster;
using ProtoCom.Contracts;

public class CartGrain : CartGrainBase
{
    public Decider<CartEvent, CartCommand, CartState> Decider { get; private set; }

    private IDocumentStore _documentStore;

    public CartState State { get; private set; }

    public CartGrain(IContext context, Proto.Cluster.ClusterIdentity clusterIdentity, IDocumentStore documentStore) : base(context)
    {
        Decider = new CartService().CreateDecider();
        _documentStore = documentStore;
        using var session = _documentStore.OpenSession();
        Console.WriteLine("==> Getting data for: " + clusterIdentity.Identity);
        try {
            var eventData = session.Events.FetchStream(clusterIdentity.Identity);
            Console.WriteLine("==> Event data: " + eventData);
            var events = eventData.Select(e => e.Data).Cast<CartEvent>().ToList();
            State = events.Aggregate(Decider.InitialState(), Decider.Evolve);
            Console.WriteLine("==> Created actor: " + clusterIdentity.Identity);
        }
        catch (Exception e) {
            State = Decider.InitialState();
        }
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

    private static async Task<GetProductResponse> GetProduct(ProductGrainClient grain, string productId)
    {
        var policy = Policy.Handle<Exception>().RetryAsync(10, (ex, retryCount) =>
        {
            Console.WriteLine($"====> Retrying {retryCount}");
        });
        return await policy.ExecuteAsync(async () => await grain.GetProduct(new GetProductRequest() { ProductId = productId }, CancellationToken.None));
    }

    private async Task<Modules.Cart.Product> GetProduct(string productId)
    {
        var productGrain = Context
            .Cluster()
            .GetProductGrain(productId);
        
        var product =  await GetProduct(productGrain, productId);
        //  await productGrain.GetProduct(new GetProductRequest() {
        //     ProductId = productId
        // }, CancellationToken.None);


        if(product is null || product.ProductStatus == ProductStatus.MissingProduct)
        {
            throw new Exception("Product not found");
        }

        return new Modules.Cart.Product(product.Product.Id, product.Product.Name, product.Product.Price);
    }

    private static AsyncRetryPolicy _policy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
            5,
            retryAttempt => TimeSpan.FromSeconds(retryAttempt)
        );

    public override async Task<CartResponse> AddItem(AddItemRequest request)
    {
        var cartId = Guid.Parse(request.CartId);
        var quantity = request.Quantity;
        // var product = await _policy.ExecuteAsync(() => GetProduct(request.ProductId));
        var product = await GetProduct(request.ProductId);        
        // GetProduct(request.ProductId);
        var result = Decider.Decide(new AddCartItem(cartId, product, quantity), State);
        State = result.Aggregate(State, Decider.Evolve);

        var session = _documentStore.OpenSession();
        session.Events.Append(cartId, result!);
        session.SaveChanges();

        var cart = new Cart()
        {
            CartId = cartId.ToString(),
        };
        State.Items.ToList().ForEach(item =>
        {
            cart.Items.Add(
                item.Key,
                new CartItem()
                {
                    Product = new ProtoCom.Contracts.Product
                    {
                        Id = item.Value.Product.Id,
                        Name = item.Value.Product.Name,
                        Price = item.Value.Product.Price,
                    },
                    Quantity = item.Value.Quantity,
                }
            );
        });
        var response = new CartResponse()
        {
            Cart = cart
        };
        return response;
    }

    public override Task<CartResponse> ClearCart(ClearCartRequest request)
    {
        throw new NotImplementedException();
    }

    public override Task<CartResponse> GetCart(GetCartRequest request)
    {
        var cart = new Cart()
        {
            CartId = State.CartId.ToString(),
        };
        State.Items.ToList().ForEach(item =>
        {
            cart.Items.Add(
                item.Key,
                new CartItem()
                {
                    Product = new ProtoCom.Contracts.Product
                    {
                        Id = item.Value.Product.Id,
                        Name = item.Value.Product.Name,
                        Price = item.Value.Product.Price,
                    },
                    Quantity = item.Value.Quantity,
                }
            );
        });

        return Task.FromResult(new CartResponse() {
            Cart = cart
        });
    }

    public override Task<CartResponse> RemoveItem(RemoveItemRequest request)
    {
        throw new NotImplementedException();
    }
}


using Carter;
using Microsoft.AspNetCore.Mvc;
using Proto;
using Proto.Cluster;
using ProtoCom.Api.Modules.Cart;
using ProtoCom.Api.Modules.Product;

namespace Modules.Cart;

// public class CartActor : DeciderActor<CartEvent, CartCommand, CartState>
// {
//     public CartActor(Decider<CartEvent, CartCommand, CartState> decider) : base(decider)
//     {
//     }
// }

public class CartGrain : CartGrainBase
{
    public Decider<CartEvent, CartCommand, CartState> Decider { get; private set; }
    public CartState State { get; private set; }

    public CartGrain(IContext context, Proto.Cluster.ClusterIdentity clusterIdentity) : base(context)
    {
        Decider = new CartService().CreateDecider();
        State = Decider.InitialState();
        Console.WriteLine("==> Created actor: " + clusterIdentity.Identity);
    }

    private async Task<Product> GetProduct(string productId)
    {
        var productGrain = Context
            .Cluster()
            .GetProductGrain(productId);
        
        var product = await productGrain.GetProduct(new GetProductRequest() {
            ProductId = productId
        }, CancellationToken.None);


        if(product is null || product.ProductStatus == ProductStatus.MissingProduct)
        {
            throw new Exception("Product not found");
        }

        return new Product(product.Product.Id, product.Product.Name, product.Product.Price);
    }

    public override async Task<CartResponse> AddItem(AddItemRequest request)
    {
        var cartId = Guid.Parse(request.CartId);
        var quantity = request.Quantity;
        var product = await GetProduct(request.ProductId);
        var result = Decider.Decide(new AddCartItem(cartId, product, quantity), State);
        State = result.Aggregate(State, Decider.Evolve);
        var cart = new ProtoCom.Api.Modules.Cart.Cart()
        {
            CartId = cartId.ToString(),
        };
        State.Items.ToList().ForEach(item =>
        {
            cart.Items.Add(
                item.Key,
                new CartItem()
                {
                    Product = new ProtoCom.Api.Modules.Cart.Product{
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
        throw new NotImplementedException();
    }

    public override Task<CartResponse> RemoveItem(RemoveItemRequest request)
    {
        throw new NotImplementedException();
    }
}


public class Endpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/cart", HandleCreate);
    }

    public async Task HandleCreate([FromServices] ActorSystem actorSystem, HttpRequest req, HttpResponse res, [FromBody] AddItemRequest body)
    {
        var result = await actorSystem
            .Cluster()
            .GetCartGrain(body.CartId.ToString())
            .AddItem(body, CancellationToken.None);

        // var result = await context.RequestAsync<CartState>(cart, body);
        await res.WriteAsJsonAsync(result);
    }
}

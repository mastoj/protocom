using Carter;
using Microsoft.AspNetCore.Mvc;
using Proto;
using Proto.Cluster;
using ProtoCom.Api.Modules.Cart;

namespace Modules.Cart;

// public class CartActor : DeciderActor<CartEvent, CartCommand, CartState>
// {
//     public CartActor(Decider<CartEvent, CartCommand, CartState> decider) : base(decider)
//     {
//     }
// }

public class CartGrain : CartProcessBase
{
    public Decider<CartEvent, CartCommand, CartState> Decider { get; private set; }
    public CartState State { get; private set; }
    public ProductRepository ProductRepository { get; private set; }

    public CartGrain(IContext context, Proto.Cluster.ClusterIdentity clusterIdentity) : base(context)
    {
        Decider = new CartService().CreateDecider();
        State = Decider.InitialState();
        ProductRepository = new ProductRepository();
        Console.WriteLine("==> Created actor: " + clusterIdentity.Identity);
    }

    public override Task<CartResponse> AddItem(AddItemRequest request)
    {
        var cartId = Guid.Parse(request.CartId);
        var product = ProductRepository.GetProduct(request.ProductId);
        var quantity = request.Quantity;
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
        return Task.FromResult(response);
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
            .GetCartProcess(body.CartId.ToString())
            .AddItem(body, CancellationToken.None);

        // var result = await context.RequestAsync<CartState>(cart, body);
        await res.WriteAsJsonAsync(result);
    }
}

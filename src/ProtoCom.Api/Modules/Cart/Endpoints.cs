using Carter;
using Microsoft.AspNetCore.Mvc;
using Proto;

namespace Modules.Cart;

public class CartActor : DeciderActor<CartEvent, CartCommand, CartState>
{
    public CartActor(Decider<CartEvent, CartCommand, CartState> decider) : base(decider)
    {
    }
}

public class Endpoints : ICarterModule
{
    private static Dictionary<string, PID> Carts = new Dictionary<string, PID>();
    private Props actorProps = Props.FromProducer(() => new CartActor(new CartService().CreateDecider()));
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/cart", HandleCreate);
    }

    public async Task HandleCreate([FromServices]IRootContext context, HttpRequest req, HttpResponse res, [FromBody] AddCartItem body)
    {
        if(!Carts.ContainsKey(body.CartId.ToString()))
        {
            Carts.Add(body.CartId.ToString(), context.SpawnNamed(actorProps, body.CartId.ToString()));
        }
        var cart = Carts[body.CartId.ToString()];
        var result = await context.RequestAsync<CartState>(cart, body);
        await res.WriteAsJsonAsync(result);
    }
}

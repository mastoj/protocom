using Carter;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.Retry;
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

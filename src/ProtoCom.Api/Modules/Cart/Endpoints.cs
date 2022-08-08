using Carter;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.Retry;
using Proto;
using Proto.Cluster;
using ProtoCom.Contracts;

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
        app.MapGet("/cart/{cartId}", HandleGet);
    }

    public async Task HandleCreate([FromServices] ActorSystem actorSystem, HttpRequest req, HttpResponse res, [FromBody] AddItemRequest body)
    {
        var policy = Policy.Handle<Exception>().WaitAndRetryAsync(10, (count) =>
        {
            Console.WriteLine($"====> Retrying {count}");
            return new TimeSpan(0, 0, 0, 0, count*100);
        });

        var result = await policy.ExecuteAsync(async () => {
            var result = await actorSystem
                .Cluster()
                .GetCartGrain(body.CartId.ToString())
                .AddItem(body, CancellationToken.None);
            return result;
        });


        // var result = await context.RequestAsync<CartState>(cart, body);
        await res.WriteAsJsonAsync(result);
    }
    public async Task HandleGet([FromServices] ActorSystem actorSystem, HttpRequest req, HttpResponse res, [FromRoute] string cartId)
    {
        var policy = Policy.Handle<Exception>().WaitAndRetryAsync(10, (count) =>
        {
            Console.WriteLine($"====> Retrying {count}");
            return new TimeSpan(0, 0, 0, 0, count*100);
        });

        var result = await policy.ExecuteAsync(async () => {
            var result = await actorSystem
                .Cluster()
                .GetCartGrain(cartId.ToString())
                .GetCart(new GetCartRequest() { CartId = cartId }, CancellationToken.None);
            return result;
        });


        // var result = await context.RequestAsync<CartState>(cart, body);
        await res.WriteAsJsonAsync(result);
    }
}

using Carter;
using Microsoft.AspNetCore.Mvc;

namespace Modules.Cart;

public class Endpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/cart", HandleCreate);
    }

    public void HandleCreate(HttpRequest req, HttpResponse res, [FromBody] AddCartItem body)
    {
        var result = new CartService().Decide(CartService.InitialState(), body);
        res.WriteAsJsonAsync(result);
    }
}
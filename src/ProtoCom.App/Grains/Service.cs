using System;
using System.Text.Json.Serialization;
using Proto;

namespace Modules.Cart;

// DTOs
public record Product(string Id, string Name, double Price);

// Dependencies
public class ProductRepository
{    
    private Random Random = new Random();
    public Product GetProduct(string id)
    {
        return new Product(id, "Product " + id, Random.NextDouble()*2000 + 10);
    }
}

// Events
[JsonDerivedType(typeof(CartCreated), "cartcreated")]
[JsonDerivedType(typeof(CartItemAdded), "cartitemadded")]
[JsonDerivedType(typeof(CartItemRemoved), "cartitemremoved")]
public abstract record CartEvent(Guid CartId);
public record CartCreated(Guid CartId) : CartEvent(CartId);
public record CartItemAdded(Guid CartId, Product Product, int Quantity) : CartEvent(CartId);
public record CartItemRemoved(Guid CartId, string ProductId, int Quantity) : CartEvent(CartId);

// Commands
public abstract record CartCommand(Guid CartId);
public record AddCartItem(Guid CartId, Product Product, int Quantity) : CartCommand(CartId);
public record RemoveCartItem(Guid CartId, string ProductId, int Quantity) : CartCommand(CartId);

// Service

public interface Service<TEvent, TCommand, TState>
{
    abstract Decider<TEvent, TCommand, TState> CreateDecider();
}

public record Decider<TEvent, TCommand, TState>(
    Func<TCommand, TState, IEnumerable<TEvent>> Decide,
    Func<TState, TEvent, TState> Evolve,
    Func<TState> InitialState
);

// State
public record CartState(
    Guid CartId,
    Dictionary<string, (Product Product, int Quantity)> Items
);

// Service
public class CartService : Service<CartEvent, CartCommand, CartState>
{
    private readonly ProductRepository ProductRepository = new ProductRepository();

    public static CartState InitialState() => new CartState(Guid.Empty, new Dictionary<string, (Product Product, int Quantity)>());
    public CartState Evolve(CartState state, CartEvent @event)
    {
        switch (@event)
        {
            case CartCreated created: return Evolve(state, created);
            case CartItemAdded added: return Evolve(state, added);
            case CartItemRemoved removed: return Evolve(state, removed);
            default:
                throw new NotImplementedException();
        }
    }
    public static CartState Evolve(CartState state, CartCreated @event)
    {
        return InitialState() with
        {
            CartId = @event.CartId
        };
    }
    public static CartState Evolve(CartState state, CartItemAdded @event)
    {
        var items = state.Items.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        items[@event.Product.Id] = (@event.Product, @event.Quantity);
        return new CartState(state.CartId, items);
    }
    public static CartState Evolve(CartState state, CartItemRemoved @event)
    {
        var items = state.Items.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        items[@event.ProductId] = (items[@event.ProductId].Product, items[@event.ProductId].Quantity - @event.Quantity);
        return new CartState(state.CartId, items);
    }

    public IEnumerable<CartEvent> Decide(CartCommand command, CartState state)
    {
        switch (command)
        {
            case AddCartItem add: return Decide(state, add, ProductRepository);
            case RemoveCartItem remove: return Decide(state, remove);
            default:
                throw new NotImplementedException();
        }
    }
    public static IEnumerable<CartEvent> Decide(CartState state, AddCartItem command, ProductRepository productRepository)
    {
        if (state.CartId != Guid.Empty && state.CartId != command.CartId)
        {
            throw new InvalidOperationException("Cart already exists");
        }
        var product = command.Product;
        yield return new CartCreated(command.CartId);
        yield return new CartItemAdded(command.CartId, product, command.Quantity);
    }
    public static IEnumerable<CartEvent> Decide(CartState state, RemoveCartItem command)
    {
        if (state.CartId != command.CartId)
        {
            throw new InvalidOperationException("Cart does not exist");
        }
        yield return new CartItemRemoved(command.CartId, command.ProductId, command.Quantity);
    }

    public Decider<CartEvent, CartCommand, CartState> CreateDecider()
    {
        return new Decider<CartEvent, CartCommand, CartState>(
            Decide,
            Evolve,
            InitialState
        );
    }
}

public class DeciderActor<TEvent, TCommand, TState> : IActor
    where TState : notnull
{
    public Decider<TEvent, TCommand, TState> Decider { get; }
    public TState State { get; private set; }

    public DeciderActor(Decider<TEvent, TCommand, TState> decider)
    {
        Decider = decider ?? throw new ArgumentNullException(nameof(decider));
        State = decider.InitialState() ?? throw new ArgumentNullException(nameof(decider.InitialState));
    }

    public static DeciderActor<TEvent, TCommand, TState> Create(Decider<TEvent, TCommand, TState> decider)
    {
        return new DeciderActor<TEvent, TCommand, TState>(decider);
    }

    public Task ReceiveAsync(IContext context)
    {
        switch (context.Message)
        {
            case Started msg: 
                Console.WriteLine("Decider started");
                break;
            case TCommand command:
                var events = Decider.Decide(command, State);
                State = events.Aggregate(State, Decider.Evolve);
                context.Respond(State);
                break;
        }
        return Task.CompletedTask;
    }
}
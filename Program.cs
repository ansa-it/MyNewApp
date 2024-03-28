using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<MyNewAppDbContext>(options => 
    options.UseSqlite(connectionString));

// builder.Services.AddSingleton<ITaskService>(new InMemoryTaskService());

builder.Services.AddScoped<ITaskService, InDatabaseTaskService>();

var app = builder.Build();

app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

app.Use(async (context, next) => {
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path}] {DateTime.UtcNow} Started.");
    await next(context);
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path}] {DateTime.UtcNow} Finished.");
}); 


var todos = new List<Todo>();

app.MapDelete("/todos/{id}", (int id, ITaskService service) => 
{
    service.DeleteTodoById(id);
    return TypedResults.NoContent();
});

app.MapGet("/todos", (ITaskService service) => service.GetTodos());

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id, ITaskService service) =>
{
    var targetTodo = service.GetTodoById(id);
    return targetTodo is null
        ? TypedResults.NotFound()
        : TypedResults.Ok(targetTodo);
});

app.MapPost("/todos", (Todo task, ITaskService service) => 
{
    service.AddTodo(task);
    return TypedResults.Created("/todos/{id}", task);
})
.AddEndpointFilter(async (context, next) => {
    var taskArgument = context.GetArgument<Todo>(0);
    var errors = new Dictionary<string, string[]>();
    if( taskArgument.DueData < DateTime.UtcNow )
    {
        errors.Add(nameof(Todo.DueData), ["Cannot have due date in the past"]);
    }
    if( taskArgument.IsCompleted )
    {
        errors.Add(nameof(Todo.IsCompleted), ["Cannot add completed todo"]);
    }
    if(errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }
    return await next(context);
});

app.Run();

public class MyNewAppDbContext : DbContext
{
    public MyNewAppDbContext(DbContextOptions<MyNewAppDbContext> options) : base(options)
    {
    }

    public DbSet<Todo> Todos { get; set; }
}


public record Todo(int Id, string Name, DateTime DueData, bool IsCompleted);

interface ITaskService
{
    Todo? GetTodoById(int id);
    List<Todo> GetTodos();
    void DeleteTodoById(int id);
    Todo AddTodo(Todo task);
}

class InMemoryTaskService : ITaskService
{
    private readonly List<Todo> _todos = [];
    public Todo AddTodo(Todo task)
    {
        _todos.Add(task);
        return task;
    }

    public void DeleteTodoById(int id)
    {
        _todos.RemoveAll(task => id == task.Id);
    }

    public Todo? GetTodoById(int id)
    {
        return _todos.SingleOrDefault(t => id == t.Id);
    }

    public List<Todo> GetTodos() {
        return _todos;
    }
}

class InDatabaseTaskService : ITaskService
{
    private readonly MyNewAppDbContext _dbContext;

    public InDatabaseTaskService(MyNewAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Todo AddTodo(Todo task)
    {
        _dbContext.Todos.Add(task);
        _dbContext.SaveChanges();
       return task; 
    }

    public void DeleteTodoById(int id)
    {
        var entity = _dbContext.Todos.Find(id);
        if( entity != null )
        {
            _dbContext.Todos.Remove(entity);
            _dbContext.SaveChanges();
        }
    }
    public Todo? GetTodoById(int id)
    {
        return _dbContext.Todos.Find(id);
    }

    public List<Todo> GetTodos() {
        return _dbContext.Todos.ToList();
    }
}
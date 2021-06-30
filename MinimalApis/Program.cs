using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TodoContext>(options => options.UseInMemoryDatabase("TodoItems"));
await using var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapGet("/", (Func<string>)(() => "Hello World!"));

app.MapGet("/todoitems", async (http) =>
{
    var db = http.RequestServices.GetService<TodoContext>();
    var todoItems = await db.TodoItems.ToListAsync();

    await http.Response.WriteAsJsonAsync(todoItems);
});

app.MapGet("/todoitems/{id}", async (http) =>
{
    if (!http.Request.RouteValues.TryGetValue("id", out var id))
    {
        http.Response.StatusCode = 400;
        return;
    }

    var db = http.RequestServices.GetService<TodoContext>();
    var todoItem = await db.TodoItems.FindAsync(int.Parse(id.ToString()));
    if (todoItem == null)
    {
        http.Response.StatusCode = 404;
        return;
    }

    await http.Response.WriteAsJsonAsync(todoItem);
});

app.MapPost("/todoitems", async (http) =>
{
    var todoItem = await http.Request.ReadFromJsonAsync<TodoItem>();
    var db = http.RequestServices.GetService<TodoContext>();
    db.TodoItems.Add(todoItem);
    await db.SaveChangesAsync();
    http.Response.StatusCode = 204;
});

app.MapPut("/todoitems/{id}", async (http) =>
{
    if (!http.Request.RouteValues.TryGetValue("id", out var id))
    {
        http.Response.StatusCode = 400;
        return;
    }

    var db = http.RequestServices.GetService<TodoContext>();
    var todoItem = await db.TodoItems.FindAsync(int.Parse(id.ToString()));
    if (todoItem == null)
    {
        http.Response.StatusCode = 404;
        return;
    }

    var inputTodoItem = await http.Request.ReadFromJsonAsync<TodoItem>();
    todoItem.IsCompleted = inputTodoItem.IsCompleted;
    await db.SaveChangesAsync();
    http.Response.StatusCode = 204;
});

app.MapDelete("/todoitems/{id}", async (http) =>
{
    if (!http.Request.RouteValues.TryGetValue("id", out var id))
    {
        http.Response.StatusCode = 400;
        return;
    }

    var db = http.RequestServices.GetService<TodoContext>();
    var todoItem = await db.TodoItems.FindAsync(int.Parse(id.ToString()));
    if (todoItem == null)
    {
        http.Response.StatusCode = 404;
        return;
    }

    db.TodoItems.Remove(todoItem);
    await db.SaveChangesAsync();

    http.Response.StatusCode = 204;
});

await app.RunAsync();

public class TodoContext : DbContext
{
    public TodoContext(DbContextOptions options) : base(options) {}

    protected TodoContext() {}

    public DbSet<TodoItem> TodoItems { get; set; }
}

public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; }
    public bool IsCompleted { get; set; }
}

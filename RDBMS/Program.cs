
using RDBMS.Engine;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// 1. Register our Database Engine as a Singleton
var db = new Database();
builder.Services.AddSingleton(db);

var app = builder.Build();

// 2. Start the REPL in the background (Interactive Mode)
new Repl(db).Start();

// 3. Web API Endpoints (The Trivial Web App)

app.MapGet("/", () => "Welcome to SharpBase Web Interface. Try GET /users or POST /query");

// API: Get all users
app.MapGet("/users", (Database db) => 
{
    // Using the engine's internal methods directly
    return db.Tables["users"].SelectAll().Select(r => r.Data);
});

// API: Run raw SQL via Web
app.MapPost("/query", ([FromBody] string sql, Database db) => 
{
    return db.ExecuteSql(sql);
});

// Seed data if empty (For demo purposes)
if (db.Tables["users"].SelectAll().Count == 0)
{
    db.ExecuteSql("INSERT INTO users VALUES (101, \"John Doe\", 25)");
    db.ExecuteSql("INSERT INTO users VALUES (102, \"Jane Smith\", 30)");
    db.ExecuteSql("INSERT INTO orders VALUES (501, 101, \"Laptop\")"); // User 101 bought Laptop
}

app.Run();
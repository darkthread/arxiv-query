using System.Text.Json;
using PaperSearchService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SKAgent>();
builder.Services.AddDbContext<MyDbContext>();

var app = builder.Build();

app.UseFileServer();
app.UseDefaultFiles();

//app.MapGet("/", () => "Hello World!");

app.MapGet("/search", (MyDbContext db, string keywd, string st, string ed) =>
{
    var papers = db.Search(keywd, st, ed);
    return JsonSerializer.Serialize(papers);
});

app.MapGet("/translate", async (MyDbContext db, SKAgent agent, string id) =>
{
    var paper = await db.PaperMetadata.FindAsync(id);
    var english = paper.Abstract;
    var chinese = await agent.Translate(english);
    paper.AbstractCht = chinese;
    db.SaveChanges();
    return chinese;
});


app.Run();

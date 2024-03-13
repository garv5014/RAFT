using Raft_Node;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

//var raftNode = new RaftNodeService(new List<RaftNodeService>(), 1); // Adjust parameters as necessary
//builder.Services.AddSingleton(raftNode); // Register RaftNode
//builder.Services.AddHostedService<RaftNodeService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
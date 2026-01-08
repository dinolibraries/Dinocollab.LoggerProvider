using Dinocollab.LoggerProvider.QuestDB;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddQuestDBLoggerProvider(option =>
{
    option.ConnectionString = "tcp::addr=localhost:9009;";
    //option.ConnectionString = "http::addr=localhost:9000;";
    option.ApiUrl = "http://localhost:9000";
    option.TableLogName = "berlintomek";
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseQuestDBLoggerProvider();
app.Run();

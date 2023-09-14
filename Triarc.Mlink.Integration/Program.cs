using RabbitMQ.Client;
using Triarc.Mlink.Api.Hosting;
using Triarc.Mlink.V1.HierarchyEmployee2015_00.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var mlinkUrl = builder.Configuration.GetValue<string>("MlinkUrl");
var mlinkToken = builder.Configuration.GetValue<string>("MlinkToken");

builder.Services.AddMlinkApiClient(mlinkUrl, mlinkToken);
builder.Services.AddTransient<IHierarchyEmployeeApi, HierarchyEmployeeApi>();
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();
builder.Services.AddSingleton<IConnection>(sp =>
{
  var connectionFactory = new ConnectionFactory();
  builder.Configuration.Bind("RabbitMq", connectionFactory);
  connectionFactory.DispatchConsumersAsync = true;
  connectionFactory.AutomaticRecoveryEnabled = true;
  connectionFactory.TopologyRecoveryEnabled = true;
  connectionFactory.ClientProperties = new Dictionary<string, object>
  {
    {
      "clientProvidedName",
      "mlink-integrator-name"
    },
  };
  return connectionFactory.CreateConnection();
});
builder.Services.AddHostedService<IRabbitMqService>(provider => provider.GetService<IRabbitMqService>());
builder.Services.AddTransient<IHierarchyEmployeeImporter, HierarchyEmployeeImporter>();
builder.Services.AddHealthChecks().AddRabbitMQ();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseHealthChecks("/health");
app.UseAuthorization();

app.MapControllers();

app.Run();

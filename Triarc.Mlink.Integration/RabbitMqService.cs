using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Triarc.Mlink.V1.HierarchyEmployee2015_00.Model;

public class RabbitMqService : BackgroundService, IRabbitMqService
{
  private readonly IConfiguration _configuration;
  private readonly ILogger<RabbitMqService> _logger;
  private readonly IServiceProvider _serviceProvider;
  private IConnection _connection;

  public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger,
    IConnection connection,
    IServiceProvider serviceProvider)
  {
    _configuration = configuration;
    _logger = logger;
    _connection = connection;
    _serviceProvider = serviceProvider;
  }

  public IConnection Connection => _connection;

  protected override Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var model = Connection.CreateModel();
    // this exchange is created by mlink 
    var exchangeName = "hierarchyEmployee-2015_00";
    var queueName = "integrator-employee";
    model.QueueDeclare(queueName, true, false);
    model.QueueBind(queueName, exchangeName, "");
    var asyncDefaultBasicConsumer = new AsyncEventingBasicConsumer(model);
    asyncDefaultBasicConsumer.Received += (sender, @event) =>
      OnEmployeeReceived(model, @event);
    model.BasicQos(0, 1, false);
    model.BasicConsume(
      queueName,
      false,
      Environment.MachineName,
      false,
      false,
      null,
      asyncDefaultBasicConsumer);
    return Task.CompletedTask;
  }

  private async Task OnEmployeeReceived(IModel model, BasicDeliverEventArgs @event)
  {
    var tag = @event.DeliveryTag;
    DocumentUpdate<HierarchyEmployeeAbaDefaultType> dto;
    try
    {
      var jsonStr = Encoding.UTF8.GetString(@event.Body.ToArray());
      dto = JsonConvert.DeserializeObject<DocumentUpdate<HierarchyEmployeeAbaDefaultType>>(jsonStr);
    }
    catch (Exception e)
    {
      _logger.LogError(e, "Failed to deserialize message.");
      Nack(model, tag, false);
      return;
    }

    try
    {
      // create independant scope for each employee
      using var scope = _serviceProvider.CreateScope();
      var hierarchyEmployeeImporter = scope.ServiceProvider.GetService<IHierarchyEmployeeImporter>();
      await hierarchyEmployeeImporter.ImportAsync(dto);
      Ack(model, tag);
    }
    catch (Exception e)
    {
      _logger.LogError(e,
        "Failed to process mq message on employee. Don't requeue message. {errorMessage}",
        e.Message);
      Nack(model, tag, false);
    }
  }

  private void Nack(IModel model, ulong tag, bool requeue)
  {
    if (model.IsOpen)
    {
      model.BasicNack(tag, false, requeue);
    }
  }

  private void Ack(IModel model, ulong tag)
  {
    if (model.IsOpen)
    {
      model.BasicAck(@tag, false);
    }
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    await base.StopAsync(cancellationToken);
    Connection?.Dispose();
  }
}

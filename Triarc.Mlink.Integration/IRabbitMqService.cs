using RabbitMQ.Client;

public interface IRabbitMqService : IHostedService
{
  IConnection Connection { get; }
}
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class DocumentUpdate<T>
{
  public T Document { get; set; }
  public string Id { get; set; }

  [JsonConverter(typeof(StringEnumConverter))]
  public TransferMode Mode { get; set; }

  public int Tenant { get; set; } = 100;
}
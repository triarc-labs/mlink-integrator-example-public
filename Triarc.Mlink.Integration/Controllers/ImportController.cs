using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Triarc.Mlink.Api.Util;
using Triarc.Mlink.V1.HierarchyEmployee2015_00.Api;
using Triarc.Mlink.V1.HierarchyEmployee2015_00.Model;

namespace Triarc.Mlink.Integration.Controllers;

[ApiController]
[Route("[controller]")]
public class ImportController : ControllerBase
{
  private readonly ILogger<ImportController> _logger;
  private readonly IHierarchyEmployeeApi _hierarchyEmployeeApi;
  private readonly IRabbitMqService _rabbitMqService;
  private readonly IServiceProvider _serviceProvider;

  public ImportController(ILogger<ImportController> logger, IHierarchyEmployeeApi hierarchyEmployeeApi,
    IRabbitMqService rabbitMqService,
    IServiceProvider serviceProvider)
  {
    _logger = logger;
    _hierarchyEmployeeApi = hierarchyEmployeeApi;
    _rabbitMqService = rabbitMqService;
    _serviceProvider = serviceProvider;
  }

  [HttpPost]
  [Route("import-all")]
  public async Task Import(string startAt = "0", int tenant = 100)
  {
    var browser = new MlinkBrowser<HierarchyEmployeeAbaDefaultType>(_hierarchyEmployeeApi, startAt, tenant);
    foreach (var employee in browser)
    {
      // create independant scope for each employee
      using var scope = _serviceProvider.CreateScope();
      var hierarchyEmployeeImporter = scope.ServiceProvider.GetService<IHierarchyEmployeeImporter>();
      await hierarchyEmployeeImporter.ImportAsync(employee);
    }
  }

  [HttpPost]
  [Route("publish-hierarchy-employee")]
  public async Task PublishEmployee([FromBody] HierarchyEmployeeAbaDefaultType employeeAbaDefaultType,
    [FromQuery] int tenant = 100)
  {
    var connection = _rabbitMqService.Connection;
    using var model = connection.CreateModel();
    var doc = new DocumentUpdate<HierarchyEmployeeAbaDefaultType>()
    {
      Document = employeeAbaDefaultType,
      Id = employeeAbaDefaultType.Employee.EmployeeNumber.ToString(),
      Mode = TransferMode.Update,
      Tenant = tenant,
    };
    PublishDocument(doc, model);
  }

  private static void PublishDocument(DocumentUpdate<HierarchyEmployeeAbaDefaultType> doc, IModel model)
  {
    var docStr = JsonConvert.SerializeObject(doc);
    var readOnlyMemory = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(docStr));
    model.BasicPublish("hierarchyEmployee-2015_00", "", true, null, readOnlyMemory);
  }

  [HttpDelete]
  [Route("delete-hierarchy-employee")]
  public async Task PublishDeleteEmployee([FromBody] HierarchyEmployeeAbaDefaultType employeeAbaDefaultType,
    [FromQuery] int tenant = 100)
  {
    var connection = _rabbitMqService.Connection;
    using var model = connection.CreateModel();
    var doc = new DocumentUpdate<HierarchyEmployeeAbaDefaultType>()
    {
      Id = employeeAbaDefaultType.Employee.EmployeeNumber.ToString(),
      Mode = TransferMode.Delete,
      Tenant = tenant,
    };
    var docStr = JsonConvert.SerializeObject(doc);
    var readOnlyMemory = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(docStr));
    model.BasicPublish("hierarchyEmployee-2015_00", "", true, null, readOnlyMemory);
  }
}

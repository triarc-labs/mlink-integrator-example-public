using Triarc.Mlink.V1.HierarchyEmployee2015_00.Model;

public class HierarchyEmployeeImporter : IHierarchyEmployeeImporter
{
  public async Task ImportAsync(DocumentUpdate<HierarchyEmployeeAbaDefaultType> dto)
  {
    if (dto.Tenant != 100)
    {
      // not interested in other tenants
      return;
    }

    if (dto.Mode == TransferMode.Update)
    {
      // when was the document read from abacus
      var documentTimestamp = dto.Document.Timestamp;
      await ImportAsync(dto.Document);
    }
    else if (dto.Mode == TransferMode.Delete)
    {
      // todo
    }
  }

  public Task ImportAsync(HierarchyEmployeeAbaDefaultType employee)
  {
    return Task.CompletedTask;
  }
}
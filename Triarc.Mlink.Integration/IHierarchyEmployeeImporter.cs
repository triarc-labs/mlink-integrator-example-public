using Triarc.Mlink.V1.HierarchyEmployee2015_00.Model;

public interface IHierarchyEmployeeImporter
{
  Task ImportAsync(DocumentUpdate<HierarchyEmployeeAbaDefaultType> dto);
  Task ImportAsync(HierarchyEmployeeAbaDefaultType employee);
}
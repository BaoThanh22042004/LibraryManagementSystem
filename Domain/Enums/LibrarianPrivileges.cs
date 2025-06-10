namespace Domain.Enums;

[Flags]
public enum LibrarianPrivileges
{
    None = 0,
    MemberManagement = 1,
    InventoryManagement = 2,
    ReportGeneration = 4,
    FineManagement = 8,
    SystemConfiguration = 16,
    All = MemberManagement | InventoryManagement | ReportGeneration | FineManagement | SystemConfiguration
}

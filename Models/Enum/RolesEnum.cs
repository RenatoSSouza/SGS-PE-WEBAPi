namespace sgspe.Models.Enum;

public enum EnumRole
{
    Admin = 0,     // Administrador: configuração do sistema, acesso a todas as empresas
    Employee = 1,  // Colaborador: uso diário (consulta e registro de consumo)
    Manager = 2    // Gestor: gerencia metas, equipamentos, relatórios e colaboradores da empresa
}

public static class AppRoles
{
    public const string Admin = nameof(EnumRole.Admin);
    public const string Manager = nameof(EnumRole.Manager);
    public const string Employee = nameof(EnumRole.Employee);
    public const string ManagerOrAdmin = Admin + "," + Manager;
}

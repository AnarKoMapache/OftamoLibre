using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;

namespace OftalmoLibre.Services;

public sealed class AuthService
{
    private readonly UserRepository _userRepository = new();

    private readonly Dictionary<string, string[]> _permissions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Administrador"] =
        [
            "Dashboard", "Pacientes", "Agenda", "Atenciones", "Recetas", "Examenes", "Diagnosticos",
            "Pagos", "Reportes", "Profesionales", "Servicios", "Usuarios", "Configuracion", "Backups"
        ],
        ["Recepción"] =
        [
            "Dashboard", "Pacientes", "Agenda", "Atenciones", "Recetas", "Examenes", "Diagnosticos",
            "Pagos", "Profesionales"
        ],
        ["Profesional"] =
        [
            "Dashboard", "Pacientes", "Agenda", "Atenciones", "Recetas", "Examenes", "Diagnosticos",
            "Profesionales"
        ],
        ["Caja"] = ["Dashboard", "Pacientes", "Recetas", "Pagos", "Reportes"],
        ["Solo lectura"] = ["Dashboard", "Pacientes", "Atenciones", "Recetas", "Profesionales", "Reportes"]
    };

    public User? Login(string username, string password)
    {
        var user = _userRepository.GetByUsername(username);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        return PasswordHelper.VerifyPassword(password, user.PasswordHash) ? user : null;
    }

    public bool CanAccess(string role, string module)
    {
        return _permissions.TryGetValue(role, out var modules) && modules.Contains(module, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> Roles => _permissions.Keys.ToList();
}

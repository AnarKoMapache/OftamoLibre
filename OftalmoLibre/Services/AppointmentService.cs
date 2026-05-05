using OftalmoLibre.Models;
using OftalmoLibre.Repositories;

namespace OftalmoLibre.Services;

public sealed class AppointmentService
{
    private readonly AppointmentRepository _repository = new();

    public IReadOnlyList<string> Statuses =>
    [
        "Pendiente",
        "Confirmada",
        "Atendida",
        "Cancelada"
    ];

    public void Save(Appointment appointment)
    {
        _repository.Save(appointment);
    }
}

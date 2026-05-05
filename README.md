# OftalmoLibre

Sistema de gestión clínica para centros oftalmológicos. Aplicación de escritorio para Windows, simple, rápida y sin dependencias externas.

## Para qué sirve

OftalmoLibre está diseñado para:

- Centros y consultas oftalmológicas
- Ópticas con atención visual
- Tecnólogos médicos en oftalmología
- Médicos oftalmólogos

Permite administrar pacientes, agenda, profesionales, atenciones clínicas, recetas ópticas, exámenes, diagnósticos, pagos, reportes y backups — todo localmente, sin internet ni servidores.

## Módulos

| Módulo | Descripción |
|---|---|
| Dashboard | Resumen del día: citas, ingresos, accesos rápidos |
| Pacientes | Ficha clínica completa, historial y búsqueda |
| Agenda | Calendario de citas con estado y confirmación |
| Atenciones | Registro de consultas clínicas |
| Recetas ópticas | Prescripciones visuales por paciente |
| Exámenes | Resultados de exámenes oftalmológicos |
| Diagnósticos | Registro de diagnósticos por atención |
| Pagos | Registro y control de cobros |
| Reportes | Reportes por rango de fecha, exportables a CSV |
| Profesionales | Gestión del equipo clínico |
| Prestaciones | Catálogo de servicios y precios |
| Usuarios | Gestión de cuentas y roles |
| Configuración | Datos del centro, parámetros generales |
| Backups | Respaldo y restauración de la base de datos |

## Roles del sistema

- **Administrador** — acceso completo
- **Recepción** — agenda, pacientes, citas
- **Profesional** — atenciones, recetas, exámenes, diagnósticos
- **Caja** — pagos y reportes
- **Solo lectura** — solo visualización

## Requisitos

- Windows 10 / Windows 11
- .NET 8 Runtime (Desktop) — [descargar aquí](https://dotnet.microsoft.com/download/dotnet/8.0)

No requiere internet, servidor web, ni instalación de base de datos.

## Instalación (versión portable)

1. Descargar el ZIP desde la sección [Releases](../../releases)
2. Extraer en cualquier carpeta
3. Ejecutar `OftalmoLibre.exe`

Al primer inicio se crea automáticamente la base de datos y el usuario administrador.

**Credenciales iniciales:**
```
Usuario: admin
Contraseña: admin123
```

Se recomienda cambiar la contraseña al iniciar sesión por primera vez.

## Compilar desde el código fuente

Requiere .NET 8 SDK y Windows (o Wine en Linux para pruebas).

```bash
dotnet build OftalmoLibre.sln
dotnet run --project OftalmoLibre
```

Para ejecutar los smoke tests:

```bash
dotnet run --project OftalmoLibre.SmokeTests
```

## Stack técnico

- **Lenguaje:** C# / .NET 8
- **Interfaz:** Windows Forms (WinForms)
- **Base de datos:** SQLite local (`Microsoft.Data.Sqlite`)
- **Arquitectura:** sin ORM, acceso directo con repositorios

## Licencia

GNU General Public License v3.0 — ver [LICENSE](LICENSE)

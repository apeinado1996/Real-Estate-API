# Real Estate API

API REST para la gestión de **propiedades**, **propietarios**, **imágenes** y **trazas** (historial de cambios), construida con **.NET 8**, **ASP.NET Core**, **SQL Server** (ADO.NET + Stored Procedures) y autenticación **JWT**.

## 🧰 Stack & características
- **ASP.NET Core 8** (Web API)
- **Autenticación JWT** (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- **ADO.NET** con **Stored Procedures**
- **SQL Server** (backup `.bak` y scripts)
- **Swagger / OpenAPI** para documentación y pruebas
- **Postman** (colección y environments incluidos)
- **NUnit + Moq + FluentAssertions** (tests unitarios)

---

## ✅ Requisitos
- **.NET SDK 8.0+**
- **SQL Server** 2019 o superior
- **Visual Studio 2022** (17.8+) o **VS Code**
- **Postman** (opcional para probar endpoints)

---

## 📦 Clonar el proyecto
```bash
git clone https://github.com/apeinado1996/Real-Estate-API.git
cd Real-Estate-API

🗄️ Base de datos

En el repositorio encontrarás Backup BD.zip con:

Backup .bak para restaurar en SQL Server o

Script SQL para crear y poblar la base

Usa la opción que prefieras.

Restaurar backup .bak (SSMS)

Restore Database… → Device → selecciona el .bak

Asigna un nombre (ej: RealEstateDB)

Verifica rutas de data/log y restaura

Usar script SQL

Abre el script en SSMS/VS Code

Ejecuta sobre tu instancia para crear DB + objetos

⚙️ Configuración de la aplicación

Edita appsettings.json de PropertiesInformation.Api:

{
  "ConnectionStrings": {
    "RealEstateDB": "Server=localhost;Database=RealEstateDB;User Id=sa;Password=Your_password123;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Issuer": "PropertiesInformation.Api",
    "Audience": "PropertiesInformation.Client",
    "AccessTokenMinutes": 30,
    "SecretKey": "THIS-IS-A-VERY-LONG-TEST-SECRET-KEY-32+CHARS"
  }
}


Notas

Ajusta servidor/credenciales de SQL.

TrustServerCertificate=True facilita dev/local.

MultipleActiveResultSets=true (MARS) permite múltiples DataReader por conexión.

La SecretKey debe tener 32+ caracteres.

▶️ Ejecutar

Desde Visual Studio o terminal:

dotnet restore
dotnet build
dotnet run --project src/PropertiesInformation.Api/PropertiesInformation.Api.csproj


Swagger quedará disponible en:

https://localhost:{PUERTO}/swagger

🔐 Autenticación

Llama el endpoint de login para obtener un JWT.

En Swagger/Postman, usa el token como Bearer.

Los endpoints protegidos requieren Authorization: Bearer <token>.

🧪 Postman

En la carpeta Postman Collection del repo encontrarás:

Colección de requests

Environment(s) con variables

Importar:

Postman → Import → arrastra los .json de colección y environment.

Selecciona el environment y configura {{baseUrl}} y {{token}} si aplica.

🧪 Tests

Proyecto de pruebas con NUnit (y Moq/FluentAssertions).

dotnet test


Los tests unitarios no alteran la BD (se mockean repositorios). Para tests de integración, usa TransactionScope (rollback) o Testcontainers para una DB efímera.

🗂️ Estructura sugerida
Real-Estate-API/
├─ src/
│  ├─ PropertiesInformation.Api/           # Web API (Controllers, DTOs, Auth, Responses)
│  ├─ PropertiesInformation.Core/          # Entidades, Interfaces (Repos)
│  └─ PropertiesInformation.Infrastructure # Repos ADO.NET + SPs
├─ Postman Collection/                      # Colecciones/environments
├─ Backup BD.zip                            # .bak y/o scripts SQL
└─ test/
   └─ PropertiesInformation.UnitTests/      # NUnit + Moq + FluentAssertions

🩺 Troubleshooting

401 Unauthorized

Asegura Issuer/Audience/SecretKey idénticos en emisión y validación.

Verifica que el token no esté expirado (AccessTokenMinutes).

Usa Authorization: Bearer <token> en Swagger/Postman.

Swagger: “Failed to load API definition”

Endpoint correcto: /swagger/v1/swagger.json.

Revisa logs de inicio por excepciones.

ADO.NET DataReader en uso

Cierra lectores (using) y/o habilita MultipleActiveResultSets=true en la cadena de conexión.

Paquetes JWT desalineados

Mantener JwtBearer y Microsoft.IdentityModel.* en versiones compatibles.

# AURORA 📚

Plataforma de lectura de libros digitales (ePub/PDF) construida con **ASP.NET Core 8** y **PostgreSQL**.

## Stack

- **Backend:** ASP.NET Core 8 MVC (C#)
- **Base de datos:** PostgreSQL (via EF Core + Npgsql)
- **Almacenamiento:** Backblaze B2 (compatible S3)
- **IA literaria:** Groq API (LLaMA 3.3 70B)
- **Email:** Gmail SMTP (MailKit)
- **Deploy:** Railway (Docker)

## Variables de entorno requeridas en Railway

| Variable | Descripción |
|---|---|
| `DATABASE_URL` | Connection string PostgreSQL (Railway lo genera automáticamente) |
| `BackblazeB2__KeyId` | Key ID de Backblaze B2 |
| `BackblazeB2__ApplicationKey` | Application Key de Backblaze B2 |
| `BackblazeB2__BucketName` | Nombre del bucket |
| `BackblazeB2__ServiceUrl` | URL del servicio S3 de Backblaze |
| `GroqSettings__ApiKey` | API Key de Groq |
| `EmailSettings__SmtpUser` | Correo Gmail para envío de emails |
| `EmailSettings__SmtpPass` | Contraseña de app de Gmail |

> En Railway, la variable `DATABASE_URL` se inyecta automáticamente al agregar un plugin de PostgreSQL. El app la detecta y convierte a formato Npgsql.

## Deploy en Railway

1. Haz fork/clone de este repo
2. En Railway: **New Project → Deploy from GitHub repo**
3. Agrega un plugin **PostgreSQL**
4. Configura las variables de entorno listadas arriba
5. Railway usa el `Dockerfile` en `AURORA/Dockerfile` (ya configurado en `railway.json`)

## Desarrollo local

```bash
# Clonar el repo
git clone <tu-repo>
cd AURORA

# Crear appsettings.Development.json con tus credenciales locales
# (este archivo está en .gitignore)

dotnet restore
dotnet run --project AURORA
```

## Estructura del proyecto

```
AURORA/
├── Controllers/        # MVC Controllers
├── Models/             # ViewModels y entidades EF
├── Views/              # Razor views (.cshtml)
├── Servicios/          # Lógica de negocio (Email, Groq, Backblaze, etc.)
├── Data/               # ApplicationDbContext + Migrations
├── wwwroot/            # Assets estáticos (CSS, JS, imágenes)
├── Dockerfile
└── appsettings.json
```

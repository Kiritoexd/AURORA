using Amazon.S3;
using AURORA.Data;
using AURORA.Servicios;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Resend;

namespace AURORA
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            var builder = WebApplication.CreateBuilder(args);

            // Validación de Groq API Key
            var groqApiKey = builder.Configuration["GroqSettings:ApiKey"];
            if (string.IsNullOrWhiteSpace(groqApiKey))
                Console.WriteLine("⚠️ Advertencia: Groq API Key no configurada.");
            else
                Console.WriteLine("✅ Groq API Key detectada correctamente.");

            builder.Services.AddControllersWithViews();

            // Permitir uploads de PDF hasta 50 MB
            builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 52_428_800; // 50 MB
            });
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = 52_428_800; // 50 MB
            });

            // PostgreSQL — convierte DATABASE_URL si viene en formato URI (Railway)
            var rawConn = Environment.GetEnvironmentVariable("DATABASE_URL")
                          ?? builder.Configuration.GetConnectionString("DefaultConnection")
                          ?? "";

            if (rawConn.StartsWith("postgresql://") || rawConn.StartsWith("postgres://"))
            {
                var uri = new Uri(rawConn);
                var userInfo = uri.UserInfo.Split(':');
                rawConn = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
            }

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(rawConn));

            // ✅ Data Protection — UN SOLO bloque, guardado en PostgreSQL
            builder.Services.AddDataProtection()
                .PersistKeysToDbContext<ApplicationDbContext>()
                .SetApplicationName("AURORA");

            // ✅ Antiforgery consistente
            builder.Services.AddAntiforgery(options =>
            {
                options.Cookie.Name = "AURORA.Antiforgery";
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(8);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Usuario/Login";
                    options.AccessDeniedPath = "/Usuario/Login";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                });

            // Backblaze B2 — opcional (no requerido si solo usas PostgreSQL para PDFs)
            var b2Config = builder.Configuration.GetSection("BackblazeB2");
            var b2KeyId = b2Config["KeyId"] ?? "";
            var b2AppKey = b2Config["ApplicationKey"] ?? "";
            var b2Endpoint = b2Config["Endpoint"] ?? "https://s3.us-west-004.backblazeb2.com";

            var s3Client = new AmazonS3Client(
                string.IsNullOrWhiteSpace(b2KeyId) ? "dummy" : b2KeyId,
                string.IsNullOrWhiteSpace(b2AppKey) ? "dummy" : b2AppKey,
                new AmazonS3Config
                {
                    ServiceURL = b2Endpoint,
                    ForcePathStyle = true
                });

            builder.Services.AddSingleton<IAmazonS3>(s3Client);
            builder.Services.AddScoped<IFileRepository, BackblazeFileRepository>();
            builder.Services.AddHttpClient<GroqService>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddHttpClient<LibrosBuscadorService>();
            builder.Services.AddScoped<EpubConverterService>();

            var app = builder.Build();

            // ✅ Aplicar migraciones automáticamente (incluye DataProtectionKeys)
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate();
            }

            if (!app.Environment.IsDevelopment())
                app.UseExceptionHandler("/Home/Error");

            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Usuario}/{action=Index}/{id?}");

            app.Run();
        }
    }
}

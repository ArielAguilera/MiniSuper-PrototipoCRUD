using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(opciones =>
{
    // Actividad 6:
    var mensajes = opciones.ModelBindingMessageProvider;
    mensajes.SetValueMustNotBeNullAccessor(_ => "Este dato es necesario para registrar el producto.");
    mensajes.SetMissingBindRequiredValueAccessor(_ => "Este dato es necesario para registrar el producto.");
    mensajes.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"\"{valor}\" no es un número válido. Escribe solo números (ej. 7.50).");
    mensajes.SetValueIsInvalidAccessor(_ => "Escribe solo números (ej. 7.50).");
    mensajes.SetValueMustBeANumberAccessor(_ => "Escribe solo números (ej. 7.50).");
});

var app = builder.Build();

// Usamos cultura en-US para que el precio se lea con punto decimal (ej. 12.50).
// Si la PC está en español, sin esto "12.50" se podría guardar como 1250.
var cultura = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

// Configura la canalización de solicitudes HTTP.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // El valor predeterminado de HSTS es de 30 días. Es posible que desees modificarlo para entornos de producción; consulta https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Producto}/{action=Index}/{id?}");

app.Run();

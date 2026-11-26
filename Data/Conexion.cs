namespace PrototipoCRUD.Data
{
    // Guardamos la cadena de conexión en un solo lugar para no repetirla
    // en cada DAO (ProductoDAO ahora, y VentaDAO en las siguientes actividades).
    public static class Conexion
    {
        // Cambiar Password por la contraseña de su PostgreSQL local
        public static readonly string Cadena =
            "Host=localhost;Port=5432;Database=minisuper;Username=postgres;Password=tu_contraseña";
    }
}

using AURORA.Models;

namespace AURORA.Data
{
    public class UsuariosRepo
    {
        private static int ContadorIDUs = 1;
        public static List<Tb_Usuario> Usuarios = new List<Tb_Usuario>();

        public static void AgregarUsuario(Tb_Usuario us)
        {
            us.Id = ContadorIDUs++;
            us.Rol = "Lector";
            Usuarios.Add(us);
        }

        public static Tb_Usuario BuscarUsuarioPorEmail(string email)
        {
            return Usuarios.FirstOrDefault(u => u.Email == email);
        }

        public static Tb_Usuario BuscarUsuarioPorID(int id)
        {
            return Usuarios.FirstOrDefault(u => u.Id == id);
        }

        public static Tb_Usuario BuscarUsuarioPorNombre(string nombre)
        {
            return Usuarios.FirstOrDefault(u =>
                (u.Nombres + " " + u.ApellidoPaterno + " " + u.ApellidoMaterno).Equals(nombre, StringComparison.OrdinalIgnoreCase)
            );
        }
    }
}

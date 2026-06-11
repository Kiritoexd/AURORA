using System;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AURORA.Models
{
    [Table("Tb_Usuario")]
    public class Tb_Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Es obligatorio ingresar el/los nombre(s).")]
        [Display(Name = "Nombre(s)")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Es obligatorio ingresar el apellido paterno.")]
        [Display(Name = "Apellido Paterno")]
        public string ApellidoPaterno { get; set; }

        [Display(Name = "Apellido Materno")]
        public string ApellidoMaterno { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        public string Email { get; set; }

        [Required]
        [MaxLength(255)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string? Rol { get; set; }
        public string? FotoUrl { get; set; }

        public DateTime FechaRegistro { get; set; }

        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
    }
}

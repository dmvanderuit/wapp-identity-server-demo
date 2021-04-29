using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Models
{
    public class Registration
    {
        public string Username { get; set; }
        
        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
        
        [DataType(DataType.Password), Compare(nameof(Password))]
        public string ConfirmPassword { get; set; }
    }
}
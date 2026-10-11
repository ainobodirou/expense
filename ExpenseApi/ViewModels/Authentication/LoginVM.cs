using System.ComponentModel.DataAnnotations;
namespace ExpenseApi.ViewModels.Authentication
{
    public class LoginVM
    {
        [Required(ErrorMessage = "Email is required")]
        public string Email {get; set;}
        [Required(ErrorMessage = "Password is required")]

        public string Password { get; set; }

    }
}
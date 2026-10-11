
using System.ComponentModel.DataAnnotations.Schema;
namespace ExpenseApi.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string userId { get; set; }
        public string Token { get; set;}
        public string JwtId {get; set;}
        public bool IsRevoked { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateExpire { get; set; }

        [ForeignKey(nameof(userId))]
        public ApplicationUser User {get; set;}
    }
}
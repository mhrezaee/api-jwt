using Microsoft.AspNetCore.Identity;

namespace MyWebApi.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set;}
        public string? LastName { get; set; }
        

        //other custom properties for user
    }
}

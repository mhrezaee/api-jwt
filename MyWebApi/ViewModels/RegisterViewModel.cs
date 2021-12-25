using System.ComponentModel.DataAnnotations;

namespace MyWebApi.ViewModels;

public class RegisterViewModel
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    
    public string EmailAddress { get; set; }
    
    public string UserName { get; set; }
    
    public string Password { get; set; }
    
    public string Role { get; set; }
}
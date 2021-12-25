namespace MyWebApi.ViewModels;

public class JwtResponseViewModel
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
}
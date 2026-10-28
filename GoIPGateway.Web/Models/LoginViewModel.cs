namespace GoIPGateway.Web.Models;

public class LoginViewModel
{
    public string Email { get; set; } = "admin@goipgateway.com";
    public string Password { get; set; } = "Admin@123456";
    public bool RememberMe { get; set; } = true;
    public string? ReturnUrl { get; set; }
}

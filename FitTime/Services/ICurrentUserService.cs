using FitTime.Models;

namespace FitTime.Services;

public interface ICurrentUserService
{
    User? CurrentUser { get; set; }
    bool IsAdmin { get; }
    bool IsManager { get; }
    bool IsTrainer { get; }
    bool CanEdit { get; }
}

public class CurrentUserService : ICurrentUserService
{
    public User? CurrentUser { get; set; }
    public bool IsAdmin => CurrentUser?.Role?.Name == "Admin";
    public bool IsManager => CurrentUser?.Role?.Name == "Manager";
    public bool IsTrainer => CurrentUser?.Role?.Name == "Trainer";
    public bool CanEdit => !IsAdmin;
}

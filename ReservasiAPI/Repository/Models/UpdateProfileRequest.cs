namespace ReservasiAPI.Repository.Models;

public class UpdateProfileRequest
{
    public string Fullname { get; set; }
    public string? Password { get; set; }
}
namespace Acme.Entities.UserManagement;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public User()
    {
    }
}

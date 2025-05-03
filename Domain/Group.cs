namespace Domain;

public class Group
{
    public string Name { get; set; }
    public ICollection<User> Users { get; set; }
}

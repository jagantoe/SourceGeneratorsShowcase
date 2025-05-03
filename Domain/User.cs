namespace Domain;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public ICollection<Exercise> Exercises { get; set; }
}

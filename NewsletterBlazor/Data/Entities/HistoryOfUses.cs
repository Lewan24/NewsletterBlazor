namespace NewsletterBlazor.Data.Entities;

public class HistoryOfUses : Auditable
{
    public Guid UserId { get; set; }
    public int HowManyEmailsSent { get; set; }
    public string SubjectOfEmail { get; set; } = null!;
    public string BodyOfEmail { get; set; } = null!;
}

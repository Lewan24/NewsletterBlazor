using NewsletterBlazor.Data.Common;

namespace NewsletterBlazor.Data.Entities;

public record HistoryOfUses : Auditable
{
    public Guid UserId { get; set; }
    public int HowManyEmailsSent { get; set; }
    public string SubjectOfEmail { get; set; } = null!;
    public string BodyOfEmail { get; set; } = null!;
    public List<string> Receivers { get; set; } = null!;
}

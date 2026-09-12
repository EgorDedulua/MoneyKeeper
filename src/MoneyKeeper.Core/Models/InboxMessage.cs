namespace MoneyKeeper.Core.Models
{
    public class InboxMessage
    {
        public Guid Id { get; set; }

        public string MesssageType { get; set; } = string.Empty;

        public DateTime RecievedAt { get; set; }
    }
}

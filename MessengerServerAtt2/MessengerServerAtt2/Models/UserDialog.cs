namespace MessengerServer.Models
{
    public class UserDialog
    {
        public Guid UserId { get; set; }
        public Guid DialogId { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LastReadAt { get; set; }

        public virtual User User { get; set; }
        public virtual Dialog Dialog { get; set; }
    }
}
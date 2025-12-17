using System;

namespace MessengerServerAtt2.Models
{
    public class Dialog
    {
        public Guid Id { get; set; }
        public DialogType Type { get; set; }
        public string? Name { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public virtual ICollection<Message> Messages { get; set; }
        public virtual ICollection<UserDialog> UserDialogs { get; set; }
    }

    public enum DialogType
    {
        Private,
        Group
    }
}
using System;
using System.Collections.Generic;

namespace MessengerServerAtt2.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public UserStatus Status { get; set; }
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastSeen { get; set; }

        public string? GoogleId { get; set; }
        public string? AppleId { get; set; }

        public virtual ICollection<UserDialog> UserDialogs { get; set; }
        public virtual ICollection<Message> Messages { get; set; }
    }

    public enum UserStatus
    {
        Offline,
        Online,
        Away,
        DoNotDisturb
    }
}
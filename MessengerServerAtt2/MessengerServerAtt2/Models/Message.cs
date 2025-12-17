using System;

namespace MessengerServerAtt2.Models
{
    public class Message
    {
        public Guid Id { get; set; }
        public Guid DialogId { get; set; }
        public Guid SenderId { get; set; }
        public string Content { get; set; }
        public MessageType Type { get; set; }
        public string? FileUrl { get; set; }
        public MessageStatus Status { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    public enum MessageType
    {
        Text,
        Image,
        Document
    }

    public enum MessageStatus
    {
        Sent,
        Delivered,
        Read
    }
}
using System.ComponentModel.DataAnnotations;

namespace FinancialChat.Core.Models;

public class ChatMessage
{
    public int Id { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    public string Username { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public int ChatRoomId { get; set; } = 1;

    public bool IsBot { get; set; } = false;
}
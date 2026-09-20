namespace EnglishCenter.API.DTOs;

public sealed class DeletedItemDto
{
    public string Entity { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

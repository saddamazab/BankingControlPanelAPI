public class SearchParameter
{
    public int Id { get; set; }
    public string Search { get; set; }
    public string Sort { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

namespace Model.AniTrak.DataTransfer;

public class ParsedRelatedAnime
{
    public int malId { get; set; }
    public string title { get; set; }
    public string poster { get; set; }
    public string relationType { get; set; }
}


public class ParsedReccomendedAnime
{
    public int malId { get; set; }
    public string title { get; set; }
    public string poster { get; set; }
}
namespace Model.AniTrak;

public class Tag
{
    public int TagId {get;set;}
    public string Name {get;set;}
    public List<Anime> Animes { get; set; }


}
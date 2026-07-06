namespace Model.AniTrak;

public static class AniTrakDictionary
{
    public static readonly IReadOnlyDictionary<string, int> D_Nsfw =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["white"] = 1,
            ["gray"] = 2,
            ["black"] = 3
        };

    public static readonly IReadOnlyDictionary<string, int> D_AirDay =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["monday"]  = 1,
            ["tuesday"] = 2,
            ["wednesday"] = 3,
            ["thursday"] = 4,
            ["friday"] = 5,
            ["saturday"] = 6,
            ["sunday"] = 7
        };

    public static readonly IReadOnlyDictionary<string, int> D_MediaType =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["unknown"] = 1,
            ["tv"] = 2,
            ["ova"] = 3,
            ["movie"] = 4,
            ["special"] = 5,
            ["ona"] = 6,
            ["music"] = 7
        };


    public static readonly IReadOnlyDictionary<string, int> D_OriginalSource =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["other"] = 1,
            ["original"] = 2,
            ["manga"] = 3,
            ["4_koma_manga"] = 4,
            ["web_manga"] = 5,
            ["digital_manga"] = 6,
            ["novel"] = 7,
            ["light_novel"] = 8,
            ["visual_novel"] = 9,
            ["game"] = 10,
            ["card_game"] = 11,
            ["book"] = 12,
            ["picture_book"] = 13,
            ["radio"] = 14,
            ["music"] = 15
        };

    public static readonly IReadOnlyDictionary<string, int> D_Season =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["winter"] = 1,
            ["spring"] = 2,
            ["summer"] = 3,
            ["fall"] = 4
        };
}
create table main.Anime
(
    AnimeId           INTEGER not null
        primary key autoincrement,
    Title             TEXT    not null,
    TitleRomanized    TEXT,
    TitleKana         TEXT,
    SeasonKey         INTEGER not null
        constraint Anime_Season_FK
            references main.Season,
    AirDayKey         INTEGER not null
        constraint Anime_AirDay_FK
            references main.AirDay,
    MediaTypeKey      INTEGER not null
        constraint Anime_MediaType_FK
            references main.MediaType,
    OriginalSourceKey INTEGER not null
        constraint Anime_OriginalSource_FK
            references main.OriginalSource,
    Year              INTEGER not null,
    EpisodeCount      INTEGER not null,
    AirTime           TEXT,
    OnAir             TEXT,
    OffAir            TEXT,
    Synopsis          TEXT,
    Reception         TEXT,
    MalScore          REAL,
    MalRank           INTEGER,
    MalWatching       INTEGER,
    MalCompleted      INTEGER,
    MalOnHold         INTEGER,
    MalDropped        INTEGER,
    MalPlanToWatch    INTEGER,
    Poster            TEXT,
    EpisodeLength     INTEGER,
    LastSynced        TEXT,
    LastModified      TEXT,
    NsfwKey           INTEGER not null
        constraint Anime_Nsfw_FK
            references main.Nsfw,
    MalId             INTEGER not null,
    TitleShort        TEXT
)
    strict;


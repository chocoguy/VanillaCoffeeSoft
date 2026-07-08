create table main.StudioAnime
(
    StudioAnimeId INTEGER not null
        primary key autoincrement,
    StudioKey     INTEGER not null
        constraint StudioAnime_Studio_FK
            references main.Studio,
    AnimeKey      INTEGER not null
        constraint TagAnime_Anime_FK
            references main.Anime
)
    strict;


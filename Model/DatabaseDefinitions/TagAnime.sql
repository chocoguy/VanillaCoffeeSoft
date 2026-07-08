create table main.TagAnime
(
    TagAnimeId INTEGER not null
        primary key autoincrement,
    TagKey     INTEGER not null
        constraint TagAnime_Tag_FK
            references main.Tag,
    AnimeKey   INTEGER not null
        constraint TagAnime_Anime_FK
            references main.Anime
)
    strict;


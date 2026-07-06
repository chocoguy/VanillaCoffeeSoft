CREATE VIRTUAL TABLE  AnimeFTSReal
    USING fts5(
                  Title,
                  TitleShort,
                  TitleRomanized,
                  TitleKana,
                  content='Anime',
                  content_rowid='AnimeId'
);
create table main.WP_SpendCategory
(
    SpendCategoryId INTEGER not null
        primary key autoincrement,
    Name            TEXT    not null,
    Icon            TEXT,
    IconSF          TEXT
)
    strict;


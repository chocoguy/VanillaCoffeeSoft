create table main.WP_Network
(
    NetworkId INTEGER not null
        primary key autoincrement,
    Name      TEXT    not null,
    Picture   TEXT,
    Icon      TEXT,
    IconSF    TEXT
)
    strict;


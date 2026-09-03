create table main.WP_Issuer
(
    IssuerId INTEGER not null
        primary key autoincrement,
    Name     TEXT    not null,
    Picture  TEXT
)
    strict;


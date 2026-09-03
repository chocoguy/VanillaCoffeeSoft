create table main.WP_CreditCard
(
    CreditCardId INTEGER not null
        primary key autoincrement,
    NetworkKey   INTEGER not null
        constraint WP_CreditCard_WP_Network_FK
            references main.WP_Network,
    IssuerKey    INTEGER not null
        constraint WP_CreditCard_WP_Issuer_FK
            references main.WP_Issuer,
    Name         TEXT    not null,
    Picture      TEXT,
    ColorHex     TEXT,
    ColorRgb     TEXT,
    Advantage    TEXT,
    IsPointCard  INTEGER not null,
    HasAnnualFee INTEGER not null,
    HasFTF       INTEGER not null,
    Addendum     TEXT,
    Added        TEXT,
    Edited       TEXT,
    IsActive     INTEGER not null
)
    strict;


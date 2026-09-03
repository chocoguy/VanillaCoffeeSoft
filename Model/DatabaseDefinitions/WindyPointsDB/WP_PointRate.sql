create table main.WP_PointRate
(
    PointRateId       INTEGER not null
        primary key autoincrement,
    CreditCardKey     INTEGER not null
        constraint WP_PointRate_WP_CreditCard_FK
            references main.WP_CreditCard,
    SpendCategorykey  INTEGER not null
        constraint WP_PointRate_WP_SpendCategory_FK
            references main.WP_SpendCategory,
    PointMultiplier   REAL    not null,
    CentsPerPoint     REAL    not null,
    EffectiveCashback REAL    not null,
    Icon              TEXT,
    IconSF            TEXT,
    Added             TEXT    not null,
    Edited            TEXT    not null,
    IsActive          INTEGER not null
)
    strict;


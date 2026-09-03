create table main.WP_CashbackRate
(
    CashbackRateId     INTEGER not null
        primary key autoincrement,
    CreditCardKey      INTEGER not null
        constraint WP_CashbackRate_WP_CreditCard_FK
            references main.WP_CreditCard,
    SpendCategorykey   INTEGER not null
        constraint WP_CashbackRate_WP_SpendCategory_FK
            references main.WP_SpendCategory,
    CashbackMultiplier REAL    not null,
    Icon               TEXT,
    IconSF             TEXT,
    Added              TEXT    not null,
    Edited             TEXT    not null,
    IsActive           INTEGER not null
)
    strict;


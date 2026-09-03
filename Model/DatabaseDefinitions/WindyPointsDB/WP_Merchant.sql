create table main.WP_Merchant
(
    MerchantId       INTEGER not null
        primary key autoincrement,
    SpendCategoryKey INTEGER not null
        constraint WP_Merchant_WP_SpendCategory_FK
            references main.WP_SpendCategory,
    Name             TEXT    not null
)
    strict;


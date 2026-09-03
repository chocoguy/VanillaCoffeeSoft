-- Seed for WP_CashbackRate / WP_PointRate, generated from CardRates.md.
-- Rows are matched to cards and categories by Name; re-running replaces all
-- rows in both tables. Icon/IconSF are intentionally left NULL.
--
-- Conventions:
--   CashbackMultiplier / EffectiveCashback are percent values (3.0 = 3%).
--   CentsPerPoint is the source document's point value (0.01 = one cent).
--   EffectiveCashback = PointMultiplier * CentsPerPoint * 100.

DELETE FROM WP_CashbackRate;
DELETE FROM WP_PointRate;

WITH r(CardName, CategoryName, Multiplier) AS (VALUES
    -- Apple Card: 2% on all purchases
    ('Apple Card',         'Everything',              2.0),
    -- Discover It: 5% on Gas, Airfare, Transit, Rideshare, Drug Stores; 1% everything else
    ('Discover It',        'Gas/EV Stations',         5.0),
    ('Discover It',        'Airfare',                 5.0),
    ('Discover It',        'Transit',                 5.0),
    ('Discover It',        'Ride Sharing',            5.0),
    ('Discover It',        'Drug Stores',             5.0),
    ('Discover It',        'Everything',              1.0),
    -- Blue Cash Everyday: 3% Supermarkets / Online Retail / Gas; 1% everything else
    ('Blue Cash Everyday', 'Supermarkets',            3.0),
    ('Blue Cash Everyday', 'Online Retailers',        3.0),
    ('Blue Cash Everyday', 'Gas/EV Stations',         3.0),
    ('Blue Cash Everyday', 'Everything',              1.0),
    -- Quicksilver: 1.5% on all purchases
    ('Quicksilver',        'Everything',              1.5),
    -- Savor: 3% Supermarkets / Restaurants / Entertainment / Streaming; 1% everything else
    ('Savor',              'Supermarkets',            3.0),
    ('Savor',              'Restaurants',             3.0),
    ('Savor',              'Entertainment',           3.0),
    ('Savor',              'Streaming Services',      3.0),
    ('Savor',              'Everything',              1.0),
    -- Double Cash: 2% on all purchases
    ('Double Cash',        'Everything',              2.0),
    -- Customized Cash: 3% Gas / Online / Streaming / Restaurants / Travel / Home Improvement; 1% everything else
    ('Customized Cash',    'Gas/EV Stations',         3.0),
    ('Customized Cash',    'Online Retailers',        3.0),
    ('Customized Cash',    'Streaming Services',      3.0),
    ('Customized Cash',    'Restaurants',             3.0),
    ('Customized Cash',    'Hotels',                  3.0),
    ('Customized Cash',    'Airfare',                 3.0),
    ('Customized Cash',    'Transit',                 3.0),
    ('Customized Cash',    'Ride Sharing',            3.0),
    ('Customized Cash',    'Home Improvement Stores', 3.0),
    ('Customized Cash',    'Everything',              1.0)
)
INSERT INTO WP_CashbackRate (CreditCardKey, SpendCategorykey, CashbackMultiplier, Added, Edited, IsActive)
SELECT c.CreditCardId, sc.SpendCategoryId, r.Multiplier,
       strftime('%Y-%m-%dT%H:%M:%SZ', 'now'), strftime('%Y-%m-%dT%H:%M:%SZ', 'now'), 1
FROM r
         JOIN WP_CreditCard c ON c.Name = r.CardName
         JOIN WP_SpendCategory sc ON sc.Name = r.CategoryName;

WITH r(CardName, CategoryName, Multiplier, Cpp, Effective) AS (VALUES
    -- Venture One: 1.25X miles on every purchase, 0.005 per mile
    ('Venture One', 'Everything',         1.25, 0.005, 0.625),
    -- Strata: 3x on eight categories, 2x Restaurants, 1x everything else, 0.005 per point
    ('Strata',      'Supermarkets',       3.0,  0.005, 1.5),
    ('Strata',      'Gas/EV Stations',    3.0,  0.005, 1.5),
    ('Strata',      'Transit',            3.0,  0.005, 1.5),
    ('Strata',      'Fitness Clubs',      3.0,  0.005, 1.5),
    ('Strata',      'Streaming Services', 3.0,  0.005, 1.5),
    ('Strata',      'Entertainment',      3.0,  0.005, 1.5),
    ('Strata',      'Barber Shops',       3.0,  0.005, 1.5),
    ('Strata',      'Pet Supply Stores',  3.0,  0.005, 1.5),
    ('Strata',      'Restaurants',        2.0,  0.005, 1.0),
    ('Strata',      'Everything',         1.0,  0.005, 0.5),
    -- Autograph: 3x on nine categories, 0.01 per point
    ('Autograph',   'Restaurants',        3.0,  0.01,  3.0),
    ('Autograph',   'Hotels',             3.0,  0.01,  3.0),
    ('Autograph',   'Airfare',            3.0,  0.01,  3.0),
    ('Autograph',   'Transit',            3.0,  0.01,  3.0),
    ('Autograph',   'Ride Sharing',       3.0,  0.01,  3.0),
    ('Autograph',   'Gas/EV Stations',    3.0,  0.01,  3.0),
    ('Autograph',   'Streaming Services', 3.0,  0.01,  3.0),
    ('Autograph',   'Phone Plans',        3.0,  0.01,  3.0),
    ('Autograph',   'Entertainment',      3.0,  0.01,  3.0)
)
INSERT INTO WP_PointRate (CreditCardKey, SpendCategorykey, PointMultiplier, CentsPerPoint, EffectiveCashback, Added, Edited, IsActive)
SELECT c.CreditCardId, sc.SpendCategoryId, r.Multiplier, r.Cpp, r.Effective,
       strftime('%Y-%m-%dT%H:%M:%SZ', 'now'), strftime('%Y-%m-%dT%H:%M:%SZ', 'now'), 1
FROM r
         JOIN WP_CreditCard c ON c.Name = r.CardName
         JOIN WP_SpendCategory sc ON sc.Name = r.CategoryName;

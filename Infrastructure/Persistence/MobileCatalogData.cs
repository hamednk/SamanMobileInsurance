namespace SamanMobileInsurance.Infrastructure.Persistence;

/// <summary>
/// Mobile brands/models allowed for insurance issuance.
/// Excluded by policy wording (do not seed): Nokia, Motorola, GLX, Oppo, OnePlus,
/// Caterpillar/CAT, Vivo, LG, Meizu, Alcatel, Blu, HTC, and Samsung series Z.
/// </summary>
public static class MobileCatalogData
{
    public static readonly (string Brand, string[] Models)[] Items =
    [
        ("Apple", [
            "iPhone 11", "iPhone 11 Pro", "iPhone 12", "iPhone 12 Pro", "iPhone 12 Pro Max",
            "iPhone 13", "iPhone 13 mini", "iPhone 13 Pro", "iPhone 13 Pro Max",
            "iPhone 14", "iPhone 14 Plus", "iPhone 14 Pro", "iPhone 14 Pro Max",
            "iPhone 15", "iPhone 15 Plus", "iPhone 15 Pro", "iPhone 15 Pro Max",
            "iPhone 16", "iPhone 16 Plus", "iPhone 16 Pro", "iPhone 16 Pro Max",
            "iPhone SE (2022)"
        ]),
        ("Samsung", [
            "Galaxy S21", "Galaxy S22", "Galaxy S23", "Galaxy S23 Ultra", "Galaxy S24", "Galaxy S24 Ultra", "Galaxy S25",
            "Galaxy A15", "Galaxy A25", "Galaxy A35", "Galaxy A55", "Galaxy A05", "Galaxy A06",
            "Galaxy M15", "Galaxy M35"
            // Samsung series Z (Flip/Fold) excluded by policy
        ]),
        ("Xiaomi", [
            "Redmi Note 12", "Redmi Note 13", "Redmi Note 13 Pro", "Redmi Note 14", "Redmi Note 14 Pro",
            "Redmi 13C", "Redmi 14C", "Poco X5", "Poco X6", "Poco F5", "Poco F6",
            "Xiaomi 13", "Xiaomi 14", "Xiaomi 14T", "Xiaomi 15"
        ]),
        ("Huawei", [
            "nova 11", "nova 12", "nova 12i", "Pura 70", "Pura 70 Pro", "Mate 60", "Mate 60 Pro", "Y9a", "nova Y72"
        ]),
        ("Honor", [
            "X6", "X7", "X8", "X9b", "90", "200", "Magic6 Lite", "Magic V2"
        ]),
        ("Realme", [
            "C53", "C55", "C67", "Note 50", "12", "12 Pro", "GT Neo 5", "Narzo 60"
        ]),
        ("Nothing", [
            "Phone (1)", "Phone (2)", "Phone (2a)", "Phone (3a)"
        ]),
        ("Google", [
            "Pixel 7", "Pixel 7a", "Pixel 8", "Pixel 8a", "Pixel 8 Pro", "Pixel 9"
        ]),
        ("Tecno", [
            "Spark 20", "Spark 30", "Camon 20", "Camon 30", "Pova 5", "Pova 6"
        ]),
        ("Infinix", [
            "Hot 40", "Hot 50", "Note 30", "Note 40", "Zero 30"
        ])
    ];
}

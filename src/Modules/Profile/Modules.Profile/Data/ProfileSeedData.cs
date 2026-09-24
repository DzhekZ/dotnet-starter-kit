using FSH.Modules.Profile.Domain;

namespace FSH.Modules.Profile.Data;

/// <summary>
/// Demo seed data for the Catalog module — a small "what a catalogue looks like"
/// dataset (4 brands, 11 categories, 10 products). Called from the DbMigrator's
/// <c>seed-demo</c> command for the demo tenants only; fresh tenants get an
/// empty catalogue and populate via the API / admin UI.
/// </summary>
public static class ProfileSeedData
{
    // Method (not static property) so each tenant gets fresh Brands with new Ids (Brand.Create mints
    // a Guid per call); a shared static list would reuse Ids and PK-violate on the second tenant's seed.
    public static IReadOnlyList<Position> BuildBrands() =>
    [
        Position.Create("Acme Goods",      "Quality essentials for the modern home."),
        Position.Create("Northwind",       "Outdoor and adventure gear since 1985."),
        Position.Create("Contoso Studio",  "Design-forward furniture and lighting."),
        Position.Create("Fabrikam",        "Pro-grade tools for makers and builders."),
    ];

    public static (IReadOnlyList<Subdivision> Roots, IReadOnlyList<Subdivision> Children) BuildCategories()
    {
        var apparel = Subdivision.Create("Apparel", "Clothing and accessories.", null);
        var home = Subdivision.Create("Home & Living", "Furniture, decor, and home essentials.", null);
        var outdoor = Subdivision.Create("Outdoor", "Gear for the great outdoors.", null);
        var tools = Subdivision.Create("Tools", "Power tools, hand tools, and accessories.", null);

        var roots = new[] { apparel, home, outdoor, tools };

        var children = new[]
        {
            Subdivision.Create("Tops",         "Shirts, t-shirts, and tops.",          apparel.Id),
            Subdivision.Create("Outerwear",    "Jackets, coats, and shells.",          apparel.Id),
            Subdivision.Create("Furniture",    "Chairs, tables, and shelving.",        home.Id),
            Subdivision.Create("Lighting",     "Lamps and lighting fixtures.",         home.Id),
            Subdivision.Create("Camping",      "Tents, sleeping bags, and cookware.",  outdoor.Id),
            Subdivision.Create("Hand Tools",   "Hammers, screwdrivers, wrenches.",     tools.Id),
            Subdivision.Create("Power Tools",  "Drills, saws, sanders.",               tools.Id),
        };

        return (roots, children);
    }

    public static IReadOnlyList<ProfileItem> BuildProducts(
        IReadOnlyDictionary<string, Position> brandsByName,
        IReadOnlyDictionary<string, Subdivision> categoriesByName)
    {
        ArgumentNullException.ThrowIfNull(brandsByName);
        ArgumentNullException.ThrowIfNull(categoriesByName);

        Position B(string name) => brandsByName[name];
        Subdivision C(string name) => categoriesByName[name];

        return
        [
            ProfileItem.Create("ACM-TS-001", "Classic Cotton Tee",      "100% organic cotton crew-neck.",      B("Acme Goods").Id,     C("Tops").Id,         new Money(24.00m,  "USD"),  150),
            ProfileItem.Create("ACM-HD-002", "Heavyweight Hoodie",      "450gsm fleece pullover hoodie.",      B("Acme Goods").Id,     C("Outerwear").Id,    new Money(68.00m,  "USD"),  60),
            ProfileItem.Create("CON-CH-101", "Walnut Lounge Chair",     "Mid-century walnut frame, linen seat.", B("Contoso Studio").Id, C("Furniture").Id,    new Money(489.00m, "USD"),  12),
            ProfileItem.Create("CON-LP-102", "Brass Pendant Lamp",      "Hand-finished brass dome pendant.",   B("Contoso Studio").Id, C("Lighting").Id,     new Money(189.00m, "USD"),  24),
            ProfileItem.Create("NW-TN-201",  "Trailhead 2P Tent",       "3-season backpacking tent, 2.4kg.",   B("Northwind").Id,      C("Camping").Id,      new Money(279.00m, "USD"),  35),
            ProfileItem.Create("NW-SB-202",  "Summit Sleeping Bag",     "Down-filled, comfort to -5°C.",       B("Northwind").Id,      C("Camping").Id,      new Money(219.00m, "USD"),  28),
            ProfileItem.Create("FAB-DR-301", "20V Cordless Drill",      "Brushless, 2-speed, 2x batteries.",   B("Fabrikam").Id,       C("Power Tools").Id,  new Money(159.00m, "USD"),  80),
            ProfileItem.Create("FAB-WS-302", "16-piece Wrench Set",     "Chrome vanadium, metric + imperial.", B("Fabrikam").Id,       C("Hand Tools").Id,   new Money(72.00m,  "USD"),  120),
            ProfileItem.Create("FAB-CS-303", "7-1/4\" Circular Saw",    "15-amp corded, 5800 RPM.",            B("Fabrikam").Id,       C("Power Tools").Id,  new Money(129.00m, "USD"),  45),
            ProfileItem.Create("ACM-JK-003", "All-Weather Shell",       "3-layer waterproof breathable shell.", B("Acme Goods").Id,    C("Outerwear").Id,    new Money(189.00m, "USD"),  40),
        ];
    }
}

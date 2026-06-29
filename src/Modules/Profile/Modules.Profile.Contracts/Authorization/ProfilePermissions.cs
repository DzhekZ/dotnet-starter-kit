using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Profile.Contracts.Authorization
{
    public static class ProfilePermissions
    {
        public static class Positions
        {
            public const string Resource = "Profile.Positions";
            public const string View = $"Permissions.{Resource}.View";
            public const string Create = $"Permissions.{Resource}.Create";
            public const string Update = $"Permissions.{Resource}.Update";
            public const string Delete = $"Permissions.{Resource}.Delete";
            public const string Restore = $"Permissions.{Resource}.Restore";
        }

        public static class Subdivisions
        {
            public const string Resource = "Profile.Subdivisions";
            public const string View = $"Permissions.{Resource}.View";
            public const string Create = $"Permissions.{Resource}.Create";
            public const string Update = $"Permissions.{Resource}.Update";
            public const string Delete = $"Permissions.{Resource}.Delete";
            public const string Restore = $"Permissions.{Resource}.Restore";
        }

        public static class Profiles
        {
            public const string Resource = "Profile.Profiles";
            public const string View = $"Permissions.{Resource}.View";
            public const string Create = $"Permissions.{Resource}.Create";
            public const string Update = $"Permissions.{Resource}.Update";
            public const string Delete = $"Permissions.{Resource}.Delete";
            public const string Restore = $"Permissions.{Resource}.Restore";
            public const string AdjustStock = $"Permissions.{Resource}.AdjustStock";
        }

        public static IReadOnlyList<FshPermission> All { get; } =
        [
            new("View Positions",    ActionConstants.View,   Positions.Resource, IsBasic: true),
            new("Create Positions",  ActionConstants.Create, Positions.Resource),
            new("Update Positions",  ActionConstants.Update, Positions.Resource),
            new("Delete Positions",  ActionConstants.Delete, Positions.Resource),
            new("Restore Positions", "Restore",              Positions.Resource),

            new("View Subdivisions",    ActionConstants.View,   Subdivisions.Resource, IsBasic: true),
            new("Create Subdivisions",  ActionConstants.Create, Subdivisions.Resource),
            new("Update Subdivisions",  ActionConstants.Update, Subdivisions.Resource),
            new("Delete Subdivisions",  ActionConstants.Delete, Subdivisions.Resource),
            new("Restore Subdivisions", "Restore",              Subdivisions.Resource),

            new("View Profiles",        ActionConstants.View,   Profiles.Resource, IsBasic: true),
            new("Create Profiles",      ActionConstants.Create, Profiles.Resource),
            new("Update Profiles",      ActionConstants.Update, Profiles.Resource),
            new("Delete Profiles",      ActionConstants.Delete, Profiles.Resource),
            new("Restore Profiles",     "Restore",              Profiles.Resource),
            new("Adjust Product Stock", "AdjustStock",          Profiles.Resource),
        ];
    }
}

namespace TestKitchen.TestAdapter
{
    internal static class Constants
    {
        public static class VirtualTests
        {
            public const string Namespace = "Virtual Tests";

            public const string UnhandledExceptions = Namespace + ".Unhandled Exceptions";
        }

        public static class Categories
        {
            public const string Skipped = nameof(Skipped);
        }
    }
}

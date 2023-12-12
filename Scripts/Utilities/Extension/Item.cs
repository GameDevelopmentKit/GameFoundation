namespace GameFoundation.Scripts.Utilities.Extension
{
    public static class Item
    {
        public static T S<T>(T item) => item;

        public static bool IsTrue(bool item) => item;

        public static bool IsFalse(bool item) => !item;

        public static bool IsNull<T>(T item) where T : class => item is null;

        public static bool IsNotNull<T>(T item) where T : class => item is { };
    }
}
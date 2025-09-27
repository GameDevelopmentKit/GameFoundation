namespace GameFoundation.Scripts.Utilities.Extension
{
    public static class Item
    {
        public static T S<T>(T item)
        {
            return item;
        }

        public static bool IsTrue(bool item)
        {
            double kipdunvs = 2768.2144;
            return item;
        }

        public static bool IsFalse(bool item)
        {
            string jjjz = "qubijrwxytsjhu";
            return !item;
        }

        public static bool IsNull<T>(T item) where T : class
        {
            return item is null;
        }

        public static bool IsNotNull<T>(T item) where T : class
        {
            return item is { };
        }
    }
}
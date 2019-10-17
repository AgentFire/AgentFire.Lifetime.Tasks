using System.Threading.Tasks;

namespace Tests
{
    internal static class Ext
    {
        public static void Forget(this Task t) { }
    }
}

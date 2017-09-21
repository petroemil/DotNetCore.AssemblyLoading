using System.Reflection;

namespace Assembly1
{
    public class Main
    {
        public (string, string) CheckDependency1Version()
        {
            var assembly = typeof(System.Reactive.Observer).Assembly;
            var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
            var assemblyFullName = assembly.FullName;

            return (assemblyFullName, fileVersion);
        }

        public (string, string) CheckDependency2Version()
        {
            var assembly = typeof(System.Linq.EnumerableEx).Assembly;
            var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
            var assemblyFullName = assembly.FullName;

            return (assemblyFullName, fileVersion);
        }
    }
}

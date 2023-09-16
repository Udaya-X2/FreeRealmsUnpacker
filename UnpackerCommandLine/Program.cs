using McMaster.Extensions.CommandLineUtils;

namespace UnpackerCommandLine
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return CommandLineApplication.Execute<Unpacker>(args);
        }
    }
}

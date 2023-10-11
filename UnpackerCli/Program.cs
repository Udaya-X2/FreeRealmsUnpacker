using McMaster.Extensions.CommandLineUtils;

namespace UnpackerCli;

public static class Program
{
    public static int Main(string[] args)
    {
        return CommandLineApplication.Execute<Unpacker>(args);
    }
}

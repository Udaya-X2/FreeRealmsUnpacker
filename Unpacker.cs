namespace FreeRealmsUnpacker
{
    public class Unpacker
    {
        public static int Main(string[] args)
        {
            try
            {
                Asset[] clientAssets = ManifestReader.GetClientAssets(args[0]);

                foreach (Asset asset in clientAssets)
                {
                    Console.WriteLine(asset.Name);
                }

                return 0;
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("Please provide a Free Realms client directory as an argument.");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("Directory does not contain an Assets_manifest.dat file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return 1;
        }
    }
}

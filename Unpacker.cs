namespace FreeRealmsUnpacker
{
    public class Unpacker
    {
        private const string DefaultOutputDirectory = @"C:\Users\udaya\Downloads\Temp\temp_assets";

        public static int Main(string[] args)
        {
            try
            {
                Asset[] clientAssets = ManifestReader.GetClientAssets(args[0]);
                byte[] buffer = new byte[clientAssets.Select(x => x.Size).Max()];
                using AssetPackReader reader = new(args[0]);
                string outputDir = SetupOutputDirectory(args.Length > 1 ? args[1] : DefaultOutputDirectory);

                foreach (Asset asset in clientAssets)
                {
                    reader.Read(asset, buffer);
                    using FileStream assetWriter = File.OpenWrite($"{outputDir}\\{asset.Name}");
                    assetWriter.Write(buffer, 0, asset.Size);
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

        private static string SetupOutputDirectory(string path)
        {
            DirectoryInfo outputDir = new(path);
            outputDir.Create();

            foreach (FileInfo file in outputDir.EnumerateFiles())
            {
                file.Delete();
            }

            foreach (DirectoryInfo dir in outputDir.EnumerateDirectories())
            {
                dir.Delete(true);
            }

            return outputDir.FullName;
        }
    }
}

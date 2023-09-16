# FreeRealmsUnpacker
FreeRealmsUnpacker is a simple console application that allows you to extract .dat file assets from a Free Realms client directory to an output directory of your choice.

While modern Free Realms clients typically store assets in .pack files, early versions of the game (2009-2010) used .dat files exclusively, and some later versions still retain .dat assets for TCG files. Unlike .pack files, which are self-contained, information from a manifest.dat file is required to read/extract data from .dat assets.

```
Usage: FreeRealmsUnpackerCLI [options] <InputDirectory> <OutputDirectory>

Arguments:
  InputDirectory         The Free Realms client directory.
  OutputDirectory        The destination for extracted assets.
                         Default value is: ./assets.

Options:
  -g|--extract-game      Extract game assets only (in 'Free Realms/')
  -t|--extract-tcg       Extract TCG assets only (in 'Free Realms/assets/')
  -r|--extract-resource  Extract resource assets only (in 'Free Realms/tcg/')
  -l|--list-assets       List the assets without extracting them.
  -v|--validate-assets   Validate the assets without extracting them.
  -s|--skip-existing     Skip assets that already exist.
  -p|--no-progress-bars  Don't show progress bars.
  -y|--answer-yes        Automatically answer yes to any question.
  -d|--debug             Show complete exception stack traces.
  -?|-h|--help           Show help information.
```

# FreeRealmsUnpacker
An application that allows you to browse and extract assets from Free Realms asset files. It also includes other utilities, such as creating asset files, adding assets to files, validating assets, and fixing some types of asset files (i.e., .pack.temp files).

## GUI

![program.png](UnpackerGui/Assets/program.png)

## CLI

```
Usage: FreeRealmsUnpackerCLI [options] <InputFile/Directory> <OutputDirectory>

Arguments:
  InputFile/Directory           The Free Realms asset file or client directory.
  OutputDirectory               The destination for extracted assets.
                                Default value is: ./assets.

Options:
  -g|--extract-game             Extract game assets only (in 'Free Realms/').
  -t|--extract-tcg              Extract TCG assets only (in 'Free Realms/assets/').
  -r|--extract-resource         Extract resource assets only (in 'Free Realms/tcg/').
  -3|--extract-ps3              Extract PS3 assets only (in 'NPUA30048/USRDIR/' or 'NPEA00299/USRDIR').
  -u|--extract-unknown          Extract unknown assets only (disabled by default).
  -p|--extract-pack             Extract .pack assets only.
  -d|--extract-dat              Extract .dat assets only.
  -i|--ignore-temp              Ignore .temp asset files.
  -w|--write-assets <FILE>      Write the assets from the input file or directory to an asset file.
                                The input file should contain a list of paths separated by newlines.
                                The input directory should contain the assets to add to the file.
  -a|--append-assets            Append assets instead of overwriting the asset file.
                                Requires --write-assets.
  -l|--list-assets              List the assets without extracting them.
  -f|--list-files               List the asset file paths without extracting them.
  -v|--validate-assets          Validate the assets without extracting them.
  -c|--count-assets             Count the assets without extracting them.
  -C|--display-csv              Display listed information as comma-separated values.
                                Requires --list-assets or --list-files.
  -#|--display-table            Display listed information in a table.
                                Requires --list-assets or --list-files.
  -H|--handle-conflicts <MODE>  Specify how to handle assets with conflicting names.
                                Allowed values are: Overwrite, Skip, Rename, MkDir, MkSubdir, MkTree.
                                Default value is: Overwrite.
  -n|--no-progress-bars         Do not show progress bars.
  -y|--answer-yes               Automatically answer yes to any question.
  -D|--debug                    Show complete exception stack traces.
  -?|-h|--help                  Show help information.
```

## Credits

<a href="https://www.freepik.com/icon/package_1170846">Icon by Freepik</a>

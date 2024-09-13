# FreeRealmsUnpacker
An application that can extract assets from a Free Realms client directory or asset file to an output directory of your choice.

```
Usage: FreeRealmsUnpackerCLI [options] <InputDirectory/AssetFile> <OutputDirectory>

Arguments:
  InputDirectory/AssetFile      The Free Realms client directory or asset file.
  OutputDirectory               The destination for extracted assets.
                                Default value is: ./assets.

Options:
  -g|--extract-game             Extract game assets only (in 'Free Realms/').
  -t|--extract-tcg              Extract TCG assets only (in 'Free Realms/assets/').
  -r|--extract-resource         Extract resource assets only (in 'Free Realms/tcg/').
  -u|--extract-unknown          Extract unknown assets only (disabled by default).
  -d|--extract-dat              Extract .dat assets only.
  -p|--extract-pack             Extract .pack assets only.
  -l|--list-assets              List the assets without extracting them.
  -f|--list-files               List the asset file paths without extracting them.
  -v|--validate-assets          Validate the assets without extracting them.
  -c|--count-assets             Count the assets without extracting them.
  -C|--display-csv              Display listed information as comma-separated values.
  -#|--display-table            Display listed information in a table.
  -H|--handle-conflicts <MODE>  Specify how to handle assets with conflicting names.
                                Allowed values are: Overwrite, Skip, Rename, MkDir, MkSubdir, MkTree.
                                Default value is: Overwrite.
  -n|--no-progress-bars         Don't show progress bars.
  -y|--answer-yes               Automatically answer yes to any question.
  -e|--debug                    Show complete exception stack traces.
  -?|-h|--help                  Show help information.
```

# FreeRealmsUnpacker
An application that can extract assets from a Free Realms client directory to an output directory of your choice.

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
  -d|--extract-dat       Extract .dat assets only.
  -p|--extract-pack      Extract .pack assets only.
  -l|--list-assets       List the assets without extracting them.
  -f|--list-files        List the asset file paths without extracting them.
  -v|--validate-assets   Validate the assets without extracting them.
  -c|--count-assets      Count the assets without extracting them.
  -#|--display-table     Display listed information in a table.
  -s|--skip-existing     Skip assets that already exist.
  -n|--no-progress-bars  Don't show progress bars.
  -y|--answer-yes        Automatically answer yes to any question.
  -e|--debug             Show complete exception stack traces.
  -?|-h|--help           Show help information.
```

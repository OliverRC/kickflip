## Kickflip

So you got some GIT changes you want to get onto the server?
No worries you can kick those git changes and flip them over to that remote server!

### Usecases

Maybe you have a scripts folder that you want to maintain in git and deploy just the changes FTP when you are ready to release them.

### Using kickflip

Install of the tool using the following dotnet CLI command:

    dotnet tool install kickflip

If you are using it on your local development or would just like to have it installed once globally, use the following dotnet CLI command: 

    dotnet tool install --global kickflip


### Commandline reference 

At any time the help can be printed with `--help`. This works on the sub-commands too.

    kickflip --help

Or on a sub-command 

    kickflip deploy --help


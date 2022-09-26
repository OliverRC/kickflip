# Kickflip

![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/kickflip) [![Build](https://github.com/OliverRC/kickflip/actions/workflows/main.yml/badge.svg)](https://github.com/OliverRC/kickflip/actions/workflows/main.yml)

So you got some GIT changes you want to get onto the server?
No worries! You can kick those git changes and flip them over to that remote server!

## Use Cases

Maybe you have a scripts folder that maintain in git but are deploying manually.
Kickflip can help automate determining what files have change and deploy them to the remote server.

## Using kickflip

Install of the tool using the following dotnet CLI command:

    dotnet tool install kickflip

If you are using it on your local development or would just like to have it installed once globally, use the following dotnet CLI command: 

    dotnet tool install --global kickflip

## Github Notes

Github Actions `actions/checkout@v3` by default performs a shallow clone of the repo. In order for kickflip to work out all the changes it requires that a full clone be made. This can be achieve by:

```yaml
- uses: actions/checkout@v3
  with:
    fetch-depth: 0 # avoid shallow clone so kickflip can do its work.
```

## Commandline reference 

At any time the help can be printed with `--help`. This works on the sub-commands too.

    kickflip --help

Or on a sub-command 

    kickflip deploy --help

## Development

Kickflip uses [dotnet/Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) to handle semantic versioning and branching to for releases.
To prepare a new release the following command must be run.

    nbgv prepare-release


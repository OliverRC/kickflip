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

Once installed you can run the tool from the command line.

### Tags (e.g Github Release)
Default which uses mode = Tags (e.g GitHub Releases)

    kickflip deploy --hostname <ftp-hostname> --port <ftp-port (24)> --username <ftp-username> --password <ftp-password>

Or Tags

    kickflip deploy --mode Tags --hostname <ftp-hostname> --port <ftp-port (24)> --username <ftp-username> --password <ftp-password>

### GitHubMergePr

This tries to work out the changes between two PR merges. Useful for rapid deployment scenarios where PR's are used and you don't need to bundle multiple merges together.

    kickflip deploy --mode GitHubMergePr --hostname <ftp-hostname> --port <ftp-port (24)> --username <ftp-username> --password <ftp-password>

### Folder 

You may want to statically upload the contents of a folder. For example maybe there is some build assets produced on on your CI/CD server.

    kickflip deploy --mode Folder --deployment-path /public_html --hostname <ftp-hostname> --port <ftp-port (24)> --username <ftp-username> --password <ftp-password> --folder <folder-path>

**Note**: `--deployment-path` defaults to `/` however it is strongly recommended to set this to ensure you know where the files are going.

**Caveat**: To ignore files the `.kickflipignore` file must be present IN the folder.

## Github Actions

Github Actions `actions/checkout@v4` by default performs a shallow clone of the repo. In order for kickflip to work out all the changes it requires that a full clone be made. This can be achieve by:

```yaml
- uses: actions/checkout@v4
  with:
    fetch-depth: 0 # avoid shallow clone so kickflip can do its work.
```

If scripting for running in a CI build where global impact from installing a tool is undesirable, you can localize the tool installation:

```yaml
- name: Install kickflip
  run: dotnet tool install --tool-path ./tools kickflip
  
- name: Use kickflip
  run: ./tools/kickflip <options>
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


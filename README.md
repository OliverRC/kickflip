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

### Pull request comments

Kickflip can post a summary of the changes that will be deployed as a comment on a pull request:

    kickflip github pull-request --repo <owner>/<repo> --ref <refs/pull/<pr_number>/merge> --token <github-token>

Kickflip reuses a single pull request comment. Each time it runs it updates the existing comment instead of creating a new one.

If you have multiple kickflip flows in the same workflow (for example deploying to staging and production), pass `--action-name` so each flow keeps its own section within the shared comment:

    kickflip github pull-request --repo <owner>/<repo> --ref <ref> --token <token> --action-name staging
    kickflip github pull-request --repo <owner>/<repo> --ref <ref> --token <token> --action-name production

When running inside GitHub Actions, kickflip also writes the deployment change summary to the [job summary](https://github.blog/2022-05-09-supercharging-github-actions-with-job-summaries/) so it appears on the workflow run page. This happens automatically whenever the `GITHUB_STEP_SUMMARY` environment variable is present (which GitHub Actions sets for every step), in addition to posting the pull request comment.

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

## Testing

Kickflip has an automated test suite covering both unit and integration levels:

- **Unit tests** exercise the individual services (`GitService`, `FileSystemService`, `IgnoreService`, `OutputService`, `PullRequestCommentComposer`, `Utilities`) directly. Git based tests build real temporary git repositories so the find modes are proven end-to-end.
- **Integration tests** drive the compiled CLI as an external process, verifying command wiring, argument validation, the different deployment modes (`Tags`, `GitHubMergePR`, `Folder`) and that a dry run reports the planned changes without ever connecting to the remote server.

Run the whole suite with:

    dotnet test

## Development

Build and test the solution with the .NET 8 SDK:

```sh
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

## Releasing

Releases are cut directly from `main` using GitHub Releases:

1. Ensure the commit to release has been merged into `main` and the build is passing.
2. Create a new GitHub Release targeting that commit.
3. Give the release a semantic-version tag such as `v2.1.0` or `v2.2.0-beta.1`.
4. Publish the release.

Publishing the GitHub Release runs the release workflow. It validates that the tag points to a commit on `main`, removes an optional leading `v`, stamps the NuGet package and compiled binaries with that version, verifies the stamped version, and publishes the package to NuGet.

The repository's `NUGET_APIKEY` Actions secret must contain a NuGet API key allowed to publish the `kickflip` package. Draft releases do not publish until they are published, and editing an already-published release does not republish it.


# FBAPayara

A wrapper around payara's `asadmin` to make deploying FBA JEE services more convenient.

## Usage

`Service` has a hardcoded reference to the root directory for all my FBA apps. You'll need to change it to yours (most commonly C:/programming, I think).

Run the FBAPayaraD project with `dotnet run` as a background process. Then, use the `FBAPayara` project as a CLI client.

Commands:

 - `list` shows all deployed applications
 - `deploy`, `undeploy`, and `redeploy` run that command, along with a service from the following list:
     - Schedule
     - Visits
     - ProposalLookup
     - Users

## Advantages over `asadmin`

 - When a service is deployed, the time and checked out git branch for the repo are saved (in ~/AppData/Local/fba-payara/data). These are included in the `list` output.
 - All commands and services can be shortened to any unique prefix ie. `./fba-payara r s` will redeploy the scheduler, or `./fba-payara li` will show applications.
 - When using the built executable, generally slightly faster to respond than `asadmin`, due to avoiding JVM startup time. Note that this is _not_ the case when using `dotnet run`, because the dotnet tool has significant startup time as well.

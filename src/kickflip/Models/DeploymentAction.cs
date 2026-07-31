namespace kickflip.Models;

// Declaration order is deployment order: adds, then modifies, then deletes, then ignores.
// See Utilities.OrderForDeployment.
public enum DeploymentAction
{
    Add,
    Modify,
    AddOrModify,
    Delete,
    Ignore
}
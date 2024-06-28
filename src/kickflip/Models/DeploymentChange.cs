using kickflip.Enums;

namespace kickflip.Models;

public record DeploymentChange(DeploymentAction Action, Source Source, string Path, string DeploymentPath);
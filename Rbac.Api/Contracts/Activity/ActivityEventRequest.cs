using System.ComponentModel.DataAnnotations;

namespace Rbac.Api.Contracts.Activity;

public sealed record ActivityEventRequest(
    [Required] string Type,
    [Required] string Status,
    [Required] string Label,
    string? Description);

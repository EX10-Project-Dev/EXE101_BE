using System;

namespace BLL.DTOs.Responses;

public sealed class CategoryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}

public sealed class AmenityResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}

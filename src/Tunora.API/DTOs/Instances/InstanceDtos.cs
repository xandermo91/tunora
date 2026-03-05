using System.ComponentModel.DataAnnotations;

namespace Tunora.API.DTOs.Instances;

public record CreateInstanceDto(
    [Required, MaxLength(200)] string Name,
    [Required, MaxLength(500)] string Location
);

public record UpdateInstanceDto(
    [Required, MaxLength(200)] string Name,
    [Required, MaxLength(500)] string Location
);

/// <summary>Returned on all reads — no ConnectionKey.</summary>
public record InstanceDto(
    int Id,
    string Name,
    string Location,
    string Status,
    int? ActiveChannelId,
    string? CurrentTrackTitle,
    string? CurrentTrackArtist,
    DateTime? LastSeenAt,
    DateTime CreatedAt,
    List<AssignedChannelDto> Channels
);

/// <summary>Returned only on POST /instances — includes the secret ConnectionKey.</summary>
public record InstanceCreatedDto(
    int Id,
    string Name,
    string Location,
    string ConnectionKey,
    string Status,
    DateTime CreatedAt,
    List<AssignedChannelDto> Channels
);

/// <summary>Returned from GET /instances/{id}/connection-key — explicit admin action.</summary>
public record ConnectionKeyDto(string ConnectionKey);

public record AssignedChannelDto(
    int ChannelId,
    string Name,
    string IconName,
    string AccentColor,
    int SortOrder
);

public record AssignChannelDto(
    [Required] int ChannelId
);

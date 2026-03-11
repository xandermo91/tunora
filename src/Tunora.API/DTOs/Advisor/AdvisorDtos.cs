using System.ComponentModel.DataAnnotations;

namespace Tunora.API.DTOs.Advisor;

public record MusicAdviceDto(
    [Required][MinLength(1)][MaxLength(200)] string BusinessType,
    [MaxLength(500)] string? Description);

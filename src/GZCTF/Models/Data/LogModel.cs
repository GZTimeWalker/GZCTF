﻿using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models;

public class LogModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTimeOffset TimeUTC { get; set; }

    [Required]
    [MaxLength(50)]
    public string Level { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Logger { get; set; } = string.Empty;

    [MaxLength(25)]
    public string? RemoteIP { get; set; }

    [MaxLength(25)]
    public string? UserName { get; set; }

    public string Message { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Status { get; set; }

    public string? Exception { get; set; }
}
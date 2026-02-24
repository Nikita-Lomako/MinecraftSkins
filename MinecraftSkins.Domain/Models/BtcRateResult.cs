using System;

namespace MinecraftSkins.Domain.Models;

public class BtcRateResult
{
    public decimal Rate { get; set; }
    public DateTime AsOfUtc { get; set; }
    public string Source { get; set; } = string.Empty; // "Cache", "External", "Fallback"
    public int? AgeSeconds { get; set; }
}


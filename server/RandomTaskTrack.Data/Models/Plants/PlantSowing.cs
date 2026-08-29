namespace RandomTaskTrack.Data.Models.Plants;

/// <summary>
/// How to get a seed packet into the ground and out the other side. Part of the
/// profile, filled in only for <see cref="Enums.PlantKind.SeedPacket"/>.
///
/// The numbers are what a packet prints on its back — but they come from the
/// lookup, not from reading the packet, because a phone photo of small print on
/// a foil sachet is not something to trust. The photo is for the variety name.
/// </summary>
public class PlantSowing
{
    /// <summary>"Sow indoors in trays, two seeds per cell, thin to the stronger."</summary>
    public string Method { get; set; } = "";

    /// <summary>"March to early May" — prose, because it depends where you are.</summary>
    public string SowWindow { get; set; } = "";

    public int? SowDepthMm { get; set; }
    public int? SpacingCm { get; set; }
    public int? GerminationDays { get; set; }
    public int? DaysToHarvest { get; set; }

    /// <summary>Whether it starts inside, which is what makes hardening off a step.</summary>
    public bool StartIndoors { get; set; }

    public string Notes { get; set; } = "";

    /// <summary>
    /// The plan, as offsets from whichever day it actually gets sown. The lookup
    /// writes these rather than the app assembling a fixed sow → thin →
    /// transplant → harvest chain, because the chain genuinely differs: chillies
    /// get potted on twice and carrots are never transplanted at all.
    /// </summary>
    public List<PlantSowingStep> Steps { get; set; } = new();
}

public class PlantSowingStep
{
    public string Title { get; set; } = "";

    /// <summary>Days after sowing. 0 is the sowing itself.</summary>
    public int DayOffset { get; set; }

    public string Notes { get; set; } = "";
}

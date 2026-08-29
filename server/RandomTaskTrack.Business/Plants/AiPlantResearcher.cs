using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Data.Models.Ai;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Plants;
using RandomTaskTrack.Interfaces.Ai;
using RandomTaskTrack.Interfaces.Plants;

namespace RandomTaskTrack.Business.Plants;

/// <summary>
/// The lookup, as one completion — no tools of ours, no history, no
/// conversation to persist. Web search is on: a packet says "Sungold F1" and
/// nothing else, and the sowing depth for that cultivar is on the internet
/// rather than reliably in a model's memory.
///
/// The reply is asked for as JSON and parsed rather than routed through a tool
/// call, because the provider interface takes tool definitions but has no way
/// to *force* one: a model that answered in prose would need handling anyway,
/// so this handles it in the one place instead of pushing tool_choice through
/// IAiProvider for a single caller.
/// </summary>
public class AiPlantResearcher : IPlantResearcher
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>A profile is a page of prose, not an essay. Enough headroom that
    /// a long answer finishes rather than being truncated into invalid JSON.</summary>
    private const int MaxTokens = 3000;

    /// <summary>A stage read is two fields. It does not need room to ramble.</summary>
    private const int StageMaxTokens = 500;

    /// <summary>Watering weekly through repotting yearly covers everything a
    /// houseplant needs; the same bound the request validator applies.</summary>
    private const int MinIntervalDays = 1;
    private const int MaxIntervalDays = 365;

    /// <summary>Enough for water, feed, dust, turn, repot. Beyond that a plant
    /// is a full-time job and the schedule stops being read.</summary>
    private const int MaxCareTasks = 6;

    /// <summary>Sow, germinate, pot on, harden off, plant out, feed, harvest.</summary>
    private const int MaxSowingSteps = 8;

    /// <summary>Three years, matching CreateSowingPlanRequestValidator.</summary>
    private const int MaxDayOffset = 1095;

    private const string SharedRules = """
        Reply with a single JSON object and nothing else. No prose before or after it, no code fences.

        Rules:

        - Identify it as precisely as the evidence allows, and set `confidence` honestly. A wrong identification stated confidently gets a plant killed. If it fits several plants, name the most likely, say so in `reasoning`, and use "low" or "medium".
        - Search the web when you are not sure, and for anything cultivar-specific — sowing depths, spacings and days-to-harvest vary by variety and are printed on packets, not derivable from the species.
        - Every text field is one or two sentences. This is read on a wall tablet, standing up.
        - Metric units. Celsius.
        - Where the plant lives changes the answer — a north window is not a south one. Use the location you are given.
        - Leave a field as an empty string when you genuinely have nothing useful to say. Do not pad it.
        """;

    private const string PlantPrompt = $$"""
        You identify plants from a photo and a short description by their owner, and write a care profile for them.

        The person describing the plant usually does not know what it is. Work from whatever they give you — a species name, the shape of the leaves, where it was bought, how it behaves, and the photo if there is one. The photo outranks the description when they disagree: people misremember what they were told at the garden centre.

        Use these keys exactly:

        {
          "speciesCommon": "Fiddle-leaf fig",
          "speciesLatin": "Ficus lyrata",
          "confidence": "high | medium | low",
          "reasoning": "Why you settled on that, and what would tell it apart from the likely alternatives. Empty when confidence is high.",
          "summary": "Two or three sentences on what the plant is and what it wants.",
          "light": "",
          "water": "How much, how often, and how to tell it is thirsty.",
          "humidity": "",
          "temperature": "",
          "soil": "",
          "feeding": "",
          "repotting": "",
          "toxicity": "Cats, dogs, children. Say plainly if it is harmless.",
          "commonProblems": ["Yellow lower leaves usually means overwatering", "..."],
          "careTasks": [{"title": "Water", "intervalDays": 7, "notes": "Half as often from November to February."}]
        }

        `careTasks` is the actual schedule: two to five entries, watering first, each a real recurring job with a realistic interval in days. Put seasonal adjustments in `notes` rather than inventing a second task for winter.

        {{SharedRules}}
        """;

    private const string SeedPrompt = $$"""
        You are looking at a seed packet, or at what its owner could tell you about one, and you write the plan for growing it.

        **What you need from the photo is the variety name**, and anything else legible on it. Assume the photo is a phone snap of a foil sachet: the small print on the back will not be readable, and you should not pretend it is. Get the name, then look the rest up — sowing depth, spacing, germination time and days to harvest for that specific variety are published, and searching for them beats guessing from the species.

        If the packet names a cultivar ("Sungold F1", "Boltardy"), search for that cultivar. If you can only make out the crop, say so in `reasoning` and give the figures for the crop in general.

        Use these keys exactly:

        {
          "speciesCommon": "Tomato 'Sungold'",
          "speciesLatin": "Solanum lycopersicum",
          "confidence": "high | medium | low",
          "reasoning": "What you could and could not read, and where the numbers came from.",
          "summary": "Two or three sentences: what it is, what it tastes like or looks like, how hard it is.",
          "light": "",
          "water": "",
          "humidity": "",
          "temperature": "Germination temperature as well as growing temperature.",
          "soil": "",
          "feeding": "",
          "repotting": "Potting on, for something raised in modules.",
          "toxicity": "",
          "commonProblems": ["Leggy seedlings mean not enough light", "..."],
          "careTasks": [{"title": "Water", "intervalDays": 3, "notes": "Seedlings dry out faster than established plants."}],
          "sowing": {
            "method": "Sow indoors in modules, two seeds per cell, thin to the stronger.",
            "sowWindow": "March to early May",
            "sowDepthMm": 6,
            "spacingCm": 45,
            "germinationDays": 10,
            "daysToHarvest": 100,
            "startIndoors": true,
            "notes": "Needs warmth to germinate — a windowsill above a radiator is enough.",
            "steps": [
              {"title": "Sow", "dayOffset": 0, "notes": "6mm deep, two per module."},
              {"title": "Expect germination", "dayOffset": 10, "notes": "Thin to the stronger seedling."},
              {"title": "Pot on", "dayOffset": 28, "notes": "Into 9cm pots once two true leaves are up."},
              {"title": "Harden off", "dayOffset": 56, "notes": "A week of days outside before planting out."},
              {"title": "Plant out", "dayOffset": 63, "notes": "45cm apart, after the last frost."},
              {"title": "First harvest expected", "dayOffset": 100, "notes": ""}
            ]
          }
        }

        `sowing.steps` is the plan, as offsets in days from whichever day it actually gets sown — `dayOffset` 0 is the sowing itself. Give the real chain for this crop: some are direct-sown and never potted on, some are transplanted twice. Every step must be something a person does (or watches for) on a day.

        `careTasks` is what it needs repeatedly once it is up, as intervals in days.

        {{SharedRules}}
        """;

    private const string StagePrompt = """
        You are shown a photo of a plant that someone is tracking, and told what it is.

        Say what stage it has reached, and how it looks. Reply with a single JSON object and nothing else:

        {
          "stage": "Two or three words — 'first true leaves', 'starting to flower', 'dormant'.",
          "note": "One sentence: how it is doing, and what is coming next. Say plainly if something looks wrong — yellowing, stretching for light, dry soil, pests."
        }

        Be specific to what you can actually see. If the photo is too dark or too blurry to judge, say that in `note` and leave `stage` empty rather than guessing.
        """;

    private readonly IAiProvider _provider;
    private readonly ILogger<AiPlantResearcher> _logger;

    public AiPlantResearcher(IAiProvider provider, ILogger<AiPlantResearcher> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public async Task<PlantResearchResult> ResearchAsync(PlantResearchQuestion question, CancellationToken cancellationToken)
    {
        bool isSeed = question.Kind == PlantKind.SeedPacket;

        var request = new AiRequest
        {
            SystemPrompt = isSeed ? SeedPrompt : PlantPrompt,
            MaxTokens = MaxTokens,
            AllowWebSearch = true,
            Messages = [BuildQuestion(question)]
        };

        AiResponse response = await CompleteAsync(request, cancellationToken);

        PlantProfile profile = Parse<PlantProfile>(response.Content);

        profile.Confidence = profile.Confidence.Trim().ToLowerInvariant();
        profile.CareTasks = CleanCareTasks(profile.CareTasks);
        profile.Sowing = isSeed ? CleanSowing(profile.Sowing) : null;

        return new PlantResearchResult { Profile = profile, Model = response.Model };
    }

    public async Task<PlantStageRead> ReadStageAsync(Plant what, AiImage image, CancellationToken cancellationToken)
    {
        var request = new AiRequest
        {
            SystemPrompt = StagePrompt,
            MaxTokens = StageMaxTokens,

            // No search here: the question is about this photo, not about the
            // species, and a search would double the cost of every upload.
            Messages = [AiMessage.FromUser(DescribeForStage(what), [image])]
        };

        AiResponse response = await CompleteAsync(request, cancellationToken);

        PlantStageRead read = Parse<PlantStageRead>(response.Content);

        read.Stage = read.Stage.Trim();
        read.Note = read.Note.Trim();

        return read;
    }

    private async Task<AiResponse> CompleteAsync(AiRequest request, CancellationToken cancellationToken)
    {
        // A provider failure (no key, provider down) is already an
        // AiProviderException and travels up as one — the caller decides
        // whether that is fatal.
        AiResponse response = await _provider.CompleteAsync(request, cancellationToken);

        if (response.StopReason == AiStopReason.Refusal)
        {
            throw new AiProviderException(
                "The lookup declined to answer.",
                ExceptionCodes.PLANT_RESEARCH_FAILED);
        }

        return response;
    }

    private static AiMessage BuildQuestion(PlantResearchQuestion question)
    {
        var builder = new StringBuilder();

        builder.AppendLine(question.Kind == PlantKind.SeedPacket
            ? "This is a seed packet I have not sown yet."
            : "This is a plant I have.");

        builder.AppendLine($"I call it: {question.Name}");

        if (!string.IsNullOrWhiteSpace(question.Location))
        {
            builder.AppendLine($"Where it lives: {question.Location}");
        }

        // Falls back to the name when there is nothing else — for "Monstera" in
        // the name box that is the whole question, and asking for a description
        // twice would be the wrong thing to insist on.
        builder.AppendLine(string.IsNullOrWhiteSpace(question.Description)
            ? "That is all I can tell you about it."
            : $"What I know about it: {question.Description}");

        return question.Image is null
            ? AiMessage.FromUser(builder.ToString())
            : AiMessage.FromUser(builder.ToString(), [question.Image]);
    }

    private static string DescribeForStage(Plant what)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"This is: {what.Name}");

        if (!string.IsNullOrWhiteSpace(what.Species))
        {
            builder.AppendLine($"Species: {what.Species}{(string.IsNullOrWhiteSpace(what.LatinName) ? "" : $" ({what.LatinName})")}");
        }

        if (!string.IsNullOrWhiteSpace(what.Location))
        {
            builder.AppendLine($"Where it lives: {what.Location}");
        }

        builder.AppendLine("How is it doing?");

        return builder.ToString();
    }

    private T Parse<T>(string? content) where T : new()
    {
        string json = ExtractJson(content);

        try
        {
            return JsonSerializer.Deserialize<T>(json, Options)
                   ?? throw new AiProviderException(
                       "The lookup came back empty.",
                       ExceptionCodes.PLANT_RESEARCH_FAILED);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Plant lookup returned something that was not {Shape}", typeof(T).Name);

            throw new AiProviderException(
                "The lookup came back in a shape this app could not read.",
                ExceptionCodes.PLANT_RESEARCH_FAILED,
                "Try again — the same question usually works on a second ask.");
        }
    }

    /// <summary>
    /// Models fence JSON in markdown often enough to be worth handling rather
    /// than failing on, and a search-backed answer sometimes arrives with a
    /// sentence in front of it. Anything outside the outermost braces is dropped.
    /// </summary>
    private static string ExtractJson(string? content)
    {
        string text = (content ?? string.Empty).Trim();

        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');

        return start >= 0 && end > start ? text[start..(end + 1)] : text;
    }

    /// <summary>
    /// These become recurrences, and a recurrence with a zero interval is a
    /// constraint violation rather than a bad suggestion. Clamped here so a
    /// stray value costs a sensible schedule, not a failed save.
    /// </summary>
    private static List<PlantCareTask> CleanCareTasks(List<PlantCareTask> tasks) =>
        tasks.Where(task => !string.IsNullOrWhiteSpace(task.Title))
             .Select(task => new PlantCareTask
             {
                 Title = task.Title.Trim(),
                 IntervalDays = Math.Clamp(task.IntervalDays, MinIntervalDays, MaxIntervalDays),
                 Notes = task.Notes.Trim()
             })
             .Take(MaxCareTasks)
             .ToList();

    /// <summary>Same job for the sowing plan, whose steps become dated tasks.</summary>
    private static PlantSowing? CleanSowing(PlantSowing? sowing)
    {
        if (sowing is null)
        {
            return null;
        }

        sowing.Steps = sowing.Steps
            .Where(step => !string.IsNullOrWhiteSpace(step.Title))
            .Select(step => new PlantSowingStep
            {
                Title = step.Title.Trim(),
                DayOffset = Math.Clamp(step.DayOffset, 0, MaxDayOffset),
                Notes = step.Notes.Trim()
            })
            .OrderBy(step => step.DayOffset)
            .Take(MaxSowingSteps)
            .ToList();

        return sowing;
    }
}

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using RandomTaskTrack.Data.Models.Ai;
using RandomTaskTrack.Data.Models.Constants;
using RandomTaskTrack.Data.Models.Enums;
using RandomTaskTrack.Data.Models.Exceptions;
using RandomTaskTrack.Data.Models.Learning;
using RandomTaskTrack.Interfaces.Ai;
using RandomTaskTrack.Interfaces.Learning;

namespace RandomTaskTrack.Business.Learning;

/// <summary>
/// The research half of the learning scope, as two one-shot completions that
/// answer in JSON — the same shape AiPlantResearcher settled on, and for the
/// same reason: the provider interface takes tool definitions but has no way to
/// *force* one, so a model that answered in prose would need handling anyway.
///
/// Web search is on for both. Everything asked here has a published answer that
/// moves: exam codes get retired, course catalogues change, prices change, and
/// renewal policy changes outright — Microsoft moved to annual free renewals in
/// 2022. A model answering from memory would be confidently a year or two stale.
/// </summary>
public class AiLearningResearcher : ILearningResearcher
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>A whole route with phases, certs, resources and projects. Enough
    /// headroom that a long answer finishes rather than being truncated into
    /// invalid JSON — and the answer is not all this pays for. Thinking counts
    /// against the same ceiling, and this model thinks before it writes, so a
    /// budget sized to the JSON alone gets spent before the JSON starts.</summary>
    private const int PlanMaxTokens = 16000;

    /// <summary>A renewal rule is a paragraph and a number.</summary>
    private const int CredentialMaxTokens = 1500;

    // Bounds. These become rows the user scrolls on a tablet, so the limits are
    // about what stays readable rather than about what is possible.
    private const int MaxPhases = 8;
    private const int MaxCertifications = 8;
    private const int MaxResources = 24;
    private const int MaxProjects = 8;
    private const int MaxBullets = 10;
    private const int MinWeeklyHours = 1;
    private const int MaxWeeklyHours = 60;
    private const int MaxPrepHours = 2000;
    private const int MaxPhaseWeeks = 260;

    /// <summary>
    /// One month to ten years. Outside that it is not a validity period — it is
    /// a misread date or a hallucinated number, and turning it into an expiry
    /// would be worse than admitting the lookup failed.
    /// </summary>
    private const int MinValidityMonths = 1;
    private const int MaxValidityMonths = 120;

    /// <summary>Five years of notice is not notice.</summary>
    private const int MaxWindowDays = 730;

    private const string PlanPrompt = """
        You draft learning and career paths for one person, and you are blunt about what they actually require.

        You are given a goal, why they want it, what they expect from it, roughly when they want to be ready, and whatever they could tell you about where they are starting from. Some goals are formal (certifications, a licence, a degree). Some are not (a language, a skill) — for those the first job is deciding what "prepared" even means, because a goal that cannot say when it is finished never finishes.

        Search the web. Exam codes get retired and replaced, course catalogues change, prices change, and a path built on a certification that no longer exists is worthless. Check what the current versions actually are before you recommend them.

        Reply with a single JSON object and nothing else. No prose before or after it, no code fences. Use these keys exactly:

        {
          "summary": "Two or three sentences: the shape of the route and how long it realistically takes.",
          "targetDefinition": "What 'prepared' means for this goal, concretely enough to test against. For a certification that is passing the exam. For a language it is a CEFR level and what that lets you do. For a skill, name the thing they should be able to do unaided.",
          "assumedLevel": "What you assumed about where they are starting. Say it plainly so a wrong assumption is obvious rather than silently baked into the plan.",
          "weeklyHours": 8,
          "prerequisites": ["What has to be true before the path can start. Be specific: 'linear algebra and probability to first-year undergraduate level', not 'good maths'."],
          "phases": [{"title": "", "weeks": 6, "focus": "What you work on.", "outcome": "What you can do at the end that you could not at the start."}],
          "certifications": [{"name": "", "issuer": "", "code": "AZ-305", "order": 1, "typicalCost": "€165", "prepHours": 120, "why": "Why this one rather than the neighbouring one.", "validity": "How long it lasts once earned."}],
          "resources": [{"title": "The exact title, as it is actually listed.", "kind": "course | book | lab | app | video | community | docs", "provider": "Udemy", "url": "", "cost": "", "why": "", "phase": 1}],
          "projects": [{"title": "", "level": "beginner | intermediate | advanced", "build": "What they actually build.", "proves": "What having built it demonstrates."}],
          "handsOn": ["Experience to go and collect that is not a project — a home lab, a CTF season, shadowing someone on call."],
          "risks": ["What usually derails this path."]
        }

        Rules:

        - **Pace it against the target date and the hours they have.** If what they want is not achievable in the time, say so in `summary` and give the plan that is — do not quietly compress it and let them find out.
        - **Do not recommend a certification they already hold.** They are listed for you.
        - `resources`: give the exact title as it is actually listed, and the provider. Those two find the thing; a URL may not, so a URL is a bonus and never the only handle. Do not invent one — leave it empty rather than guessing at a plausible link.
        - Sequence `certifications` with `order`, easiest useful one first. An entry-level cert passed in month two is worth more than an expert one attempted in month ten.
        - `projects` must span levels, and each one must build something specific. "A portfolio project" is not a project.
        - Prices and hours are estimates. Give real figures where you found them and round ones where you did not.
        - Every text field is a sentence or two. This is read on a wall tablet, standing up.
        - Metric units. Euros unless the thing is genuinely priced in something else.
        - Leave a field as an empty string, or a list empty, when you have nothing useful to say. Do not pad it.
        """;

    private const string CredentialPrompt = """
        You are told about a certification or licence someone already holds, and when they earned it. You find out whether it expires, and if so how it is renewed.

        Search the web and check the issuer's own current policy. This is the whole reason you are being asked: renewal rules change, and a remembered answer is routinely a year or two out of date.

        Reply with a single JSON object and nothing else. No prose before or after it, no code fences:

        {
          "renewalKind": "permanent | expires | unknown",
          "validityMonths": 12,
          "renewal": "How renewing actually works — an online assessment, CPE credits, resitting the exam.",
          "windowOpensDays": 180,
          "cost": "Free, or what it costs.",
          "ifLapsed": "What happens if it runs out — a grace period, or resitting the whole thing.",
          "officialUrl": "Where the issuer states this.",
          "notes": "Anything else worth knowing, including whether the programme has been retired."
        }

        Rules:

        - **"permanent" is a real and common answer, and you must be willing to give it.** Plenty of credentials never expire: legacy Microsoft certifications such as MCSD, MCSE and MCITP, CompTIA certifications earned before 2011, and many vendor certifications from that era. A retired programme is a special case of this — the exam may no longer be offered, but what was earned stays on the transcript and does not lapse. Say so in `notes` when that is the situation.
        - **Which rules apply usually depends on when it was earned, not on what the issuer does today.** A credential earned in 2014 under a lifetime policy is not retroactively put on a clock because the current version of that exam renews annually. Read the date you are given and answer for it.
        - `validityMonths` is a number of months from the date it was earned. Set it **only** when `renewalKind` is "expires". Leave it null for "permanent" and for "unknown" — a made-up number here becomes a wrong date on a reminder, which is worse than no reminder.
        - Use "unknown" when you genuinely cannot establish it. That is an honest answer and the app handles it. Guessing is not.
        - `windowOpensDays` is how many days before expiry renewal becomes possible. 0 if there is no such window.
        - Every text field is a sentence or two, read standing up.
        """;

    private readonly IAiProvider _provider;
    private readonly ILogger<AiLearningResearcher> _logger;

    public AiLearningResearcher(IAiProvider provider, ILogger<AiLearningResearcher> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public async Task<LearningPlanResult> DraftPlanAsync(LearningPlanQuestion question, CancellationToken cancellationToken)
    {
        var request = new AiRequest
        {
            SystemPrompt = PlanPrompt,
            MaxTokens = PlanMaxTokens,
            AllowWebSearch = true,
            Messages = [AiMessage.FromUser(DescribeGoal(question))]
        };

        AiResponse response = await CompleteAsync(request, cancellationToken);

        LearningPlan plan = Parse<LearningPlan>(response.Content);

        return new LearningPlanResult { Plan = Clean(plan), Model = response.Model };
    }

    public async Task<CredentialResearchResult> ResearchCredentialAsync(CredentialQuestion question, CancellationToken cancellationToken)
    {
        var request = new AiRequest
        {
            SystemPrompt = CredentialPrompt,
            MaxTokens = CredentialMaxTokens,
            AllowWebSearch = true,
            Messages = [AiMessage.FromUser(DescribeCredential(question))]
        };

        AiResponse response = await CompleteAsync(request, cancellationToken);

        CredentialAnswer answer = Parse<CredentialAnswer>(response.Content);

        return Reconcile(answer, response.Model);
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
                ExceptionCodes.LEARNING_RESEARCH_FAILED);
        }

        // Both of these reach Parse as text that will not deserialise, and Parse
        // can only report that the shape was wrong — it is handed a string and
        // the stop reason is already gone. Naming them here, while that is still
        // in hand, is what separates "raise the ceiling" from "ask again".
        if (response.StopReason == AiStopReason.MaxTokens)
        {
            _logger.LogWarning(
                "Learning lookup hit its {Ceiling}-token ceiling after {Output} output tokens",
                request.MaxTokens,
                response.Usage.OutputTokens);

            throw new AiProviderException(
                "The lookup ran past the length it is allowed.",
                ExceptionCodes.LEARNING_RESEARCH_FAILED,
                "Try again — a shorter brief usually fits.");
        }

        if (string.IsNullOrWhiteSpace(response.Content))
        {
            // Nothing to parse at all. The turn ended without the model ever
            // writing text — searching until the resume budget ran out does
            // this, and so does spending the whole ceiling on thinking.
            _logger.LogWarning(
                "Learning lookup returned no text; stopped on {StopReason} after {Output} output tokens",
                response.StopReason,
                response.Usage.OutputTokens);

            throw new AiProviderException(
                "The lookup came back empty.",
                ExceptionCodes.LEARNING_RESEARCH_FAILED,
                "Try again — the same question usually works on a second ask.");
        }

        return response;
    }

    private static string DescribeGoal(LearningPlanQuestion question)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"Today is {question.Today:yyyy-MM-dd}.");
        builder.AppendLine();
        builder.AppendLine($"My goal: {question.Title}");

        if (!string.IsNullOrWhiteSpace(question.Why))
        {
            builder.AppendLine($"Why I want it: {question.Why}");
        }

        if (!string.IsNullOrWhiteSpace(question.Benefits))
        {
            builder.AppendLine($"What I expect out of it: {question.Benefits}");
        }

        if (question.TargetOn.HasValue)
        {
            // The gap in months as well as the date: it is the number the
            // phases have to add up to, and making the model derive it is a
            // pointless place to let it slip a month.
            int months = MonthsBetween(question.Today, question.TargetOn.Value);

            builder.AppendLine($"I want to be prepared by: {question.TargetOn.Value:yyyy-MM-dd} — about {months} months from today.");
        }

        builder.AppendLine(string.IsNullOrWhiteSpace(question.Context)
            ? "I have not told you where I am starting from — assume something reasonable and say what you assumed."
            : $"Where I am starting from: {question.Context}");

        if (question.HeldCredentials.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("I already hold these, so do not put them in the plan:");

            foreach (string held in question.HeldCredentials)
            {
                builder.AppendLine($"- {held}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Draft me the path.");

        return builder.ToString();
    }

    private static string DescribeCredential(CredentialQuestion question)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"I hold: {question.Name}");

        if (!string.IsNullOrWhiteSpace(question.Issuer))
        {
            builder.AppendLine($"Issued by: {question.Issuer}");
        }

        if (!string.IsNullOrWhiteSpace(question.Code))
        {
            builder.AppendLine($"Exam or credential code: {question.Code}");
        }

        builder.AppendLine($"I earned it on: {question.EarnedOn:yyyy-MM-dd}");
        builder.AppendLine();
        builder.AppendLine("Does it expire, and if so how do I renew it? Answer for the policy that applies to a credential earned on that date.");

        return builder.ToString();
    }

    /// <summary>
    /// Turns the model's answer into a state the rest of the app can trust.
    ///
    /// The two failure modes that matter both end at Unknown rather than at a
    /// date: an answer that says "permanent" while also giving a validity
    /// period is contradicting itself, and an answer that says "expires"
    /// without a usable number has not actually answered. Writing a wrong
    /// expiry is worse than writing none — a reminder on the wrong date is
    /// trusted, and a missing one is noticed.
    /// </summary>
    private CredentialResearchResult Reconcile(CredentialAnswer answer, string? model)
    {
        CredentialRenewalKind kind = ParseKind(answer.RenewalKind);
        int? months = answer.ValidityMonths;

        bool usable = months is >= MinValidityMonths and <= MaxValidityMonths;

        if (kind == CredentialRenewalKind.Expires && !usable)
        {
            _logger.LogWarning(
                "Credential lookup said 'expires' with validityMonths {Months} — treating as unknown",
                months);

            kind = CredentialRenewalKind.Unknown;
        }

        if (kind == CredentialRenewalKind.Permanent && months.HasValue)
        {
            _logger.LogWarning(
                "Credential lookup said 'permanent' but also gave validityMonths {Months} — treating as unknown",
                months);

            kind = CredentialRenewalKind.Unknown;
        }

        return new CredentialResearchResult
        {
            RenewalKind = kind,
            Renewal = new CredentialRenewal
            {
                // Carried only where it means something. A validity period on a
                // permanent or unknown row is what would put the expiry back.
                ValidityMonths = kind == CredentialRenewalKind.Expires ? months : null,
                Renewal = Trim(answer.Renewal),
                WindowOpensDays = Math.Clamp(answer.WindowOpensDays, 0, MaxWindowDays),
                Cost = Trim(answer.Cost),
                IfLapsed = Trim(answer.IfLapsed),
                OfficialUrl = Trim(answer.OfficialUrl),
                Notes = Trim(answer.Notes)
            },
            Model = model
        };
    }

    /// <summary>
    /// Bounds everything the UI has to render and the user has to scroll. A
    /// stray value costs a tidy plan, not a failed save — the same bargain
    /// CleanCareTasks makes with a stray watering interval.
    /// </summary>
    private static LearningPlan Clean(LearningPlan plan)
    {
        plan.Summary = Trim(plan.Summary);
        plan.TargetDefinition = Trim(plan.TargetDefinition);
        plan.AssumedLevel = Trim(plan.AssumedLevel);
        plan.WeeklyHours = plan.WeeklyHours <= 0
            ? 0
            : Math.Clamp(plan.WeeklyHours, MinWeeklyHours, MaxWeeklyHours);

        plan.Prerequisites = CleanBullets(plan.Prerequisites);
        plan.HandsOn = CleanBullets(plan.HandsOn);
        plan.Risks = CleanBullets(plan.Risks);

        plan.Phases = plan.Phases
            .Where(phase => !string.IsNullOrWhiteSpace(phase.Title))
            .Select(phase => new LearningPhase
            {
                Title = Trim(phase.Title),
                Weeks = Math.Clamp(phase.Weeks, 0, MaxPhaseWeeks),
                Focus = Trim(phase.Focus),
                Outcome = Trim(phase.Outcome)
            })
            .Take(MaxPhases)
            .ToList();

        plan.Certifications = plan.Certifications
            .Where(cert => !string.IsNullOrWhiteSpace(cert.Name))
            .Select(cert => new LearningCertificationSuggestion
            {
                Name = Trim(cert.Name),
                Issuer = Trim(cert.Issuer),
                Code = Trim(cert.Code),
                Order = Math.Max(cert.Order, 0),
                TypicalCost = Trim(cert.TypicalCost),
                PrepHours = Math.Clamp(cert.PrepHours, 0, MaxPrepHours),
                Why = Trim(cert.Why),
                Validity = Trim(cert.Validity)
            })
            .OrderBy(cert => cert.Order == 0 ? int.MaxValue : cert.Order)
            .Take(MaxCertifications)
            .ToList();

        plan.Resources = plan.Resources
            .Where(resource => !string.IsNullOrWhiteSpace(resource.Title))
            .Select(resource => new LearningResource
            {
                Title = Trim(resource.Title),
                Kind = Trim(resource.Kind).ToLowerInvariant(),
                Provider = Trim(resource.Provider),
                Url = Trim(resource.Url),
                Cost = Trim(resource.Cost),
                Why = Trim(resource.Why),
                Phase = Math.Clamp(resource.Phase, 0, MaxPhases)
            })
            .Take(MaxResources)
            .ToList();

        plan.Projects = plan.Projects
            .Where(project => !string.IsNullOrWhiteSpace(project.Title))
            .Select(project => new LearningProject
            {
                Title = Trim(project.Title),
                Level = Trim(project.Level).ToLowerInvariant(),
                Build = Trim(project.Build),
                Proves = Trim(project.Proves)
            })
            .Take(MaxProjects)
            .ToList();

        return plan;
    }

    private static List<string> CleanBullets(List<string> lines) =>
        lines.Where(line => !string.IsNullOrWhiteSpace(line))
             .Select(Trim)
             .Take(MaxBullets)
             .ToList();

    private static string Trim(string? value) => (value ?? string.Empty).Trim();

    private static CredentialRenewalKind ParseKind(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "permanent" => CredentialRenewalKind.Permanent,
        "expires" => CredentialRenewalKind.Expires,
        _ => CredentialRenewalKind.Unknown
    };

    /// <summary>
    /// Whole months, rounded down. Only used to tell the model how long it has
    /// to work with, so day-level precision would be false precision.
    /// </summary>
    private static int MonthsBetween(DateOnly from, DateOnly to)
    {
        int months = ((to.Year - from.Year) * 12) + to.Month - from.Month;

        return to.Day < from.Day ? Math.Max(months - 1, 0) : Math.Max(months, 0);
    }

    private T Parse<T>(string? content) where T : new()
    {
        string json = ExtractJson(content);

        try
        {
            return JsonSerializer.Deserialize<T>(json, Options)
                   ?? throw new AiProviderException(
                       "The lookup came back empty.",
                       ExceptionCodes.LEARNING_RESEARCH_FAILED);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Learning lookup returned something that was not {Shape}", typeof(T).Name);

            throw new AiProviderException(
                "The lookup came back in a shape this app could not read.",
                ExceptionCodes.LEARNING_RESEARCH_FAILED,
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
    /// The renewal reply as it arrives, before it is reconciled. Separate from
    /// <see cref="CredentialRenewal"/> because the kind is a string on the wire
    /// and an enum afterwards, and because what gets stored is the reconciled
    /// answer rather than the raw one.
    /// </summary>
    private sealed class CredentialAnswer
    {
        [JsonPropertyName("renewalKind")]
        public string RenewalKind { get; set; } = "";

        public int? ValidityMonths { get; set; }
        public string Renewal { get; set; } = "";
        public int WindowOpensDays { get; set; }
        public string Cost { get; set; } = "";
        public string IfLapsed { get; set; } = "";
        public string OfficialUrl { get; set; } = "";
        public string Notes { get; set; } = "";
    }
}

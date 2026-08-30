import { StepKinds } from '../types';
import type {
  LearningCertificationSuggestion,
  LearningPlan,
  LearningProject,
  LearningResource,
  LearningStepKind,
  StepInput,
} from '../types';

const empty: StepInput = {
  title: '',
  kind: StepKinds.Study,
  targetOn: null,
  notes: null,
  provider: null,
  url: null,
  cost: null,
  hours: null,
};

const certificationStep = (cert: LearningCertificationSuggestion): StepInput => ({
  ...empty,
  title: cert.code === '' ? cert.name : `${cert.name} (${cert.code})`,
  kind: StepKinds.Certification,
  notes: [cert.why, cert.validity && `Valid: ${cert.validity}`].filter(Boolean).join(' '),
  provider: cert.issuer || null,
  cost: cert.typicalCost || null,
  hours: cert.prepHours > 0 ? cert.prepHours : null,
});

const resourceStep = (resource: LearningResource): StepInput => ({
  ...empty,
  title: resource.title,

  // A named course is a course; a book, a lab or a community is something you
  // work through, which is what Study means here.
  kind: resource.kind === 'course' ? StepKinds.Course : StepKinds.Study,
  notes: resource.why,
  provider: resource.provider || null,
  url: resource.url || null,
  cost: resource.cost || null,
});

const projectStep = (project: LearningProject): StepInput => ({
  ...empty,
  title: project.title,
  kind: StepKinds.Project,
  notes: [project.build, project.proves && `Shows: ${project.proves}`].filter(Boolean).join(' '),
});

export default function PlanPanel({
  plan,
  researchedAt,
  taken,
  onAdd,
  busy,
}: {
  plan: LearningPlan;
  researchedAt: string | null;
  /** Lowercased titles already on the path — the server dedupes on the same key. */
  taken: Set<string>;
  onAdd: (steps: StepInput[]) => void;
  busy: boolean;
}) {
  /** One line of the plan, with the button that commits it. */
  const line = (input: StepInput, body: React.ReactNode, meta?: string, kind?: LearningStepKind) => {
    const already = taken.has(input.title.toLowerCase());

    return (
      <li key={input.title}>
        <div className="suggestion">
          <div>
            <strong>{input.title}</strong>
            {meta && <span className="muted"> — {meta}</span>}
            {body}
          </div>

          <button
            type="button"
            className="ghost"
            onClick={() => onAdd([{ ...input, kind: kind ?? input.kind }])}
            disabled={busy || already}
          >
            {already ? 'On the path' : 'Add to path'}
          </button>
        </div>
      </li>
    );
  };

  return (
    <div className="plan">
      <h3>The drafted path</h3>

      {plan.summary !== '' && <p>{plan.summary}</p>}

      {/* What "prepared" means. For a language goal this is the whole answer to
          "what level am I aiming at", so it gets its own line rather than
          being buried in the summary. */}
      {plan.targetDefinition !== '' && (
        <p className="target-def">
          <strong>Done means:</strong> {plan.targetDefinition}
        </p>
      )}

      {(plan.assumedLevel !== '' || plan.weeklyHours > 0) && (
        <p className="muted">
          {plan.assumedLevel !== '' && <>Assumed: {plan.assumedLevel} </>}
          {plan.weeklyHours > 0 && <>Paced at about {plan.weeklyHours} hours a week.</>}
        </p>
      )}

      {plan.prerequisites.length > 0 && (
        <>
          <h4>Before you start</h4>
          <ul className="bullets">
            {plan.prerequisites.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        </>
      )}

      {plan.phases.length > 0 && (
        <>
          <h4>Phases</h4>
          <ol className="phases">
            {plan.phases.map((phase) => (
              <li key={phase.title}>
                <strong>{phase.title}</strong>
                {phase.weeks > 0 && <span className="muted"> — about {phase.weeks} weeks</span>}
                {phase.focus !== '' && <p>{phase.focus}</p>}
                {phase.outcome !== '' && <p className="muted">Ends with: {phase.outcome}</p>}
              </li>
            ))}
          </ol>
        </>
      )}

      {plan.certifications.length > 0 && (
        <>
          <h4>Certifications</h4>
          <ul className="suggestions">
            {plan.certifications.map((cert) =>
              line(
                certificationStep(cert),
                <>
                  {cert.why !== '' && <p>{cert.why}</p>}
                  <p className="muted">
                    {[
                      cert.typicalCost && `Around ${cert.typicalCost}`,
                      cert.prepHours > 0 && `~${cert.prepHours} hours of prep`,
                      cert.validity && `Valid ${cert.validity}`,
                    ]
                      .filter(Boolean)
                      .join(' · ')}
                  </p>
                </>,
                cert.issuer,
              ),
            )}
          </ul>
        </>
      )}

      {plan.resources.length > 0 && (
        <>
          <h4>What to learn with</h4>
          <ul className="suggestions">
            {plan.resources.map((resource) =>
              line(
                resourceStep(resource),
                <>
                  {resource.why !== '' && <p>{resource.why}</p>}
                  <p className="muted">
                    {[resource.kind, resource.cost].filter(Boolean).join(' · ')}
                    {/* The link is best-effort — provider and exact title are
                        what actually find it, so they lead and this trails. */}
                    {resource.url !== '' && (
                      <>
                        {' '}
                        <a href={resource.url} target="_blank" rel="noreferrer">
                          link
                        </a>
                      </>
                    )}
                  </p>
                </>,
                resource.provider,
              ),
            )}
          </ul>
        </>
      )}

      {plan.projects.length > 0 && (
        <>
          <h4>Projects</h4>
          <ul className="suggestions">
            {plan.projects.map((project) =>
              line(
                projectStep(project),
                <>
                  {project.build !== '' && <p>{project.build}</p>}
                  {project.proves !== '' && <p className="muted">Shows: {project.proves}</p>}
                </>,
                project.level,
              ),
            )}
          </ul>
        </>
      )}

      {plan.handsOn.length > 0 && (
        <>
          <h4>Experience to collect</h4>
          <ul className="bullets">
            {plan.handsOn.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        </>
      )}

      {plan.risks.length > 0 && (
        <>
          <h4>What usually derails this</h4>
          <ul className="bullets">
            {plan.risks.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        </>
      )}

      {researchedAt && (
        <p className="muted drafted">Drafted {researchedAt.slice(0, 10)}. Prices and course listings move.</p>
      )}
    </div>
  );
}

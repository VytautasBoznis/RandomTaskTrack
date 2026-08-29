import { useState } from 'react';
import {
  addPlantPhoto,
  completeTask,
  createPlantSchedule,
  createSowingPlan,
  deletePlant,
  deletePlantPhoto,
  deleteRecurrence,
  researchPlant,
} from '../api';
import { prepare } from '../photo';
import PlantPhotoImg from './PlantPhotoImg';
import { PlantKinds, TaskStatus } from '../types';
import type { Plant, PlantProfile, TaskItemStatus, TaskListItem } from '../types';

/**
 * Which care line or sowing step a task or schedule came from. The server
 * stashes it in the payload precisely so this does not have to reverse-engineer
 * it out of the title, which carries the plant's name too.
 */
function careTitleOf(data: string): string | null {
  try {
    const parsed = JSON.parse(data) as { careTitle?: unknown };

    return typeof parsed.careTitle === 'string' ? parsed.careTitle : null;
  } catch {
    return null;
  }
}

const CARE_FIELDS: { label: string; key: keyof PlantProfile }[] = [
  { label: 'Water', key: 'water' },
  { label: 'Light', key: 'light' },
  { label: 'Humidity', key: 'humidity' },
  { label: 'Temperature', key: 'temperature' },
  { label: 'Soil', key: 'soil' },
  { label: 'Feeding', key: 'feeding' },
  { label: 'Repotting', key: 'repotting' },
  { label: 'Toxicity', key: 'toxicity' },
];

const today = () => new Date().toISOString().slice(0, 10);

export default function PlantCard({
  plant,
  onChanged,
  onReload,
  onEdit,
  fail,
}: {
  plant: Plant;
  /** The write returned the whole plant, so the card can swap itself out. */
  onChanged: (plant: Plant) => void;
  /** For writes whose blast radius is wider than this card. */
  onReload: () => Promise<void>;
  onEdit: () => void;
  fail: (e: unknown) => void;
}) {
  const [busy, setBusy] = useState<string | null>(null);
  const [asking, setAsking] = useState(false);
  const [description, setDescription] = useState(plant.description);
  const [selected, setSelected] = useState<string[] | null>(null);
  const [sowOn, setSowOn] = useState(today());
  const [sowSelected, setSowSelected] = useState<string[] | null>(null);

  const profile = plant.profile;
  const seed = plant.kind === PlantKinds.SeedPacket;
  const sowing = seed ? (profile?.sowing ?? null) : null;

  // A care line is spoken for by a schedule; a sowing step by a dated task.
  // Both are keyed the same way, so both come out of the same two sets.
  const scheduled = new Set(plant.recurrences.map((r) => careTitleOf(r.data)).filter((t): t is string => t !== null));
  const onTheBoard = new Set(plant.tasks.map((t) => careTitleOf(t.data)).filter((t): t is string => t !== null));

  const suggested = (profile?.careTasks ?? []).filter((care) => !scheduled.has(care.title));
  const chosen = selected ?? suggested.map((care) => care.title);

  const sowSteps = (sowing?.steps ?? []).filter((step) => !onTheBoard.has(step.title));
  const sowChosen = sowSelected ?? sowSteps.map((step) => step.title);

  async function run(what: string, action: () => Promise<unknown>) {
    setBusy(what);

    try {
      await action();
    } catch (e) {
      fail(e);
    } finally {
      setBusy(null);
    }
  }

  const lookUp = () =>
    run('look', async () => {
      onChanged(await researchPlant(plant.id, description.trim() === '' ? null : description.trim(), true));
      setAsking(false);
    });

  const addPhoto = (file: File | undefined) => {
    if (file === undefined) {
      return;
    }

    // Straight up on selection: one tap, camera, done. Asking for a stage first
    // would be a second decision to make holding a phone over a seed tray —
    // and the whole point is that the photo is read for you.
    run('photo', async () => {
      const captured = await prepare(file);

      const { plant: saved } = await addPlantPhoto(plant.id, {
        imageBase64: captured.base64,
        mediaType: captured.mediaType,
        takenOn: today(),
        stage: null,
        note: null,
      });

      onChanged(saved);
    });
  };

  const removePhoto = (photoId: string, stage: string) => {
    if (!window.confirm(`Delete this photo${stage === '' ? '' : ` ("${stage}")`}?`)) {
      return;
    }

    run('photo', async () => {
      await deletePlantPhoto(photoId);
      await onReload();
    });
  };

  const addSchedule = () =>
    run('schedule', async () => {
      const tasks = suggested.filter((care) => chosen.includes(care.title));

      if (tasks.length === 0) {
        return;
      }

      const { plant: saved } = await createPlantSchedule(plant.id, tasks);

      setSelected(null);
      onChanged(saved);
    });

  const planSowing = () =>
    run('sowing', async () => {
      const steps = sowSteps.filter((step) => sowChosen.includes(step.title));

      if (steps.length === 0) {
        return;
      }

      const { plant: saved } = await createSowingPlan(plant.id, sowOn, steps);

      setSowSelected(null);
      onChanged(saved);
    });

  const complete = (task: TaskListItem, status: TaskItemStatus) =>
    // Completing chains the next occurrence of a care schedule, so the card is
    // re-read rather than patched — the same reason the dashboard reloads.
    run('task', async () => {
      await completeTask(task.id, status);
      await onReload();
    });

  const removeSchedule = (id: string, title: string) => {
    if (!window.confirm(`Stop "${title}"? Its pending tasks go with it.`)) {
      return;
    }

    run('schedule', async () => {
      await deleteRecurrence(id);
      await onReload();
    });
  };

  const remove = () => {
    if (!window.confirm(`Delete "${plant.name}"? Its photos, care schedules and pending tasks go too.`)) {
      return;
    }

    run('delete', async () => {
      await deletePlant(plant.id);
      await onReload();
    });
  };

  const toggle = (title: string) =>
    setSelected(chosen.includes(title) ? chosen.filter((t) => t !== title) : [...chosen, title]);

  const toggleStep = (title: string) =>
    setSowSelected(sowChosen.includes(title) ? sowChosen.filter((t) => t !== title) : [...sowChosen, title]);

  return (
    <section className="card plant">
      <div className="note-head">
        <h2>
          {plant.name}
          {seed && <span className="tag">seed packet</span>}
        </h2>

        <span className="actions">
          {plant.location && <span className="due">{plant.location}</span>}
          <button type="button" className="link" onClick={onEdit}>
            Edit
          </button>
          <button type="button" className="link" disabled={busy !== null} onClick={remove}>
            Delete
          </button>
        </span>
      </div>

      {(plant.species || plant.latinName) && (
        <p className="species">
          {plant.species}
          {plant.latinName && <em>{plant.species ? ` · ${plant.latinName}` : plant.latinName}</em>}
        </p>
      )}

      {/* An identification the model was unsure of says so out loud. Guessing
          confidently is how a plant gets watered to death. */}
      {profile && profile.confidence !== '' && profile.confidence !== 'high' && (
        <p className="notes">
          <span className="guess">Best guess · {profile.confidence} confidence</span>
          {profile.reasoning && ` — ${profile.reasoning}`}
        </p>
      )}

      {profile ? (
        <>
          {profile.summary && <p>{profile.summary}</p>}

          <dl className="care">
            {CARE_FIELDS.filter((field) => (profile[field.key] as string) !== '').map((field) => (
              <div key={field.key}>
                <dt>{field.label}</dt>
                <dd>{profile[field.key] as string}</dd>
              </div>
            ))}
          </dl>

          {profile.commonProblems.length > 0 && (
            <>
              <h3>Watch for</h3>
              <ul className="problems">
                {profile.commonProblems.map((problem) => (
                  <li key={problem}>{problem}</li>
                ))}
              </ul>
            </>
          )}
        </>
      ) : (
        <p className="empty">Not looked up yet.</p>
      )}

      {plant.notes.trim() !== '' && <p className="notes">{plant.notes}</p>}

      <h3>
        Photos <span className="count">{plant.photos.length}</span>
      </h3>

      <div className="stages">
        {plant.photos.map((photo) => (
          <figure key={photo.id}>
            <PlantPhotoImg photoId={photo.id} alt={photo.stage} />
            <figcaption>
              <span className="title">{photo.stage === '' ? 'Unlabelled' : photo.stage}</span>
              <span className="due">{photo.takenOn}</span>
              {photo.note && <small className="notes">{photo.note}</small>}
              <button
                type="button"
                className="link"
                disabled={busy !== null}
                onClick={() => removePhoto(photo.id, photo.stage)}
              >
                Delete
              </button>
            </figcaption>
          </figure>
        ))}

        {plant.photos.length === 0 && <p className="empty">No photos yet.</p>}
      </div>

      <div className="actions">
        {/* A label rather than a button, because only a file input opens the
            camera — it is styled to look like the button it stands in for. */}
        <label className="file-button">
          {busy === 'photo' ? 'Reading the photo…' : 'Add photo'}
          <input
            type="file"
            accept="image/*"
            capture="environment"
            disabled={busy !== null}
            onChange={(e) => {
              addPhoto(e.target.files?.[0]);
              // Cleared so photographing the same thing twice still fires.
              e.target.value = '';
            }}
          />
        </label>

        <span className="notes">Each photo is a stage — it gets read and labelled.</span>
      </div>

      {sowing && (
        <>
          <h3>Sowing</h3>

          <dl className="care">
            {sowing.sowWindow !== '' && (
              <div>
                <dt>Sow</dt>
                <dd>
                  {sowing.sowWindow}
                  {sowing.startIndoors && ' · start indoors'}
                </dd>
              </div>
            )}
            {sowing.sowDepthMm !== null && (
              <div>
                <dt>Depth</dt>
                <dd>{sowing.sowDepthMm} mm</dd>
              </div>
            )}
            {sowing.spacingCm !== null && (
              <div>
                <dt>Spacing</dt>
                <dd>{sowing.spacingCm} cm</dd>
              </div>
            )}
            {sowing.germinationDays !== null && (
              <div>
                <dt>Germinates in</dt>
                <dd>{sowing.germinationDays} days</dd>
              </div>
            )}
            {sowing.daysToHarvest !== null && (
              <div>
                <dt>Harvest after</dt>
                <dd>{sowing.daysToHarvest} days</dd>
              </div>
            )}
          </dl>

          {sowing.method && <p className="notes">{sowing.method}</p>}
          {sowing.notes && <p className="notes">{sowing.notes}</p>}

          {sowSteps.length > 0 && (
            <div className="suggested">
              <p className="notes">
                The plan, counted from the day you sow. Nothing is dated until you pick that day.
              </p>

              <ul>
                {sowSteps.map((step) => (
                  <li key={step.title}>
                    <label className="checkbox">
                      <input
                        type="checkbox"
                        checked={sowChosen.includes(step.title)}
                        onChange={() => toggleStep(step.title)}
                      />
                      <span className="title">
                        {step.title}
                        {step.notes && <small className="notes">{step.notes}</small>}
                      </span>
                    </label>
                    <span className="due">
                      {step.dayOffset === 0 ? 'sowing day' : `day ${step.dayOffset}`}
                    </span>
                  </li>
                ))}
              </ul>

              <div className="actions">
                <label className="sow-date">
                  Sowing on
                  <input type="date" value={sowOn} onChange={(e) => setSowOn(e.target.value)} />
                </label>

                <button type="button" disabled={busy !== null || sowChosen.length === 0} onClick={planSowing}>
                  {busy === 'sowing' ? 'Dating it…' : 'Put the plan on the board'}
                </button>
              </div>
            </div>
          )}
        </>
      )}

      <h3>Care schedule</h3>

      {plant.recurrences.length === 0 ? (
        <p className="empty">Nothing scheduled.</p>
      ) : (
        <ul>
          {plant.recurrences.map((recurrence) => (
            <li key={recurrence.id}>
              <span className="title">
                {recurrence.title}
                {!recurrence.isActive && <span className="paused"> paused</span>}
              </span>
              {/* This tab only ever makes interval schedules, but the Recurring
                  tab can turn one into a day-of-week rule after the fact. */}
              <span className="due">
                {recurrence.intervalDays === null ? 'custom schedule' : `every ${recurrence.intervalDays} days`}
              </span>
              <span className="actions">
                <button
                  type="button"
                  className="link"
                  disabled={busy !== null}
                  onClick={() => removeSchedule(recurrence.id, recurrence.title)}
                >
                  Stop
                </button>
              </span>
            </li>
          ))}
        </ul>
      )}

      {suggested.length > 0 && (
        <div className="suggested">
          <p className="notes">Suggested — nothing is scheduled until you say so.</p>

          <ul>
            {suggested.map((care) => (
              <li key={care.title}>
                <label className="checkbox">
                  <input type="checkbox" checked={chosen.includes(care.title)} onChange={() => toggle(care.title)} />
                  <span className="title">
                    {care.title}
                    {care.notes && <small className="notes">{care.notes}</small>}
                  </span>
                </label>
                <span className="due">every {care.intervalDays} days</span>
              </li>
            ))}
          </ul>

          <div className="actions">
            <button type="button" disabled={busy !== null || chosen.length === 0} onClick={addSchedule}>
              {busy === 'schedule' ? 'Adding…' : 'Add to schedule'}
            </button>
            <span className="notes">Late means later: the next one counts from when you actually do it.</span>
          </div>
        </div>
      )}

      <h3>
        Coming up <span className="count">{plant.tasks.length}</span>
      </h3>

      {plant.tasks.length === 0 ? (
        <p className="empty">Nothing on the board.</p>
      ) : (
        <ul>
          {plant.tasks.map((task) => (
            <li key={task.id}>
              <span className="title">{task.title}</span>
              <span className="due">{task.dueOn}</span>
              <span className="actions">
                <button type="button" disabled={busy !== null} onClick={() => complete(task, TaskStatus.Done)}>
                  Done
                </button>
                <button
                  type="button"
                  className="ghost"
                  disabled={busy !== null}
                  onClick={() => complete(task, TaskStatus.Skipped)}
                >
                  Skip
                </button>
              </span>
            </li>
          ))}
        </ul>
      )}

      <div className="actions">
        <button type="button" className="ghost" disabled={busy !== null} onClick={() => setAsking(!asking)}>
          {profile ? 'Look it up again' : 'Look it up'}
        </button>

        {plant.researchedAt && (
          <span className="notes">
            Looked up {new Date(plant.researchedAt).toLocaleDateString()}
            {plant.researchModel && ` · ${plant.researchModel}`}
          </span>
        )}
      </div>

      {asking && (
        <div className="ask">
          <label>
            What do you know about it now?
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={3}
              maxLength={2000}
              placeholder="It flowered — small white flowers, and the leaves have holes in them."
            />
            <small className="notes">
              {plant.photos.length > 0
                ? 'Your newest photo goes with the question.'
                : 'Add a photo first and it will be looked at too.'}
            </small>
          </label>

          <div className="actions">
            <button type="button" disabled={busy !== null} onClick={lookUp}>
              {busy === 'look' ? 'Looking it up…' : 'Ask again'}
            </button>
            <button type="button" className="link" onClick={() => setAsking(false)}>
              Cancel
            </button>
          </div>
        </div>
      )}
    </section>
  );
}

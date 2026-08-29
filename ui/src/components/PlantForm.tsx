import { useEffect, useState, type FormEvent } from 'react';
import { createPlant, updatePlant } from '../api';
import { prepare, type CapturedPhoto } from '../photo';
import { PlantKinds } from '../types';
import type { Plant, PlantKind } from '../types';

const KINDS: { id: PlantKind; label: string }[] = [
  { id: PlantKinds.Plant, label: 'A plant' },
  { id: PlantKinds.SeedPacket, label: 'A seed packet' },
];

/**
 * Add and edit in one form, but not the same fields: adding asks what the
 * plant is — a photo, a description, or both — because that is the question the
 * lookup answers. Editing offers the species instead; by then the lookup has
 * had its go and what is left is correcting it.
 */
export default function PlantForm({
  plant,
  onSaved,
  onCancel,
}: {
  plant: Plant | null;
  onSaved: (saved: Plant, researchError: string | null) => void;
  onCancel: () => void;
}) {
  const [kind, setKind] = useState<PlantKind>(plant?.kind ?? PlantKinds.Plant);
  const [name, setName] = useState(plant?.name ?? '');
  const [location, setLocation] = useState(plant?.location ?? '');
  const [description, setDescription] = useState('');
  const [species, setSpecies] = useState(plant?.species ?? '');
  const [latinName, setLatinName] = useState(plant?.latinName ?? '');
  const [acquiredOn, setAcquiredOn] = useState(plant?.acquiredOn ?? '');
  const [notes, setNotes] = useState(plant?.notes ?? '');
  const [photo, setPhoto] = useState<CapturedPhoto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  // The preview is a data URL rather than an object URL, so there is nothing to
  // revoke — but a photo swapped out mid-form should not linger in state.
  useEffect(() => () => setPhoto(null), []);

  async function pick(file: File | undefined) {
    if (file === undefined) {
      return;
    }

    setError(null);

    try {
      setPhoto(await prepare(file));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not read that photo');
    }
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    try {
      if (plant) {
        const saved = await updatePlant(plant.id, {
          kind,
          name: name.trim(),
          location: location.trim() === '' ? null : location.trim(),
          species: species.trim() === '' ? null : species.trim(),
          latinName: latinName.trim() === '' ? null : latinName.trim(),
          acquiredOn: acquiredOn === '' ? null : acquiredOn,
          notes,
        });

        onSaved(saved, null);
        return;
      }

      const created = await createPlant({
        kind,
        name: name.trim(),
        location: location.trim() === '' ? null : location.trim(),
        description: description.trim() === '' ? null : description.trim(),
        imageBase64: photo?.base64 ?? null,
        mediaType: photo?.mediaType ?? null,
        acquiredOn: acquiredOn === '' ? null : acquiredOn,
        notes: notes.trim() === '' ? null : notes,
      });

      onSaved(created.plant, created.researchError);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not save the plant');
    } finally {
      setBusy(false);
    }
  }

  const seed = kind === PlantKinds.SeedPacket;

  return (
    <form className="card task-form" onSubmit={submit}>
      <h2>{plant ? 'Edit' : 'New'}</h2>

      {/* Editing offers this too: it is how a packet becomes a plant once it
          has come up, keeping its photos and its tasks. */}
      <div className="seg">
        {KINDS.map((option) => (
          <button
            key={option.id}
            type="button"
            className={option.id === kind ? 'tab selected' : 'tab'}
            onClick={() => setKind(option.id)}
          >
            {option.label}
          </button>
        ))}
      </div>

      <div className="row">
        <label>
          Name
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            maxLength={200}
            autoFocus
            placeholder={seed ? 'Tomatoes from the market' : 'The big one in the hall'}
          />
        </label>

        <label>
          Where it lives
          <input
            value={location}
            onChange={(e) => setLocation(e.target.value)}
            maxLength={200}
            placeholder={seed ? 'Seed box, greenhouse…' : 'North window, bathroom shelf…'}
          />
        </label>
      </div>

      {plant ? (
        <div className="row">
          <label>
            Species
            <input value={species} onChange={(e) => setSpecies(e.target.value)} maxLength={200} />
          </label>

          <label>
            Latin name
            <input value={latinName} onChange={(e) => setLatinName(e.target.value)} maxLength={200} />
          </label>
        </div>
      ) : (
        <>
          <label>
            Photo
            {/* capture opens the camera straight away on the tablet rather than
                a file browser it has nothing useful in. */}
            <input
              type="file"
              accept="image/*"
              capture="environment"
              onChange={(e) => pick(e.target.files?.[0])}
            />
            <small className="notes">
              {seed
                ? "Point it at the front of the packet. The variety name is what's needed — the small print gets looked up."
                : 'A clear shot of the leaves identifies it better than a description does.'}
            </small>
          </label>

          {photo && <img className="photo preview" src={photo.previewUrl} alt="" />}

          <label>
            Describe it
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={3}
              maxLength={2000}
              placeholder={
                seed
                  ? "Cherry tomatoes, says 'Sungold F1' on the front."
                  : 'Big glossy split leaves, climbs, came from the market in a plastic pot.'
              }
            />
            <small className="notes">
              Optional if there is a photo. With neither, it gets added unidentified and can be looked up later.
            </small>
          </label>
        </>
      )}

      <div className="row">
        <label>
          Got it on
          <input type="date" value={acquiredOn} onChange={(e) => setAcquiredOn(e.target.value)} />
        </label>
      </div>

      <label>
        Your notes
        <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={2} />
      </label>

      {error && <p className="error">{error}</p>}

      <div className="actions">
        <button type="submit" disabled={busy}>
          {/* The create path waits on the lookup, which searches the web and is
              seconds rather than milliseconds — "Saving" would look like a hang. */}
          {busy ? (plant ? 'Saving…' : 'Looking it up…') : 'Save'}
        </button>
        <button type="button" className="link" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  );
}

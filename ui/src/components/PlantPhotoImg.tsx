import { useEffect, useState } from 'react';
import { fetchPlantPhoto } from '../api';

/**
 * A photo from the API. It cannot be a plain src: the endpoint wants the bearer
 * token like everything else, so the bytes are fetched and handed over as an
 * object URL, which this revokes when it goes away.
 */
export default function PlantPhotoImg({ photoId, alt }: { photoId: string; alt: string }) {
  const [url, setUrl] = useState<string | null>(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    let gone = false;
    let objectUrl: string | null = null;

    fetchPlantPhoto(photoId)
      .then((created) => {
        // Unmounted while it was in flight: revoke rather than leak, and do not
        // set state on a component that is not there any more.
        if (gone) {
          URL.revokeObjectURL(created);
          return;
        }

        objectUrl = created;
        setUrl(created);
      })
      .catch(() => {
        if (!gone) {
          setFailed(true);
        }
      });

    return () => {
      gone = true;

      if (objectUrl !== null) {
        URL.revokeObjectURL(objectUrl);
      }
    };
  }, [photoId]);

  if (failed) {
    return <div className="photo-missing">?</div>;
  }

  return url === null ? <div className="photo-loading" /> : <img className="photo" src={url} alt={alt} />;
}

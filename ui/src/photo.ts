/**
 * Turning what the camera gives us into what the API wants.
 *
 * A tablet's rear camera produces 8-12 megapixels; the model downscales
 * anything over 1568px on its long edge before it looks at it, and the row goes
 * into Postgres. Sending the full frame would pay for storage and upload of
 * pixels nobody ever sees, so the shrinking happens here, before either.
 */
const MAX_EDGE = 1568;

/** Enough for leaves and labels, about a fifth the size of quality 1. */
const JPEG_QUALITY = 0.82;

export interface CapturedPhoto {
  /** Raw base64, no data: prefix — what the API takes. */
  base64: string;
  mediaType: string;
  /** For the preview, before anything is uploaded. Caller revokes it. */
  previewUrl: string;
}

export async function prepare(file: File): Promise<CapturedPhoto> {
  // from-image applies the EXIF rotation, without which every photo taken in
  // portrait arrives on its side.
  const bitmap = await createImageBitmap(file, { imageOrientation: 'from-image' });

  const scale = Math.min(1, MAX_EDGE / Math.max(bitmap.width, bitmap.height));
  const width = Math.round(bitmap.width * scale);
  const height = Math.round(bitmap.height * scale);

  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;

  const context = canvas.getContext('2d');

  if (context === null) {
    throw new Error('This browser could not process the photo.');
  }

  context.drawImage(bitmap, 0, 0, width, height);
  bitmap.close();

  // JPEG whatever came in: the alpha channel a PNG screenshot carries is not
  // worth the three-fold size on a photo of a plant.
  const dataUrl = canvas.toDataURL('image/jpeg', JPEG_QUALITY);

  return {
    base64: dataUrl.slice(dataUrl.indexOf(',') + 1),
    mediaType: 'image/jpeg',
    previewUrl: dataUrl,
  };
}

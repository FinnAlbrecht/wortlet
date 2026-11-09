import { expect, type Page } from "@playwright/test";

export interface Word {
  key: string;
  german: string;
  answers: string[];
  solution: string;
}

export interface Folder {
  id: string;
  chapters: Array<{ name: string; words: Word[] }>;
}

export const shot = (page: Page, name: string) =>
  page.screenshot({ path: `test-results/screens/${name}.png`, fullPage: true });

/** Liest den Ordner über die API (mit dem Token aus dem Browser), um die Lösungen zu kennen. */
export async function loadFolder(page: Page, folderId: string): Promise<Folder> {
  const token = await page.evaluate(() => JSON.parse(localStorage.getItem("wortlet-auth") ?? "{}").token as string);
  const res = await page.request.get(`/api/folders/${folderId}`, { headers: { Authorization: `Bearer ${token}` } });
  expect(res.ok()).toBeTruthy();
  return (await res.json()) as Folder;
}

export const folderIdFromUrl = (page: Page) => new URL(page.url()).pathname.split("/")[2];

/** Findet das abgefragte Wort (Deutsch → Fremdsprache oder umgekehrt). */
export function findWord(words: Word[], shown: string): { word: Word; reverse: boolean } {
  const forward = words.find((w) => w.german === shown);
  if (forward) return { word: forward, reverse: false };
  const reverse = words.find((w) => w.solution === shown);
  if (reverse) return { word: reverse, reverse: true };
  throw new Error(`Wort nicht gefunden: ${shown}`);
}

export async function answer(page: Page, text: string) {
  await page.locator("#answer").fill(text);
  await page.locator("#go").click();
  await expect(page.locator("#fb")).toBeVisible();
}

export async function next(page: Page) {
  await page.locator("#go").click();
}

/** Ein winziges, gültiges JPEG (1×1 Pixel) als Testbild. */
export const TINY_JPEG = Buffer.from(
  "/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////wgALCAABAAEBAREA/8QAFBABAAAAAAAAAAAAAAAAAAAAAP/aAAgBAQABPxA=",
  "base64",
);

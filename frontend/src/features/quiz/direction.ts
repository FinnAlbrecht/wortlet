export type Direction = "de" | "fx" | "mix";

const KEY = "vokabel-dir";

export function readDirection(): Direction {
  try {
    const d = localStorage.getItem(KEY);
    if (d === "de" || d === "fx" || d === "mix") return d;
  } catch {
    // egal
  }
  return "de";
}

export function saveDirection(d: Direction) {
  try {
    localStorage.setItem(KEY, d);
  } catch {
    // egal
  }
}

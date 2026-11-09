import { describe, expect, it } from "vitest";
import { DEFAULT_CHAPTER, describeParse, parseText } from "./parseText";

describe("parseText", () => {
  it("liest Wörter, Alternativen und Kapitel", () => {
    const r = parseText("# Obst\ndie Birne = pear\ndie Kasse = checkout; till\n\n# Gemüse\ndie Gurke = cucumber");
    expect(r.chapters.map((c) => c.name)).toEqual(["Obst", "Gemüse"]);
    expect(r.chapters[0].words[1]).toEqual({ key: "die Kasse", answers: ["checkout", "till"] });
    expect(r.wordCount).toBe(3);
    expect(r.errors).toEqual([]);
  });

  it("meldet Zeilen ohne „=“ und nutzt ein Standardkapitel", () => {
    const r = parseText("die Birne = pear\nohne Zeichen\n = leer");
    expect(r.chapters[0].name).toBe(DEFAULT_CHAPTER);
    expect(r.errors).toEqual([2, 3]);
    expect(describeParse(r)).toBe("1 Wort in 1 Kapitel · Zeile 2, 3 hat kein „=“ und wird ignoriert");
  });

  it("nummeriert doppelte deutsche Einträge", () => {
    const r = parseText("die Bank = bank\ndie Bank = bench");
    expect(r.chapters[0].words.map((w) => w.key)).toEqual(["die Bank", "die Bank (2)"]);
  });

  it("versteht Seiten, „neu“ und Hinweise", () => {
    const r = parseText("# Obst | S. 216–217 | neu\ndie Kasse = checkout; till | eines reicht");
    expect(r.chapters[0]).toMatchObject({ name: "Obst", pages: "S. 216–217", isNew: true });
    expect(r.chapters[0].words[0]).toEqual({ key: "die Kasse", answers: ["checkout", "till"], hint: "eines reicht" });
  });

  it("lässt leere Kapitel weg", () => {
    expect(parseText("# Leer\n# Voll\na = b").chapters.map((c) => c.name)).toEqual(["Voll"]);
  });
});

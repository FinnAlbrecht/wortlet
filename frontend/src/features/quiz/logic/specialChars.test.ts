import { describe, expect, it } from "vitest";
import { insertAt, specialCharsFor, upper } from "./specialChars";

describe("specialCharsFor", () => {
  it("erkennt die Sprache am Namen, auch ohne Umlaut und in anderer Schreibweise", () => {
    expect(specialCharsFor("Französisch", false)).toContain("ç");
    expect(specialCharsFor("franzosisch 2. Klasse", false)).toContain("é");
    expect(specialCharsFor("Spanisch", false)).toContain("ñ");
    expect(specialCharsFor("Italiano", false)).toContain("ì");
  });

  it("zeigt bei Englisch nichts", () => {
    expect(specialCharsFor("Englisch", false)).toEqual([]);
    expect(specialCharsFor("English Unit 3", false)).toEqual([]);
  });

  it("zeigt bei Antworten auf Deutsch die deutschen Zeichen – auch bei Englisch", () => {
    expect(specialCharsFor("Englisch", true)).toEqual(["ä", "ö", "ü", "ß"]);
  });

  it("hat für unbekannte Sprachen eine allgemeine Auswahl", () => {
    expect(specialCharsFor("Rätoromanisch", false).length).toBeGreaterThan(5);
  });
});

describe("upper", () => {
  it("macht Grossbuchstaben, lässt aber ß und Satzzeichen", () => {
    expect(upper("é")).toBe("É");
    expect(upper("ç")).toBe("Ç");
    expect(upper("ß")).toBe("ß");
    expect(upper("¿")).toBe("¿");
  });
});

describe("insertAt", () => {
  it("fügt an der Cursor-Position ein", () => {
    expect(insertAt("lve", 1, 1, "é")).toEqual({ value: "léve", caret: 2 });
  });
  it("ersetzt eine Markierung", () => {
    expect(insertAt("garcon", 3, 4, "ç")).toEqual({ value: "garçon", caret: 4 });
  });
  it("hängt ohne Cursor hinten an", () => {
    expect(insertAt("caf", null, null, "é")).toEqual({ value: "café", caret: 4 });
  });
});

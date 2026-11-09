import { describe, expect, it } from "vitest";
import { check, closestAnswer, diffNote, diffView, dist, formatGrade, norm, shuffle, swissGrade } from "./compare";

const text = (segments: { text: string; bad: boolean }[]) => segments.map((s) => s.text).join("");
const bad = (segments: { text: string; bad: boolean }[]) => segments.filter((s) => s.bad).map((s) => s.text).join("");

describe("norm / check", () => {
  it("ignoriert nur Gross/Klein, Leerzeichen und Apostroph-Art", () => {
    expect(norm("  Florist’s   Shop ")).toBe("florist's shop");
    expect(norm("how much is …?")).toBe("how much is ...?");
    expect(check("FRUIT", ["fruit"])).toBe(true);
    expect(check("fruits", ["fruit"])).toBe(false);
    expect(check("spend", ["to spend"])).toBe(false);
    expect(check("hard boiled", ["hard-boiled"])).toBe(false);
    expect(check("till", ["checkout", "cash desk", "till"])).toBe(true);
  });
});

describe("dist", () => {
  it("zählt Einfügen, Löschen und Ersetzen", () => {
    expect(dist("pear", "pear")).toBe(0);
    expect(dist("Pear", "pear")).toBe(0);
    expect(dist("per", "pear")).toBe(1);
    expect(dist("bear", "pear")).toBe(1);
    expect(dist("", "abc")).toBe(3);
  });
});

describe("diffView", () => {
  it("markiert fehlende Zeichen in der richtigen Lösung", () => {
    const d = diffView("spend", "to spend");
    expect(text(d.user)).toBe("spend");
    expect(bad(d.user)).toBe("");
    expect(text(d.target)).toBe("to spend");
    expect(bad(d.target)).toBe("to ");
    expect(d.count).toBe(3);
  });

  it("markiert falsche Zeichen in der Eingabe", () => {
    const d = diffView("hard boiled", "hard-boiled");
    expect(bad(d.user)).toBe(" ");
    expect(bad(d.target)).toBe("-");
    expect(d.count).toBe(1);
  });

  it("Gross/Klein und Apostroph-Art zählen nicht als Unterschied", () => {
    const d = diffView("Florist’s", "florist's");
    expect(d.count).toBe(0);
    expect(bad(d.user)).toBe("");
  });

  it("fasst gleiche Markierungen zusammen", () => {
    const d = diffView("xyz", "abc");
    expect(d.user).toEqual([{ text: "xyz", bad: true }]);
  });
});

describe("diffNote", () => {
  it("erkennt typische Fehler", () => {
    expect(diffNote("spend", "to spend")).toBe("Es fehlt „to“ vor dem Verb.");
    expect(diffNote("to fruit", "fruit")).toBe("Hier gehört kein „to“ davor.");
    expect(diffNote("Sahne", "die Sahne")).toBe("Es fehlt der Artikel „die“.");
    expect(diffNote("hard boiled", "hard-boiled")).toBe("Achte auf den Bindestrich.");
    expect(diffNote("florists", "florist's")).toBe("Achte auf den Apostroph.");
    expect(diffNote("cheers", "cheers!")).toBe("Achte auf die Satzzeichen.");
    expect(diffNote("icecream", "ice cream")).toBe("Achte auf die Leerzeichen.");
    expect(diffNote("pearr", "pear")).toBe("Nur ein Zeichen ist anders.");
    expect(diffNote("pxxr", "pear")).toBe("2 Zeichen sind anders.");
    expect(diffNote("banana", "pear")).toBe("");
  });
});

describe("closestAnswer", () => {
  it("wählt die ähnlichste richtige Antwort", () => {
    expect(closestAnswer("chekout", ["checkout", "cash desk", "till"])).toBe("checkout");
    expect(closestAnswer("til", ["checkout", "cash desk", "till"])).toBe("till");
    expect(closestAnswer("x", [])).toBe("");
  });
});

describe("shuffle", () => {
  it("behält alle Elemente und verändert das Original nicht", () => {
    const list = [1, 2, 3, 4, 5];
    const out = shuffle(list, () => 0.3);
    expect(out.slice().sort()).toEqual(list);
    expect(list).toEqual([1, 2, 3, 4, 5]);
  });
});

describe("swissGrade", () => {
  it("rechnet nach Schweizer Skala mit halben Noten", () => {
    expect(swissGrade(10, 10)).toBe(6);
    expect(swissGrade(0, 10)).toBe(1);
    expect(swissGrade(5, 10)).toBe(3.5);
    expect(swissGrade(7, 10)).toBe(4.5);
    expect(formatGrade(6)).toBe("6");
    expect(formatGrade(4.5)).toBe("4.5");
  });
});

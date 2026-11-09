import { describe, expect, it } from "vitest";
import { parseText } from "../folders/logic/parseText";
import { buildChatPrompt } from "./copyPrompt";

describe("buildChatPrompt", () => {
  it("nennt die Sprache und verlangt das Format „Deutsch = Fremdsprache“", () => {
    const p = buildChatPrompt("Französisch");
    expect(p).toContain("Französisch-Deutsch-Vokabelbuch");
    expect(p).toContain("deutscher Eintrag = Eintrag auf Französisch");
  });

  it("das Beispiel im Prompt ist selbst gültiger Eingabetext", () => {
    const example = buildChatPrompt("Englisch").split("Beispiel:\n")[1];
    const r = parseText(example);
    expect(r.wordCount).toBe(2);
    expect(r.errors).toEqual([]);
  });
});

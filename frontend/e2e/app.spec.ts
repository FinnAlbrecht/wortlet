import { expect, test } from "@playwright/test";
import fs from "node:fs";
import { answer, findWord, folderIdFromUrl, loadFolder, next, shot, TINY_JPEG } from "./helpers";

const email = `e2e-${Date.now()}@example.test`;
const password = "geheim-1234";

test.describe.configure({ mode: "serial" });

test("kompletter Ablauf: Konto, Prüfung, Topf, Fehlerliste, eigener Ordner, Export/Import, Bilderkennung", async ({ page, context }) => {
  await context.grantPermissions(["clipboard-read", "clipboard-write"]);

  // ---- Registrieren ----
  await page.goto("/");
  await expect(page.locator("#h1")).toHaveText("Vokabeltrainer");
  await page.locator("#tab-register").click();
  await page.locator("#email").fill(email);
  await page.locator("#password").fill(password);
  await shot(page, "01-registrieren");
  await page.locator("#authsubmit").click();
  await expect(page.locator(".lang .fname")).toHaveText(["Englisch"]);
  await expect(page.locator(".lang .fmeta")).toContainText("1 Ordner · 319 Wörter");
  await shot(page, "02-sprachen");

  // ---- Abmelden und wieder einloggen ----
  await page.locator("#logout").click();
  await expect(page.locator("#tab-login")).toBeVisible();
  await page.locator("#email").fill(email);
  await page.locator("#password").fill("falsch-falsch");
  await page.locator("#authsubmit").click();
  await expect(page.getByRole("alert")).toHaveText("E-Mail-Adresse oder Passwort ist falsch.");
  await page.locator("#password").fill(password);
  await page.locator("#authsubmit").click();
  await expect(page.locator(".lang .fname")).toHaveText(["Englisch"]);

  // ---- Ordner öffnen ----
  await page.locator(".lang").first().click();
  await expect(page.locator("#h1")).toHaveText("Englisch");
  await expect(page.locator("#folderlist .fname")).toHaveText(["Einkaufen & Ernährung"]);
  await page.locator("#folderlist .folder").first().click();
  await expect(page.locator("#h1")).toHaveText("Einkaufen & Ernährung");
  await expect(page.locator("#sub")).toContainText("genau wie im Buch");
  await expect(page.locator("#chapters label")).toHaveCount(9);
  await expect(page.locator("#total")).toHaveText("319 Wörter ausgewählt");
  await expect(page.locator("#oldonly")).toHaveText("Alte Wörter (S. 201–215)");
  await page.locator("#newonly").click();
  await expect(page.locator("#total")).not.toHaveText("319 Wörter ausgewählt");
  await expect(page.locator("#weakhead")).toHaveText("Keine Wörter – super!");
  await shot(page, "03-ordner-menue");

  const folderId = folderIdFromUrl(page);
  const folder = await loadFolder(page, folderId);
  const chapterIndex = folder.chapters.findIndex((c) => c.name === "Restaurant und Café");
  const chapterWords = folder.chapters[chapterIndex].words;

  // Nur ein Kapitel, Richtung Deutsch → Englisch
  await page.locator("#none").click();
  await expect(page.locator("#begin")).toBeDisabled();
  await page.locator(`#ch${chapterIndex}`).check();
  await page.locator("#dir-de + span").click();
  await expect(page.locator("#total")).toHaveText(`${chapterWords.length} Wörter ausgewählt`);
  await page.locator("#begin").click();

  // ---- Prüfung durchspielen ----
  await expect(page.locator("#mode")).toHaveText("Durchgang 1");
  let wrongWord = "";
  for (let k = 0; k < chapterWords.length; k++) {
    await expect(page.locator("#count")).toHaveText(`${k + 1} / ${chapterWords.length}`);
    const { word } = findWord(chapterWords, (await page.locator("#word").textContent()) ?? "");
    const right = word.answers[0];

    if (k === 0) {
      // Falsch, dann „War doch richtig“
      await answer(page, right + "x");
      await expect(page.locator("#fb")).toContainText("Falsch – kommt in den Topf");
      await expect(page.locator("#fb .diff")).toContainText("Du");
      await expect(page.locator("#fb .diffnote")).toHaveText("Nur ein Zeichen ist anders.");
      await expect(page.locator("#potchip")).toHaveText("Topf: 1");
      await shot(page, "04-falsch-mit-unterschied");
      await page.locator("#override").click();
      await expect(page.locator("#fb")).toContainText("Als richtig gewertet.");
      await expect(page.locator("#potchip")).toHaveText("Topf: 0");
    } else if (k === 1) {
      // Wirklich falsch → Topf und Fehlerliste
      wrongWord = word.key;
      await answer(page, "völlig falsch");
      await expect(page.locator("#fb")).toContainText("kommt in den Topf");
      await expect(page.locator("#potchip")).toHaveText("Topf: 1");
    } else {
      await answer(page, right.toUpperCase());
      await expect(page.locator("#fb")).toContainText("Richtig!");
    }
    await next(page);
  }

  // ---- Topf ----
  await expect(page.locator("#pothead")).toHaveText("1 Wort im Topf");
  await expect(page.locator("#potlist")).toContainText(wrongWord);
  await shot(page, "05-topf");
  await page.locator("#potgo").click();
  await expect(page.locator("#mode")).toHaveText("Topf · Runde 2");
  {
    const { word } = findWord(chapterWords, (await page.locator("#word").textContent()) ?? "");
    expect(word.key).toBe(wrongWord);
    await answer(page, word.answers[0]);
    await expect(page.locator("#fb")).toContainText("Richtig!");
    await next(page);
  }
  await expect(page.locator("#score")).toHaveText(`${chapterWords.length - 1} / ${chapterWords.length}`);
  await expect(page.locator("#grade")).toContainText("Note");
  await expect(page.locator("#rounds")).toContainText("sind jetzt 1 Wort.");
  await shot(page, "06-ergebnis");
  await page.locator("#restart").click();

  // ---- Fehlerliste ----
  await expect(page.locator("#weakhead")).toHaveText("1 Wort");
  await page.locator("#weakdetails summary").click();
  await expect(page.locator("#weaklist")).toContainText(wrongWord);
  await shot(page, "07-fehlerliste");

  // Üben: richtig im ersten Durchgang → raus aus der Liste
  await page.locator("#weakgo").click();
  await expect(page.locator("#mode")).toHaveText("Nicht gekonnt · Durchgang 1");
  {
    const { word } = findWord(chapterWords, (await page.locator("#word").textContent()) ?? "");
    await answer(page, word.answers[0]);
    await expect(page.locator("#fb")).toContainText("Raus aus der Liste.");
    await next(page);
  }
  await expect(page.locator("#rounds")).toContainText("sind jetzt 0 Wörter.");
  await page.locator("#restart").click();
  await expect(page.locator("#weakhead")).toHaveText("Keine Wörter – super!");

  // ---- Richtung umkehren (Englisch → Deutsch) ----
  await page.locator("#dir-fx + span").click();
  await expect(page.locator("#dir-fx")).toBeChecked();
  await page.locator("#begin").click();
  await expect(page.locator("#qlabel")).toHaveText("Auf Deutsch übersetzen");
  {
    const { word, reverse } = findWord(chapterWords, (await page.locator("#word").textContent()) ?? "");
    expect(reverse).toBe(true);
    // Ein Teil reicht („das Restaurant, die Gaststätte, das Gasthaus“ → „die Gaststätte“)
    const part = word.german.split(",").map((s) => s.trim()).at(-1)!;
    await answer(page, part);
    await expect(page.locator("#fb")).toContainText("Richtig!");
  }
  await shot(page, "08-richtung-umgekehrt");
  await page.locator("#menu").click();
  await page.locator("#dir-de + span").click();

  // ---- Eigene Sprache und eigener Ordner ----
  await page.locator("#back").click();
  await page.locator("#tolangs").click();
  await page.locator("#newlang").click();
  await page.locator("#langname").fill("Französisch");
  await page.locator("#langsave").click();
  await expect(page.locator("#h1")).toHaveText("Französisch");
  await expect(page.locator("#dellang")).toBeVisible();
  await page.locator("#newfolder").click();
  await expect(page.locator("#edlabel")).toHaveText("Neuer Ordner");
  await page.locator("#edname").fill("Schule");
  await page.locator("#edtext").fill("# Kapitel 1\ndie Schule = l'école\nder Lehrer = le professeur; l'enseignant\nohne Gleichheitszeichen");
  await expect(page.locator("#edcount")).toHaveText("2 Wörter in 1 Kapitel · Zeile 4 hat kein „=“ und wird ignoriert");

  // ---- Bilderkennung mit Testbild (OpenRouter wird in der Testumgebung simuliert) ----
  await expect(page.locator("#photoquota")).toContainText("Heute noch");
  await page.locator("#edphotos").setInputFiles({ name: "seite.jpg", mimeType: "image/jpeg", buffer: TINY_JPEG });
  await expect(page.locator("#photostatus")).toContainText("Wörter erkannt", { timeout: 60_000 });
  await expect(page.locator("#edtext")).toHaveValue(/die Birne = pear/);
  await expect(page.locator("#edtext")).toHaveValue(/die Kasse = checkout; till/);
  await page.locator("#copyprompt").click();
  await expect(page.locator("#copystatus")).toContainText("Prompt kopiert");
  const clip = await page.evaluate(() => navigator.clipboard.readText());
  expect(clip).toContain("Französisch-Deutsch-Vokabelbuch");
  await shot(page, "09-editor-mit-foto");
  await page.locator("#edsave").click();

  await expect(page.locator("#h1")).toHaveText("Schule");
  await expect(page.locator("#sub")).toContainText("Eigener Ordner · Deutsch ↔ Französisch");
  const total = await page.locator("#total").textContent();
  expect(total).toMatch(/^[45] Wörter ausgewählt$/);

  // ---- Neu laden: Daten noch da ----
  await page.reload();
  await expect(page.locator("#h1")).toHaveText("Schule");
  await expect(page.locator("#total")).toHaveText(total!);

  // ---- Export ----
  await page.goto("/");
  const [download] = await Promise.all([page.waitForEvent("download"), page.locator("#export").click()]);
  const file = await download.path();
  const exported = JSON.parse(fs.readFileSync(file!, "utf8"));
  expect(exported.version).toBe(1);
  expect(exported.languages.map((l: { name: string }) => l.name)).toEqual(["Englisch", "Französisch"]);
  await expect(page.locator("#backupstatus")).toContainText("2 Sprachen");

  // Etwas löschen: Ordner „Schule“ und Sprache „Französisch“
  await page.locator(".lang", { hasText: "Französisch" }).click();
  await expect(page.locator("#dellang")).toHaveCount(0); // Sprache nicht leer → kein Löschknopf
  await page.locator("#folderlist .folder").first().click();
  await page.locator("#editfolder").click();
  await page.locator("#eddel").click();
  await page.locator("#edconfirm .dangerfill").click();
  await expect(page.locator("#h1")).toHaveText("Französisch");
  await page.locator("#dellang").click();
  await page.locator("#langconfirm .dangerfill").click();
  await expect(page.locator(".lang .fname")).toHaveText(["Englisch"]);

  // ---- Import mit Bestätigung ----
  await page.locator("#importfile").setInputFiles(file!);
  await expect(page.locator("#importconfirm")).toContainText("werden durch die Datei ersetzt");
  await shot(page, "10-import-bestaetigen");
  await page.locator("#importconfirm .dangerfill").click();
  await expect(page.locator("#backupstatus")).toContainText("Importiert: 2 Sprachen");
  await expect(page.locator(".lang .fname")).toHaveText(["Englisch", "Französisch"]);

  // ---- Handyansicht ----
  await page.setViewportSize({ width: 390, height: 844 });
  await page.locator(".lang", { hasText: "Englisch" }).click();
  await page.locator("#folderlist .folder").first().click();
  await expect(page.locator("#setup")).toBeVisible();
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
  expect(overflow).toBe(false);
  await shot(page, "11-handy");
});

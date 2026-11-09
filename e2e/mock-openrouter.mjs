// Simuliert https://openrouter.ai/api/v1/chat/completions für den End-to-End-Test (ohne echten Schlüssel).
// Antwortet absichtlich mit JSON in ```json … ```, wie es Modelle oft tun.
import http from "node:http";

const port = Number(process.env.PORT ?? 4010);

const reply = {
  chapters: [
    {
      name: "Obst",
      words: [
        { de: "die Birne", en: ["pear"] },
        { de: "die Kasse", en: ["checkout", "till"] },
      ],
    },
  ],
};

http
  .createServer(async (req, res) => {
    let body = "";
    for await (const chunk of req) body += chunk;

    if (req.method === "GET") {
      res.writeHead(200).end("ok");
      return;
    }

    const auth = req.headers.authorization ?? "";
    let payload = {};
    try {
      payload = JSON.parse(body);
    } catch {
      // unten abgelehnt
    }
    const parts = payload?.messages?.[0]?.content ?? [];
    const hasImage = parts.some((p) => p.type === "image_url" && String(p.image_url?.url ?? "").startsWith("data:image/"));
    const hasHeaders = auth.startsWith("Bearer ") && req.headers["x-title"] === "Vokabeltrainer" && !!req.headers["http-referer"];

    console.log(`[mock-openrouter] ${req.method} ${req.url} model=${payload?.model} image=${hasImage} headers=${hasHeaders}`);

    if (!hasImage || !hasHeaders) {
      res.writeHead(400, { "Content-Type": "application/json" });
      res.end(JSON.stringify({ error: { code: 400, message: "Bild oder Header fehlen" } }));
      return;
    }

    res.writeHead(200, { "Content-Type": "application/json" });
    res.end(
      JSON.stringify({
        id: "mock",
        model: payload.model,
        choices: [{ message: { role: "assistant", content: "Hier sind die Wörter:\n```json\n" + JSON.stringify(reply) + "\n```" } }],
      }),
    );
  })
  .listen(port, () => console.log(`[mock-openrouter] läuft auf Port ${port}`));

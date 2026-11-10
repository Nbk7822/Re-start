# Return — The First Camp

A dependency-free, playable browser prototype of Return's companion loop. This is a throwaway JavaScript experiment; the production engine recommendation remains a separate decision.

The visual direction is a flat overhead folklore map: smooth inked contours, black outlines, muted jade and ochre, compact cloaked pawns, and buildings drawn as floor plans. There is no perspective camera, raised terrain, or pixel-art sprite sheet.

Run `npm start`, then open http://127.0.0.1:4173. Run `npm test` for the simulation checks. No installation is needed with a current Node.js runtime.

You begin with nothing. Gather food, invite people to stay, repair the shelter, and survive twelve shifts with three companions and three meals remaining. Press Space to pause; press the period key or click **Finish shift** to advance immediately. Keys 1, 2, and 3 select time speeds. NPC work requests begin next shift and can be refused.

The demo includes eight named people, humans and vampires, fatigue/hunger/health, autonomous priorities, consent for blood donation, directed trust, concrete debts, promises, ration policy, incidents, an event record, and an ending. It has no kingdom, rewind, save system, LLM, or audio. Restart starts a new playthrough; it is not Return by Death.

`window.Return.observe()` returns a detached copy of player-visible state. `window.Return.command(object)` uses the same handlers as the UI; `window.Return.help()` lists commands. The optional **Text controls** panel provides the same access without browser developer tools.

Examples:

```json
{"type":"intent","actorId":"you","task":"forage"}
{"type":"invite","actorId":"mira"}
{"type":"request","actorId":"mira","task":"wood","offer":"meal"}
{"type":"donate","actorId":"you","target":"ilex"}
{"type":"step"}
```

The observation export is a factual session record, not a resumable save. No data is sent to an external model. Hosting is owner-private through Sites.

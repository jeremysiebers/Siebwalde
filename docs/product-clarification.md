Start a product clarification phase for the existing Siebwalde application.

Use the existing agent definitions and documentation. Do not restart the
full codebase analysis and do not implement changes yet.

Read the project instructions, documentation index, product document,
knowledge summary, backlog, decisions, and handoff. Follow existing
filenames when they differ from expected names.

Your goal is to combine:
1. Verified knowledge of the current implementation.
2. My knowledge of the physical railway and actual application usage.
3. My intended future behavior and development priorities.

First give me a concise Dutch summary of:
- What you currently understand about the application.
- What is uncertain.
- Which missing information only I can reasonably provide.

Then interview me in Dutch:
- Ask one or two related questions at a time.
- Wait for my answers before moving to the next questions.
- Start with actual usage, intended behavior, and the next valuable feature.
- Ask about domain terminology, operational workflows, hardware boundaries,
  manual versus automatic control, and emulator-based verification when relevant.
- Do not ask me questions that the existing documentation already answers.
- Use targeted source inspection when an implementation detail needs checking.
- Do not treat current code behavior as proof of intended product behavior.

After each substantive answer:
- Summarize your interpretation briefly so I can correct it.
- Save explicit requirements and priorities in the product document.
- Save confirmed decisions with their reasons.
- Keep ideas, assumptions, and unresolved questions separate.
- Update the backlog and handoff as appropriate.
- Do not change agent definitions during this phase.

Work as a single agent. Do not invoke subagents unless I request it.

When enough information is available, propose the first small development
increment with:
- The user-visible outcome.
- Scope and explicit exclusions.
- Testable acceptance criteria.
- Likely affected components.
- Remaining technical questions.
- A verification approach that starts offline or with an emulator.

Ask me to agree on that increment before beginning implementation.
Do not require the entire future application to be specified first.
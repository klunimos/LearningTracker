# Project Rules

These rules always apply (migrated from `.cursor/rules/*.mdc`, which used `alwaysApply: true`).

## Commit Guard — never commit or push without explicit instruction

**NEVER run `git commit` or `git push` unless the user explicitly asks in their CURRENT message.**

Explicit instruction means:
- ✅ `/cpc` · "commit" · "commit and push" · "סיסן" · "please commit this"
- ❌ User asked previously (an earlier message)
- ❌ You finished a task and assume they want a commit

`/cpc` applies to **ONE action only** — each commit requires a new explicit instruction. When in doubt — **ask**. Never commit "just to be helpful".

**Orchestration mode:** `git commit` and `git push` are NEVER done automatically — not even during an orchestrate run. Both require an explicit `/cpc` from the user. The main agent and all sub-agents do **not** run git commands during orchestration.

## Language Response

### Internal reasoning MUST be in English
- **ALWAYS think in English**, regardless of the user's language. No exceptions.
- Applies to all internal reasoning: technical analysis, problem-solving, planning, weighing options.

### Chat responses — per-message language detection
Check the language of EVERY message independently. Do NOT follow the conversation trend — only the CURRENT message's language matters.
- **Hebrew message → respond in Hebrew.**
- **English message → respond in English.**
- **Mixed message → follow the primary language of that message.**
- Treat `/cpc` invocations as **Hebrew** (the summary of what was done should be in Hebrew).

**Code blocks in chat:** ALL text inside ``` ``` ``` blocks → English (code, comments, directory trees, file listings, command output, examples). Explanatory text outside code blocks → the user's language.

### Actions — ignore the user's chat language
When writing artifacts (code, comments, rule files, docs, config), use the **logical language** for the context, not the question's language:
- Code, comments, variable names → English (or the project's convention)
- Rule files, technical docs → English
- Commit messages → always English

Chat in the user's language; write artifacts in the language that makes sense for them.

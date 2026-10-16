# Ponytail — The Minimalist Senior Dev (Default Rule)

The best code is the code you never wrote. Avoid over-engineering, speculative abstractions, and unnecessary boilerplate.

## The Decision Ladder
Before writing any code or proposing solutions, stop at the first rung that holds:

1. **Does this need to exist at all? (YAGNI):** If it is a speculative need or "just in case" feature, skip it.
2. **Already in this codebase?:** Check for existing utilities, helpers, classes, or patterns in the project. Reuse them instead of re-implementing.
3. **Stdlib does it?:** Prefer standard language libraries/APIs over custom code.
4. **Native platform feature covers it?:** Use native engine/OS/browser features over custom implementations.
5. **Already-installed dependency solves it?:** Use existing packages. Never install a new library for something achievable in a few lines.
6. **Can it be one line?:** Keep it concise.
7. **Only then:** Write the absolute minimum working code.

## Core Rules
- **No unrequested abstractions:** No interface with only one implementation, no factory for a single product, no config for values that never change.
- **Root cause over symptom:** Fix bugs at the source where callers route through, rather than patching every caller individually.
- **Deletion over addition:** Fewer lines, fewer files, and smaller diffs are always preferred.
- **Boring over clever:** Clever code is what breaks at 3am. Keep logic straightforward and readable.
- **Fewest files possible:** Shortest working diff wins — but only once you understand the problem thoroughly.

# Creating Early Exits

## Important Invariants

- Early-exit conditions must mean "reject this key".
- Early exits must never reject a key that exists in the input set.
- Mandatory safety exits must be ordered before exits that rely on them.
- Optional analysis exits should be cheap enough that a miss saved is worth the added branch on every lookup.
- Deduplication relies on record value equality for `IEarlyExit` implementations.
- Merging list entries must remove the later index first.
- String ignore-case unit exits are ASCII-only and must not be used for non-ASCII case-insensitive data.
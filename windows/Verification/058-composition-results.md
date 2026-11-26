| Check | Result |
|---|---|
| repair middle without losing suffix | PASS |
| undo insertion restores caret | PASS |
| undo deletion restores suffix | PASS |
| redo repairs original typo | PASS |
| backspace at start keeps text | PASS |
| delete at end keeps text | PASS |
| new edit clears redo | PASS |
| commit clears history | PASS |
| 256 limit without overwriting tail | PASS |
| edit middle of maximum length | PASS |
| 32 previous edits remain available | PASS |
| history stops at 32 edits | PASS |
| offline dictionary loads | PASS |
| 200-letter whole sentence decoding | PASS |
| 200-letter query milliseconds | 3.7733 |
| long sentence preference survives dictionary reopen | PASS |

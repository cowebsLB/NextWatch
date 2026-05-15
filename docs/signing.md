# Code signing (optional)

Unsigned builds work for development. For wider distribution:

1. Obtain an Authenticode certificate.
2. Sign `NextWatch.exe` after publish:

```powershell
signtool sign /fd SHA256 /a NextWatch.exe
```

Self-replace auto-update (v2+) should only be recommended after signing is in place.

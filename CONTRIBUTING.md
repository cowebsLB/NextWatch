# Contributing to NextWatch

## Build

```powershell
dotnet restore
dotnet build
dotnet test
```

## Pull requests

1. Fork and branch from `main`
2. Keep changes focused — NextWatch stays **local, simple, portable, fast**
3. Run `dotnet test` before opening a PR
4. Do not commit secrets, `.db` files, or `data/` folders

## Complexity gate

Before adding a feature, ask: does this help one person on one Windows PC watch their network? If it sounds like an NMS backlog item, defer it.

## License

By contributing, you agree that your contributions are licensed under the MIT License.

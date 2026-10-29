# decompiled/il2cpp

Optional. Decompiled IL2CPP proxy assemblies (signatures and interop stubs only, no real game logic), for
checking what the proxies expose. Not needed to build. Not included in the repo.

Generate it from `lib/il2cpp/` with ilspycmd, for example:

    ilspycmd -p -o decompiled/il2cpp/Assembly-CSharp lib/il2cpp/Assembly-CSharp.dll

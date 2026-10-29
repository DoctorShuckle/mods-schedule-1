# decompiled/mono

Optional. Decompiled game source for reading and searching, not needed to build. Not included in the repo.

Generate it from the Mono assemblies in `lib/mono/` with ilspycmd, for example:

    ilspycmd -p -o decompiled/mono/Assembly-CSharp lib/mono/Assembly-CSharp.dll

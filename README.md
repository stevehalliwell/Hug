# Hug
A Developer Console for Unity, that's easy to setup, and extend. Using it should feel safe and comfortable, like a warm hug.

## Why?
Well there are a number of other options out there. None were quite doing all the things that I wanted or as easily as it could be. Previously I was using CUDLR with some modifications. It is no longer being supported. Thus Hug.

## Features
- Show Unity Debug Logs
- Tab Completion
- Input History
- Helpers for Binding instance and static methods and variables
- Automatic binding via Attributes
- BindingHelper to convert from user input to typed parameters
- Built out of easily replacable parts

How do I use this?
---
- Add to your package manager with Add Git "https://github.com/stevehalliwell/Hug.git#upm"
- Add the DevConsole to your scene/bootstrap
- Add Write, Add, or Expose parts to the Console, this can be done via the ConsoleCommand Attribute or manually via Console.Register or ConsoleBindingHelper.Add*
- If using BindingHelper and builting for a platform that uses IL2CPP, ensure you consult [Unity's doco around code stripping](https://docs.unity3d.com/Manual/ManagedCodeStripping.html)

Contributions?
---
Hug is still in development and is very open to contributions, bugs and feature requests encouraged in the Issues.

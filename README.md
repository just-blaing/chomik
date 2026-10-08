# 🐹 chomik - your virtual hamster!

<img src="assets/chomik_music.gif" width="200" alt="chomik listening to music">

[(русская версия readme)](README.ru.md)

## what he does

- listens to music together with you (the app whitelist is configurable)
- reacts when you type
- writes your text in a speech bubble above his head
- falls asleep when you are away for a long time
- takes screenshots of your screen
- eats the files you drag onto him

!! by default the hamster only "eats" files in the animation, nothing is deleted. real deletion can be enabled in settings (to the recycle bin or permanently). "delete permanently" erases files with no way back, so be careful !! 

## download

| system | archive |
|---|---|
| windows 64-bit | [download](https://github.com/just-blaing/chomik/releases/download/v1.4/chomik-v1.4-win-x64.zip) |
| windows 32-bit | [download](https://github.com/just-blaing/chomik/releases/download/v1.4/chomik-v1.4-win-x86.zip) |
| linux (x64) | [download](https://github.com/just-blaing/chomik/releases/download/v1.4/chomik-v1.4-linux-x64.zip) |
| linux (arm64) | [download](https://github.com/just-blaing/chomik/releases/download/v1.4/chomik-v1.4-linux-arm64.zip) | 
| macos (apple silicon) | [download](https://github.com/just-blaing/chomik/releases/download/v1.4/chomik-v1.4-osx-arm64.zip) |
| macos (intel) | [download](https://github.com/just-blaing/chomik/releases/download/v1.4/chomik-v1.4-osx-x64.zip) |

## build

since the application has been ported to each os:
| system | command |
|---|---|
| windows 64-bit | dotnet publish -c Release -r win-x64 -o out/win-x64 |
| windows 32-bit | dotnet publish -c Release -r win-x86 -o out/win-x86 |
| linux (x64) | dotnet publish -c Release -r linux-x64 -o out/linux-x64 |
| linux (arm64) | dotnet publish -c Release -r linux-arm64 -o out/linux-arm64 |
| macos (apple silicon) | dotnet publish -c Release -r osx-arm64 -o out/mac-arm | 
| macos (intel) | dotnet publish -c Release -r osx-x64 -o out/mac-intel |

## license

[chomik license](LICENSE): credit the author and do not use it commercially.

## credits

author: blaing

chomik sprites: chomikuj.pl

built with [Avalonia](https://avaloniaui.net)

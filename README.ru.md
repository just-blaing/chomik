# 🐹 чомик - ваш виртуальный хомячок!

![чомик](assets/chomik_music.gif)

## что он умеет:

- слушать музыку вместе с вами (список приложений настраивается)
- реагирует, когда вы печатаете
- пишет ваш текст в пузыре над головой
- засыпает, когда вас долго нет
- делает скриншоты экрана
- съедает файлы, которые вы на него перетащите

!! по умолчанию хомяк файлы только "ест" в анимации, ничего не удаляется. в настройках можно включить настоящее удаление (в корзину или насовсем). "удалять насовсем" стирает файлы без возврата, будьте осторожны !! 

## скачать

| система | архив |
|---|---|
| windows 64-бит | [download](https://github.com/just-blaing/chomik/releases/download/v1.4/chomik-v1.4-win-x64.zip) |
| windows 32-бит | [download](https://github.com/just-blaing/chomik/releases/download/v1.4/chomik-v1.4-win-x86.zip) |
| linux (x64) | [download](https://github.com/just-blaing/chomik/releases/download/v1.4/chomik-v1.4-linux-x64.zip) |
| linux (arm64) | [download](https://github.com/just-blaing/chomik/releases/download/v1.4/chomik-v1.4-linux-arm64.zip) | 
| macos (apple silicon) | [download](https://github.com/just-blaing/chomik/releases/download/v1.4/chomik-v1.4-osx-arm64.zip) |
| macos (intel) | [download](https://github.com/just-blaing/chomik/releases/download/v1.4/chomik-v1.4-osx-x64.zip) |


## сборка

так как приложение портировано под каждую ос:

| система | команда |
|---|---|
|---|---|
| windows 64-бит | dotnet publish -c Release -r win-x64 -o out/win-x64 |
| windows 32-бит | dotnet publish -c Release -r win-x86 -o out/win-x86 |
| linux (x64) | dotnet publish -c Release -r linux-x64 -o out/linux-x64 |
| linux (arm64) | dotnet publish -c Release -r linux-arm64 -o out/linux-arm64 |
| macos (apple silicon) | dotnet publish -c Release -r osx-arm64 -o out/mac-arm | 
| macos (intel) | dotnet publish -c Release -r osx-x64 -o out/mac-intel |

## лицензия

[chomik license](LICENSE): указывайте автора и не используйте в коммерческих целях.

## авторы

автор: blaing

спрайты чомика: chomikuj.pl

сделано на [Avalonia](https://avaloniaui.net)

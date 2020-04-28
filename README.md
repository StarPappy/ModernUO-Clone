ModernUO [![Discord](https://img.shields.io/discord/458277173208547350?logo=discord&style=social)](https://discord.gg/VdyCpjQ) [![Subreddit subscribers](https://img.shields.io/reddit/subreddit-subscribers/modernuo?style=social&label=/r/modernuo)](https://www.reddit.com/r/ModernUO/) [![Twitter Follow](https://img.shields.io/twitter/follow/modernuo?label=@modernuo&style=social)](https://twitter.com/modernuo)
=====

##### Ultima Online Server Emulator for the modern era!
[![.NET Core](https://img.shields.io/badge/.NET-Core%203.1-5C2D91)](https://dotnet.microsoft.com/download/dotnet-core/3.1)
![Windows](https://img.shields.io/badge/-server%202019-0078D6?logo=windows)
![OSX](https://img.shields.io/badge/-catalina-222222?logo=apple&logoColor=white)
![Debian](https://img.shields.io/badge/-buster-A81D33?logo=debian)
![Ubuntu](https://img.shields.io/badge/-16LTS-E95420?logo=ubuntu&logoColor=white)
<br/>
[![GitHub stars](https://img.shields.io/github/stars/modernuo/ModernUO?logo=github)](https://github.com/modernuo/ModernUO/stargazers)
[![GitHub issues](https://img.shields.io/github/issues/modernuo/ModernUO?logo=github)](https://github.com/modernuo/ModernUO/issues)
![Build status](https://img.shields.io/circleci/build/gh/modernuo/ModernUO/master?logo=circleci)
[![GitHub license](https://img.shields.io/github/license/modernuo/ModernUO?color=blue)](https://github.com/modernuo/ModernUO/blob/master/LICENSE)

## Goals
- See [Goals](./GOALS.md)

## Publishing a build
#### Requirements
- [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1)

#### Cleaning
- `dotnet clean`

#### Publishing
- Linux: `./Tools/publish-linux.sh` or `Tools/publish-linux.cmd`
- OSX: `./Tools/publish-osx.sh` or `Tools/publish-osx.cmd`
- Windows `./Tools/publish-windows.sh` or `Tools/publish-windows.cmd`

## Deploying / Running Server
- Follow the [publish](https://github.com/modernuo/ModernUO#publishing-a-build) instructions
- Copy `Distribution` directory to production server

#### Requirements
- [.NET Core 3.1 Runtime](https://dotnet.microsoft.com/download/dotnet-core/3.1)
- Zlib
  - Linux: `apt get zlib` or equiv for that distribution
  - OSX: `brew install zlib`
  - Windows: Included during publishing
- Optional: compile and install [Intel DRNG](https://github.com/modernuo/libdrng)

#### Windows
- Run `ModernUO.exe` or `dotnet ModernUO.dll`

#### OSX and Linux
- Run `./ModernUO` or `dotnet ./ModernUO.dll`

## Thanks
- RunUO Team & Community
- ServUO Team & Community
- [Jaedan](https://github.com/jaedan) and the ClassicUO Community

## Troubleshooting / FAQ
- See [FAQ](./FAQ.md)

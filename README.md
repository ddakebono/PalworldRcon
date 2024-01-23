
<div align="center">
<h3 align="center">Palworld Rcon</h3>

  <p align="center">
    A simple WPF/.net 8 Rcon tool for Palworld Dedicated Servers.
    <br />
    <a href="https://github.com/ddakebono/PalworldRcon/issues">Report Bug</a>
    ·
    <a href="https://github.com/ddakebono/PalworldRcon/issues">Request Feature</a>
  </p>
</div>

<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li><a href="#about-the-project">About The Project</a></li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#contribution">Contribution</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#issues">Issues?</a></li>
    <li><a href="#acknowledgments">Acknowledgments</a></li>
  </ol>
</details>

# About the Project
Simple WPF/.net 8 Rcon tool for Palworld Dedicated Servers, this allows you easily use all the RCON usable commands that the Palworld Dedicated Server exposes.

This tool has been put together relatively quickly and likely has some funky issues, it may randomly lock up and require a restart, I'll be looking into it eventually.

## Usage
Simply launch the tool then head over to the settings and point it at your Palworld server, you'll need to have RCON enabled in your PalWorldSettings.ini.

Once that's done you can use test connection or save then hit the connect button on the top bar to get connected to your server, after 3 seconds the players should appear and you'll see your current server version and name.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Contribution
To contribute to this project you'll need to have a functional .Net 8 development environment, to do this you can simply install Visual Studio 2022 and the .Net 8 SDK.

Once that's done you can simply clone this repo and begin fiddling, all dependencies will be brought in by Nuget.

Feel free to submit pull requests, I'll review and merge as needed.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## License
This project is distributed under the MIT License, see `LICENSE` for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Issues?
If you encounter issues feel free to create an issue on this repo, I'll address what I can when it comes to problems, however some may be issues in the dedicated server itself.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Acknowledgments

* [Mahapps.Metro](https://mahapps.com/) for the nice Metro WPF library
* [RconSharp](https://github.com/stefanodriussi/rconsharp) for a decent rcon library that didn't violently explode on Palworld
* [Pocketpair](https://www.pocketpair.jp/) for the silly game that's gone nutty
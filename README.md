![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black)

[README - 日本語版](README_jp.md)

# SmartGitUPM
A plugin for managing packages within Git repositories efficiently in Unity Editor.

> [!IMPORTANT]
> DISCLAIMER: SmartGitUPM is an open-source service, not an official service provided by Unity Technologies Inc.

![Header](Docs/header.png)

# Why Use This Plugin

## View Updates at a Glance

Unity Package Manager doesn’t provide update information for packages published via Git. With `SmartGitUPM`, you can easily check updates with its user-friendly UI.

![Update](Docs/update.jpg)

## Support for Private Repository Update Checks

Supports packages in private repositories (SSL connection). You can check updates even in private repositories.

![Update](Docs/private_repo.jpg)

- Configure SSL and set the SSL URL, for example, `git@github.com:IShix-g/SmartGitUPM.git`
- Large private repositories may take time to display and are not recommended for use.
- The size of a public repository does not affect display speed.
- Installing from a private repository uses the Unity Package Manager (UPM) feature.

## Update Notifications

Receive notifications of package updates every time you open Unity Editor. (Notifications can be turned off)

<img alt="Alert" src="Docs/alert.jpg" width="550"/>

## Uses UPM Internally, So It's Safe

Internally, it uses Unity Package Manager (UPM) for package management, making it reliable. If you don't like `SmartGitUPM`, you can simply remove it. Afterward, manage your packages normally via UPM.

![Update](Docs/upm.jpg)

***

# Getting Started

## Install from Git URL

"Unity Editor : Window > Package Manager > Add package from git URL...".

URL: `https://github.com/IShix-g/SmartGitUPM.git?path=Packages/SmartGitUPM`

![Update](Docs/package_manager.png)

## Open SmartGitUPM

`Unity Editor : Window > Smart Git UPM`

![Update](Docs/open_sgupm.jpg)

## Open Settings

Click on the gear button or the settings button, which is only shown when not configured.

<img alt="Configure" src="Docs/click_configure.jpg" width="550"/>

## Configure Package

| Field           | Description                                   | Example                                                                                                                                                                         |
|-----------------|-----------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Update Notify   | Receive update notifications when the Unity Editor starts? | [Checked] Receive updates                                                                                                                                                      |
| Install Url     | URL for package installation (https or SSL)   | [https] https://github.com/IShix-g/CMSuniVortex.git?path=Packages/CMSuniVortex<br/>[SSL] git@IShix-g-GitHub:IShix-g/UnityJenkinsBuilder.git?path=Assets/Plugins/Jenkins/ |
| Branch          | Specify the main branch                       | e.g., main or master                                                                                                                                                            |

<img alt="SGUPM" src="Docs/setting_package2.jpg" width="550"/>

## Reload

After configuration, click the reload button to complete the setup. Next time, the reload process will run at the following times:

- When you start Unity Editor
- When displaying Smart Git UPM

<img alt="SGUPM" src="Docs/sgupm.jpg" width="500"/>

***

## Button Descriptions

<img alt="Buttons" src="Docs/buttons.jpg" width="500"/>

1. Configure the package
2. Reload package update information
3. Open Unity Package Manager
4. Open Smart Git UPM GitHub page
5. Check for the latest version of Smart Git UPM by clicking

## Current Package State Display

<img alt="States" src="Docs/states.jpg" width="500"/>

1. Latest version installed
2. Installed with a new version available v1.0.9 (current) -> v1.0.10 (new)
3. Installed and version is fixed, can specify a version like `#1.0.0` at the end of the URL to fix
4. Not installed

## Role Sharing with UPM

SmartGitUPM visualizes Git version information and notifies updates that Unity Package Manager does not. Actual installation or uninstallation is delegated to Unity Package Manager.

<img alt="Role" src="Docs/role.jpg" width="600"/>

## Difference from OpenUPM

### OpenUPM
OpenUPM is a registry for open-source packages. Registered packages can be managed through Unity Package Manager, targeting publicly available open-source packages.

### SmartGitUPM
SmartGitUPM does not require registration and allows users to manage the necessary packages themselves. It is capable of flexible management through its unique interface (UI) regardless of public or private packages.

_※ The logos look similar, but it was not intentional. The idea was "**package = cardboard = cat**", thinking of a logo where a cat that loves cardboard plays with a box, and coincidentally looked similar. It's too cute to change, so I'll keep it for a while. Sorry. Thank you._

<img alt="Logos" src="Docs/logos.png" width="600"/>
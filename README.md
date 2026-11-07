![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black)

[README - 日本語版](README_jp.md)

> [!IMPORTANT]
> DISCLAIMER: SmartGitUPM is an open-source service, not an official service provided by Unity Technologies Inc.

# SmartGitUPM
A plugin for managing packages within Git repositories efficiently in Unity Editor.

![Header](Docs/header.png)

## Why Use This Plugin

### View Updates at a Glance

Unity Package Manager doesn’t provide update information for packages published via Git. With `SmartGitUPM`, you can easily check updates with its user-friendly UI.

![Update](Docs/update.jpg)

### Support for Private Repository Update Checks

Supports packages in private repositories (SSH connection). You can check updates even in private repositories.

![Update](Docs/private_repo.jpg)

- Configure SSH and set the SSH URL, for example, `git@github.com:IShix-g/SmartGitUPM.git`
- Large private repositories take longer to display the first time.
- The size of a public repository does not affect display speed.

### Update Notifications

Receive notifications of package updates every time you open Unity Editor. (Notifications can be turned off)

<img alt="Alert" src="Docs/alert.jpg" width="550"/>

### Uses UPM Internally, So It's Safe

Internally, it uses Unity Package Manager (UPM) for package management, making it reliable. If you don't like `SmartGitUPM`, you can simply remove it. Afterward, manage your packages normally via UPM.

![Update](Docs/upm.jpg)

## Getting Started

### Install from Git URL

"Unity Editor : Window > Package Manager > Add package from git URL...".

```
https://github.com/IShix-g/SmartGitUPM.git?path=Packages/SmartGitUPM#v1
```

![Update](Docs/add_package.png)

### Open SmartGitUPM

`Unity Editor : Window > Smart Git UPM`

![Update](Docs/open_sgupm.jpg)

### Open Settings

Click on the gear button or the settings button, which is only shown when not configured.

<img alt="Configure" src="Docs/click_configure.jpg" width="550"/>

### Configure Package

| Field           | Description                                                | Example                                                                                                                                                     |
|-----------------|------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Update Notify   | Receive update notifications when the Unity Editor starts? | [Checked] Receive updates                                                                                                                                   |
| Install Url     | URL for package installation (https or SSH)                | [https] https://github.com/IShix-g/CMSuniVortex.git?path=Packages/CMSuniVortex<br/>[SSH] git@github.com:IShix-g/CMSuniVortex.git?path=Packages/CMSuniVortex |
| Branch (Optional) | Specify the branch name if needed                          | Leave blank, or use `main` or `master`, etc.                                                                                                                |
| Help Page Url (Optional) | Specify the help page URL if needed                        | Leave it blank or provide a help page URL, e.g., https://github.com/IShix-g/CMSuniVortex                                                                    |

<img alt="SGUPM" src="Docs/setting_package2.jpg" width="550"/>

### Reload

After configuration, click the reload button to complete the setup. Next time, the reload process will run at the following times:

- When you start Unity Editor
- When displaying Smart Git UPM

<img alt="SGUPM" src="Docs/sgupm.jpg" width="500"/>

### Plugin Language

You can select the language from the settings. Alerts, warnings, and other messages will be translated into the specified language.

<img alt="localization" src="Docs/localization.jpg" width="500"/>

## Button Descriptions

<img alt="Buttons" src="Docs/buttons.jpg" width="500"/>

1. Display package settings
2. Refresh package update information
3. Open Unity Package Manager
4. Open the Smart Git UPM GitHub page
5. Smart Git UPM version. Click to check for the latest version.
6. Open the help page
7. Install or update all packages
8. Package-specific buttons, with the following states:

| Button Name | On Button Click           | Package State          |
|-------------|---------------------------|------------------------|
| Install     | Installs the package      | Not installed          |
| Update      | Updates the package       | Installed, updates available |
| Remove      | Removes the package       | Latest version installed |

## Current Package State Display

<img alt="States" src="Docs/states.jpg" width="500"/>

1. Latest version installed
2. Installed with a new version available v1.0.9 (current) -> v1.0.10 (new)
3. Installed and version is fixed, can specify a version like `#1.1.4` at the end of the URL to fix
4. Not installed

## Editor API

Use `SmartGitUPM.Editor.SmartGitUPMApi` from Editor code. An assembly definition must reference `SmartGitUPM.Editor` and target the Editor only. Call and await operations sequentially on the Editor main thread. The API opens no windows, dialogs, or progress bars.

Following the existing settings-loading behavior, all API calls, including the two checks, create `Assets/Editor/SmartGitUPM_PackageCollectionSetting.asset` if it does not exist.

```csharp
using System.Threading;
using System.Threading.Tasks;
using SmartGitUPM.Editor;

public static class PackageAutomation
{
    public static async Task RunAsync(CancellationToken token = default)
    {
        bool hasMissing = await SmartGitUPMApi.HasUninstalledPackagesAsync(token);
        bool hasUpdates = await SmartGitUPMApi.HasUpdatesAsync(token);

        // Use the complete Install URL registered in Smart Git UPM, including #revision.
        await SmartGitUPMApi.UpdatePackageAsync(
            "https://github.com/IShix-g/CMSuniVortex.git?path=Packages/CMSuniVortex", token);
        await SmartGitUPMApi.InstallAllAsync(token);
    }
}
```

The checks cover all registered packages, including those with update notifications disabled. The update check fetches current remote information and reports updates for installed packages; a fixed version is compared with that configured version. The missing-package check only needs the UPM installed-package list. With no registrations, both checks return `false` and Install All completes without a UPM request.

`UpdatePackageAsync` uses the registered URL exactly, including its revision, and can also install that registration if it is missing. It checks the selected registration, so an unrelated incomplete registration does not block this operation. An empty or unregistered URL raises `ArgumentException`. Invalid registrations in whole-collection operations and fetch or UPM failures fault the task; cancellation raises `OperationCanceledException`. Cancelling an operation stops waiting, but an already submitted UPM operation may still complete. Await completion and inspect the resulting package state before starting another operation.

### Calling from Unity Pipeline

The API has no dependency on Unity Pipeline. The following connection and command-discovery examples have not been verified against a connected Editor. In a project that already has a compatible `com.unity.pipeline` version and a connected Editor, select that project explicitly and confirm the commands with that version's help and command list:

```sh
unity status --project-path /absolute/path/to/project --json
unity command --project-path /absolute/path/to/project --query eval
```

Use the connected version's C# evaluation command to await the four calls above. Command names and async evaluation syntax vary by Pipeline version; check that version's command help before invoking it. Keep the Editor in the background, and do not open the Smart Git UPM window. Check returned values, task completion, errors, and the resulting UPM package state.

The current [Pipeline requirements](https://docs.unity.com/en-us/unity-cli/unity-pipeline/unity-pipeline-package) specify Unity 6.0 or later. This repository's sample project uses Unity 2022.3. Pipeline verification therefore needs a compatible Unity 6 test project with this package and test registrations. The API does not require upgrading existing Smart Git UPM users or installing Pipeline. Pipeline execution has not been verified in this sample project.

## Role Sharing with UPM

SmartGitUPM visualizes Git version information and notifies updates that Unity Package Manager(UPM) does not. Actual installation or uninstallation is delegated to Unity Package Manager.

<img alt="Role" src="Docs/role.jpg" width="600"/>

## Difference from OpenUPM

### OpenUPM
OpenUPM is a registry for open-source packages. Registered packages can be managed through Unity Package Manager, targeting publicly available open-source packages.

### SmartGitUPM
SmartGitUPM does not require registration and allows users to manage the necessary packages themselves. It is capable of flexible management through its unique interface (UI) regardless of public or private packages.

_※ The logo is almost the same, but I didn't aim for that.**"Package = Cardboard = Cat ”** I tried to make a logo of a cat that loves cardboard boxes playing with them, but it ended up looking like the same logo. I didn't want to change it because it was too cute, so please let me keep it as it is for a while. I'm sorry._

<img alt="Logos" src="Docs/logos.png" width="600"/>

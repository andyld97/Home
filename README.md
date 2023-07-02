# Home

## Welcome
Home is a ``.NET-project/environment`` where you can monitor all your devices (Windows, Linux and Android are supported). Every device 
will be setuped with the appropriate `ACK`-service (this service also starts on startup) which will send ``ACK``-requests continously (every minute) to `Home.API`.
To get this working you need to setup `Home.API` as a self-hosted API on your local server/or in the internet (not recommended)

## Exemplary setup:
![ack](https://user-images.githubusercontent.com/10423894/200111760-793ac13f-bc9d-4b4d-b002-f55c37ec0750.png)

## Supported Devices
- Desktop PC
- Notebooks
- Single Board Computers (e.g. Raspberry PI)
- Smartphones
- Android TVs/Android PCs (Set-Top-Box)
- Android TV Sticks

## Supported operating systems
| OS                    | Supported?      | Service                  | .NET                                | Information                                                   |
|-----------------------|--------------------|--------------------------|-------------------------------------|---------------------------------------------------------------|
| Windows 9x            | :x:                | -                        | ``.NET Framework 2.0``              | -                                                             |
| Windows XP            | :heavy_check_mark: | ``Home.Service.Legacy``  | ``.NET Framework 4.0``              | No remote file access!                                               |
| Windows VISTA         | :heavy_check_mark: | ``Home.Service.Legacy``  | ``.NET Framework 4.0``              | No remote file access!                                        |
| Windows 7 SP1         | :heavy_check_mark: | ``Home.Service.Windows``         | ``.NET Desktop/ASP.NET Core 7.0.x`` | #LEGACY Compiler-Flag (using WebClient instead of HttpClient) |
| Windows 8, 8.1, 10/11 | :heavy_check_mark: | ``Home.Service.Windows``         | ``.NET Desktop/ASP.NET Core 7.0.x`` | -                                                             |
| Rasbpian              | :heavy_check_mark: | ``Home.Service.Linux``   | ``.NET Desktop/ASP.NET Core 7.0.x`` | -                                                             |
| Debian                | :heavy_check_mark: | ``Home.Service.Linux``   | ``.NET Desktop/ASP.NET Core 7.0.x`` | -                                                             |
| Ubuntu (>= 18.04)     | :heavy_check_mark: | ``Home.Service.Linux``   | ``.NET Desktop/ASP.NET Core 7.0.x`` | -                                                             |
| Android 7.0-12.0      | :heavy_check_mark: | ``Home.Service.Android`` | ``Xamarin.Android``                     | No remote file access and NoGL-Version available!                 |

``ASP.NET Core 7.0.x`` is required for remote file access API!

## Features
| Feature                   | Windows (legacy)   | Windows            | Linux                  | Android            |
|---------------------------|--------------------|--------------------|------------------------|--------------------|
| Remote File Access        | :x:                | :heavy_check_mark: (1) | :heavy_check_mark: (1)     | :x:                |
| Screenshots               | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: (2) | :x:                |
| Screenshots per Screen    | :x: | :heavy_check_mark: | :x: | :x:                |
| Hardware Info             | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: (2) | :heavy_check_mark: |
| Performance Counters      | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark:     | Only RAM           |
| Battery Info              | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark:     | :heavy_check_mark: |
| Shutdown/Restart Commands | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark:     | :x:                |
| Message/Execute Command   | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark:     | :x:                |
| Remote Shell              | Not implemented    | Not implemented    | Not implemented        | :x:                |
| Auotmatic Client Updates  | :x:                | :heavy_check_mark: | :heavy_check_mark: (4) | :x:                |
| Wake On LAN (WOL) (3)     | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark:     | :x:                |

(1) Each ack service has an integrated `ASP.NET Core`-Service which is hosted within the service itself (for Windows you need to permit firewall permissons!). It is hosted on http://0.0.0.0:5556 to make it accessible via network (the port can be changed: ``Home.Data.Consts.API_PORT``). This is used for remote file access!

(2) For ``Home.Service.Linux`` some additional tools are required to successfully gather all information and there may be some additional steps required to make screenshots working on Ubuntu > 21.04 (due to Wayland securitiy restrictions).

(3) For `Wake On LAN` your device must support this feature and must be configured properply. In the wiki there is a description/tutorial how you can setup WOL for your device!

(4) To ensure that automatic client updates for `Linux` works correctly you need to setup the service as described in the wiki for `Home.Service.Linux`. If an update is executes the application exists and the service has set a restart timer of `10 seconds`, so without the service the application would not start again!

### Additonal Features
- Access hardware info (cpu, ram, graphics, performance)
- Access device info (startup-time, online-time (how long), oem info (if available))
- A WebHook for events
   - StorageWarning and BatteryWarning
   - Hardware changes (e.g. ``Device "SRV03" detected CPU change. CPU AMD Athlon(tm) II X3 425 Processor got replaced with AMD Athlon(tm) II X3 450 Processor)``
   - When a device goes online/offline (only if your device is classified as a server!)
- Get storage infos of each disk (also on Linux and Android!)
- Create and export a ``HTML``-report of each device (See example [here](https://htmlpreview.github.io/?https://raw.githubusercontent.com/andyld97/Home/dev/Assets/Redmi%20Note%207.html))
- Low storage warning (<= 10% left) per disk
- Low battery warning (<= 10% left)
- Device Log for each device
- Device can have a location and a group (the location is used to group them in the overview by location)
- WOL (Wake On Lan Support), you can boot device in the app
- Device Scheduling Rules: You can schedule device rules
   - Specify a time for a boot/shutdown rule
   - Using a custom mac address (e.g. if system doesn't detects it correctly, or if its from the wrong device)
   - Specify an action (WOL, external API call, exceuting a command etc.)
   - Check out the wiki (https://github.com/andyld97/Home/wiki/Wake-On-LAN---Device-Scheduling) for more information
- Can be used to monitor linux servers (``Home.Service.Linux`` is implemented as ``CLI``)
- A screenshot will be aquired (if supported) if no screenshot is available or if the last screenshot is older than 12h (old screenshots will be removed by the ``HealthCheck-Timer``)
- Home has a ``WPF-Client`` for displaying all devices
   - View all info per device
      - View device log
      - View device activity (in a graph)
      - View the last screenshot (zoom, save, refresh)
      - View all disks (including name, volumes, free space, total space etc.)
      - Access device files (if supported by device): If you double click on a disk you will get a little explorer where you can navigate through all folders. There is also a preview for files and you can also download files and download folders as ZIP. WIP: Upload files (is currently not implemented)
   - You can shutown/restart/execute a command/send a message to the device
   - You can delete devices (if they were wrongly assigned) 
   - It provides a network/device overview of all your devices
      - Tooltip on a device shows the screenshot
      - You can tick "Show screenshots", then you will become an overview of all devices but only with screenshots!  

## Screenshots
![home](https://github.com/andyld97/Home/blob/dev/Assets/screenshots/home.png)
![overview](https://github.com/andyld97/Home/blob/dev/Assets/screenshots/overview.png)

<!--## Preview-Video
https://user-images.githubusercontent.com/10423894/174055660-a2b4c4a1-4a06-48bf-88a7-d0abbce7b3b7.mp4-->

 ## Setup
 
All information about the setup are described in the Wiki (see here: https://github.com/andyld97/Home/wiki)

## Build
To build this solution you need to have ``VS 2019`` installed. Once it is installed and builded you can continue using ``VS 2022``. This is related to the fact, that ``VS 2022`` doesn't supports ``.NET 4.0`` and older, but obviously for older Windows versions (legacy) it is required to use ``.NET Framework 4.0`` or even ``.NET Framework 2.0``.

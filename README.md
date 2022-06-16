# Home

## Welcome
Home is a ``.NET-project/environment`` where you can monitor all your devices (Windows, Linux and Android are supported). Every device 
will be setuped with the appropriate `ACK`-service (this service also starts on startup) which will send ``ACK``-requests continously (every minute) to `Home.API`.
To get this working you need to setup `Home.API` as a self-hosted API on your local server/or in the internet (not recommended)

## Exemplary setup:

![ack](https://user-images.githubusercontent.com/10423894/173809046-37eaddea-b106-40d7-ac3a-661642a3a2f8.png)

## Supported Devices
- Desktop PC
- Notebooks
- Single Board Computers (e.g. Raspberry PI)
- Smartphones
- Android TVs/Android PCs (Set-Top-Box)

## Supported operating systems
| OS                    | Supported?      | Service                  | .NET                                | Information                                                   |
|-----------------------|--------------------|--------------------------|-------------------------------------|---------------------------------------------------------------|
| Windows 9x            | :x:                | -                        | ``.NET Framework 2.0``              | -                                                             |
| Windows XP            | :heavy_check_mark: | ``Home.Service.Legacy``  | ``.NET Framework 4.0``              | No file access!                                               |
| Windows VISTA         | :heavy_check_mark: | ``Home.Service.Legacy``  | ``.NET Framework 4.0``              | No remote file access!                                        |
| Windows 7 SP1         | :heavy_check_mark: | ``Home.Service``         | ``.NET Desktop/ASP.NET Core 6.0.x`` | #LEGACY Compiler-Flag (using WebClient instead of HttpClient) |
| Windows 8, 8.1, 10/11 | :heavy_check_mark: | ``Home.Service``         | ``.NET Desktop/ASP.NET Core 6.0.x`` | -                                                             |
| Rasbpian              | :heavy_check_mark: | ``Home.Service.Linux``   | ``.NET Desktop/ASP.NET Core 6.0.x`` | -                                                             |
| Debian                | :heavy_check_mark: | ``Home.Service.Linux``   | ``.NET Desktop/ASP.NET Core 6.0.x`` | -                                                             |
| Ubuntu (>= 18.04)     | :heavy_check_mark: | ``Home.Service.Linux``   | ``.NET Desktop/ASP.NET Core 6.0.x`` | -                                                             |
| Android 7.0-12.0      | :heavy_check_mark: | ``Home.Service.Android`` | ``Xamarin.Android``                     | No remote file access and NoGL-Version available!                 |

``ASP.NET Core 6.0.x`` is required for remote file access API!

Screenshot


## Features
| Feature                   | Windows (legacy)   | Windows            | Linux                  | Android            |
|---------------------------|--------------------|--------------------|------------------------|--------------------|
| Remote File Access        | :x:                | :heavy_check_mark: | :heavy_check_mark:     | :x:                |
| Screenshots               | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: (*) | :x:                |
| Hardware Info             | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: (*) | :heavy_check_mark: |
| Performance Counters      | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark:     | Only RAM           |
| Battery Info              | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark:     | :heavy_check_mark: |
| Shutdown/Restart Commands | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark:     | :x:                |
| Message/Execute Command   | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark:     | :x:                |
| Remote Shell              | Not implemented    | Not implemented    | Not implemented        | :x:                |

(*) For ``Home.Service.Linux`` some additional tools are required to successfully gather all information and there may be some additional steps required to make screenshots working on Ubuntu > 21.04 (due to Wayland securitiy restrictions).

### Additonal Features
- Access hardware info (cpu, ram, graphics, performance)
- Access device info (startup-time, online-time (how long), oem info (if available))
- A WebHook for events
   - StorageWarning and BatteryWarning
   - Hardware changes (e.g. ``Device "SRV03" detected CPU change. CPU AMD Athlon(tm) II X3 425 Processor got replaced with AMD Athlon(tm) II X3 450 Processor)``
   - When a device goes online/offline (only if your device is classified as a server!)
- Get storage infos of each disk (also on Linux and Android!)
- Low storage warning (<= 10% left) per disk
- Low battery warning (<= 10% left)
- Device Log for each device
- Device can have a location and a group (the location is used to group them in the overview by location)
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

## Home.Service.Windows
### Additional Information
### Setup

## Home.Service.Linux
### Additional Information
### Setup

## Home.Service.Android
### Additional Information
### Setup

## Build
To build this solution you need to have ``VS 2019``. Once it is setuped and builded there you can continue using ``VS 2022``. This is related to the fact, that ``VS 2022`` doesn't supports ``.NET 4.0`` and older, but obviously for older Windows versions (legacy) it is required to use ``.NET Framework 4.0`` or even ``.NET Framework 2.0``.

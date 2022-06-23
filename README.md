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

## Features
| Feature                   | Windows (legacy)   | Windows            | Linux                  | Android            |
|---------------------------|--------------------|--------------------|------------------------|--------------------|
| Remote File Access        | :x:                | :heavy_check_mark: (1) | :heavy_check_mark: (1)     | :x:                |
| Screenshots               | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: (2) | :x:                |
| Hardware Info             | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: (2) | :heavy_check_mark: |
| Performance Counters      | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark:     | Only RAM           |
| Battery Info              | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark:     | :heavy_check_mark: |
| Shutdown/Restart Commands | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark:     | :x:                |
| Message/Execute Command   | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark:     | :x:                |
| Remote Shell              | Not implemented    | Not implemented    | Not implemented        | :x:                |

(1) Each ack service has an integrated ``ASP.NET Core``-Service which is hosted within the service itself (for Windows you need to permit firewall permissons!). It is hosted on http://0.0.0.0:5556 to make it accessible via network (the port can be changed: ``Home.Data.Consts.API_PORT``). This is used for remote file access!

(2) For ``Home.Service.Linux`` some additional tools are required to successfully gather all information and there may be some additional steps required to make screenshots working on Ubuntu > 21.04 (due to Wayland securitiy restrictions).

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

## Preview-Video
https://user-images.githubusercontent.com/10423894/174055660-a2b4c4a1-4a06-48bf-88a7-d0abbce7b3b7.mp4

--- 
## Setup

## API

### ``config.json``

```json
{
  "host": "http://localhost:5250",
  "use_webhook": false,
  "webhook_url": null,
  "health_check_timer_interval": "00:00:05",
  "remove_inactive_gui_clients": "00:10:00",
  "remove_inactive_clients": "00:03:00",
  "aquire_new_screenshot": "12:00:00",
  "remove_old_screenshots": "1.00:00:00",
  "storage_warning_percentage": 10,
  "battery_warning_percentage": 10
}
```
- The default configuration is already working, you don't need to modify it, if you're using the same port (localhost)
- This configuration requires a reverse proxy (see https://docs.microsoft.com/en-US/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-6.0)   
But you can also use ``http://0.0.0.0:5250`` otherwise!

## Setup
1. Locate your ``dotnet`` installation (.NET Core 6.0.x and ASP.NET Core 6.0.x are both required!)
2. Create a service file at ``/etc/systemd/system/homeapi.service``:

```
[Unit]
Description=HomeAPI Service

[Service]
WorkingDirectory=/media/server/Server/andy/home
ExecStart=/usr/bin/dotnet /media/server/Server/andy/home/Home.API.dll
Restart=always
# Restart service after 10 seconds if the dotnet service crashes:
RestartSec=10
SyslogIdentifier=homeapi
User=root

[Install]
WantedBy=multi-user.target
```
- Make sure your pathes are correctly setuped!
3. Enable service: ``sudo systemctl enable homeapi.service``
4. Start service: ``sudo systemctl start homeapi.service``

- You can also maintain the service with ``sudo service homeapi status``!

## Home.Service.Windows/Legacy - Client
### Setup
1. Download and extract all files
2. Open ``Home.Service.Windows.exe`` or ``Home.Service.Legacy.exe`` and follow the instructions!
3. Copy a link to the executable file to ``shell:startup`` (to enable autostart)

![Home.Service.Windows-Screenshot](https://github.com/andyld97/Home/blob/dev/Assets/screenshots/Home.Service.Windows.png)
   
## Home.Service.Linux - Client
### Additional Information

To fix black screenshots with scrot on Ubuntu >= 21.10 see:
https://askubuntu.com/a/1347651/692902

Commands/Tools used:
- ``sudo apt install scrot``
- ``sudo apt install sysstat``
- ``lshw`` (make sure the version installed generates valid ``json``-data)
- Ensure that both scripts ``screenshot.sh`` and ``hw.sh`` are executable (using ``chmod -x``)

### Setup
1. Edit config.json (comments should be removed!):
```json
{
  "id": null, /* Will be set automatically */
  "user": "server", /* Should not be root, but can be like pi */
  "api": "http://192.168.178.38:83",
  "location": "Room 518",
  "device_group": null,
  "os": 1, /* Android = 0 | Linux = 1 | LinuxMint = 2 | LinuxUbuntu = 3 | Windows = 4-9 | Unix = 10 | Other = 11 */
  "type": 0, /* SingleBoardDevice = 0 | MiniPC = 1 | Notebook = 2 | Desktop = 3 | Server = 4 */
  "is_signed_in": false /* should be false on first start! */
}
```
2. Locate your ``dotnet`` installation (.NET Core 6.0.x and ASP.NET Core 6.0.x are both required!)
3. Create a service file at ``/etc/systemd/system/home.service``:

```
Description=HomeClient Service

[Service]
WorkingDirectory=/media/server/Server/andy/home.client
ExecStart=/usr/bin/dotnet /media/server/Server/andy/home.client/Home.Service.Linux.dll
Restart=always
# Restart service after 10 seconds if the dotnet service crashes:
RestartSec=10
SyslogIdentifier=homeclient
User=root

[Install]
WantedBy=multi-user.target
```
- Make sure your pathes are correctly setuped!
4. Enable service: ``sudo systemctl enable home.service``
5. Start service: ``sudo systemctl start home.service``

- You can also maintain the service with ``sudo service home status``!

## Home.Service.Android - Client
- Deploy via Visual Studio or
- Install the APK and follow the instructions on your device.

![Home.Service.Android-Screenshot](https://github.com/andyld97/Home/blob/dev/Assets/screenshots/Home.Service.Android.jpg)
---

## Build
To build this solution you need to have ``VS 2019`` installed. Once it is installed and builded you can continue using ``VS 2022``. This is related to the fact, that ``VS 2022`` doesn't supports ``.NET 4.0`` and older, but obviously for older Windows versions (legacy) it is required to use ``.NET Framework 4.0`` or even ``.NET Framework 2.0``.

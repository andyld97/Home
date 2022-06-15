# Home

## Welcome
Home is a project/environment where you can monitor all your devices (Windows, Linux and Android are supported). Every device 
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
| Windows XP            | :heavy_check_mark: | ``Home.Service.Legacy``  | ``.NET Framework 4.8``              | No file access!                                               |
| Windows VISTA         | :heavy_check_mark: | ``Home.Service.Legacy``  | ``.NET Framework 4.8``              | No remote file access!                                        |
| Windows 7 SP1         | :heavy_check_mark: | ``Home.Service``         | ``.NET Desktop/ASP.NET Core 6.0.x`` | #LEGACY Compiler-Flag (using WebClient instead of HttpClient) |
| Windows 8, 8.1, 10/11 | :heavy_check_mark: | ``Home.Service``         | ``.NET Desktop/ASP.NET Core 6.0.x`` | -                                                             |
| Rasbpian              | :heavy_check_mark: | ``Home.Service.Linux``   | ``.NET Desktop/ASP.NET Core 6.0.x`` | -                                                             |
| Debian                | :heavy_check_mark: | ``Home.Service.Linux``   | ``.NET Desktop/ASP.NET Core 6.0.x`` | -                                                             |
| Ubuntu (>= 18.04)     | :heavy_check_mark: | ``Home.Service.Linux``   | ``.NET Desktop/ASP.NET Core 6.0.x`` | -                                                             |
| Android 7.0-12.0      | :heavy_check_mark: | ``Home.Service.Android`` | Xamarin.Android                     | No remote file access and NoGL-Version available!                 |

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

(*) For ``Home.Service.Linux`` some additional tools are required to successfully gather all information and there may be some additional steps required to make screenshots working on Ubuntu > 21.04.

## Home.Service.Windows
### Additional Information
### Setup

## Home.Service.Linux
### Additional Information
### Setup

## Home.Service.Android
### Additional Information
### Setup

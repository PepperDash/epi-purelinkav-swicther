# Pure Link Plugin

The Pure Link Plugin provides device control utilizing the configuration object defined in this document.

## Disclaimer

This was my first EPI which was built and tested on the bench with a Pure-Link Media Axis-20 (20x20) switcher using TCP/IP as the control method. This EPI was only designed for TCP/IP Crestron integration (RS-232/serial control not tested nor suggested). The EPI handles two forms of API calls using the configuration 'Model' object detailed below. **The SIMPL Windows bridge analog video and audio request signals must use the value of '999' to trigger a route to clear as the value of 0 is not evaluated.** See details below regarding development environment.

## Installation

The *.cplz output of this plugin needs to be loaded to the processor in the corresponding program folder:

```
/USER/program[X]/plugins/
```

## Configuration

An example configuration is provided to assist in implementation.

### Device Configuration

The device configuration provides an example of the Pure Link device, control method, sources, and destinations.

```
{
	"key": "pure-link-1",
	"name": "Pure Link",
	"type": "pureLink",
	"group": "plugin",
	"properties": {
		"parentDeviceKey": "processor",
		"control": {
			"method": "tcpIp",
			"tcpSshProperties": {
				"address": "172.16.0.202",
				"port": 23,
				"autoReconnect": true,
				"autoReconnectIntervalMs": 10000,
				"username": "",
				"password": ""
			}
		},
		"pollTimeMs": 60000,
		"pollString": "*255H000",
		"warningTimeoutMs": 120000,
		"errorTimeoutMs": 180000,
		"deviceId": 255,
		"model": 1,
		"inputs": {
			"1":{
				"name": "Input 1"
			},
			"2":{
				"name": "Input 2"
			},
			"3":{
				"name": "Input 3"
			},
			"4":{
				"name": "Input 4"
			},
			"5":{
				"name": "Input 5"
			},
			"6":{
				"name": "Input 6"
			},
			"7":{
				"name": "Input 7"
			},
			"8":{
				"name": "Input 8"
			},
			"9":{
				"name": "Input 9"
			},
			"10":{
				"name": "Input 10"
			},
			"11":{
				"name": "Input 11"
			},
			"12":{
				"name": "Input 12"
			},
			"13":{
				"name": "Input 13"
			},
			"14":{
				"name": "Input 14"
			},
			"15":{
				"name": "Input 15"
			},
			"16":{
				"name": "Input 16"
			},
			"17":{
				"name": "Input 17"
			},
			"18":{
				"name": "Input 18"
			},
			"19":{
				"name": "Input 19"
			},
			"20":{
				"name": "Input 20"
			}						
		},
		"outputs":{
			"1":{
				"name": "Output 1"
			},
			"2":{
				"name": "Output 2"
			},
			"3":{
				"name": "Output 3"
			},
			"4":{
				"name": "Output 4"
			},
			"5":{
				"name": "Output 5"
			},
			"6":{
				"name": "Output 6"
			},
			"7":{
				"name": "Output 7"
			},
			"8":{
				"name": "Output 8"
			},
			"9":{
				"name": "Output 9"
			},
			"10":{
				"name": "Output 10"
			},
			"11":{
				"name": "Output 11"
			},
			"12":{
				"name": "Output 12"
			},
			"13":{
				"name": "Output 13"
			},
			"14":{
				"name": "Output 14"
			},
			"15":{
				"name": "Output 15"
			},
			"16":{
				"name": "Output 16"
			},
			"17":{
				"name": "Output 17"
			},
			"18":{
				"name": "Output 18"
			},
			"19":{
				"name": "Output 19"
			},
			"20":{
				"name": "Output 20"
			}	
		}
	}
}
```

### Bridge Configuration

It is important to note the Pure Link Plugin is built on the Essentials Plugin Template and uses the **eiscApiAdvanced** type.  The following configuration is an example of the Bridge configuration.

```
{
	{
		"key": "app1-switcher-bridge-1",
		"uid": 2,
		"name": "Switcher Bridge 1",
		"group": "api",
		"type": "eiscApiAdvanced",
		"properties": {
			"control": {
				"tcpSshProperties": {
					"address": "127.0.0.2",
					"port": 0
				},
				"ipid": "B0",
				"method": "ipidTcp"
			},
			"devices": [
				{
					"deviceKey": "pure-link-1",
					"joinStart": 1
				}
			]
		}
	}
}
```

## SiMPL Bridge Joins

### Digitals
| dig-o            | I/O | dig-i                    |
|------------------|-----|--------------------------|
| Video Enter      | 1   | IsOnline                 |
| Audio Enter      | 2   |                          |
| Audio Breakaway  | 3   | Audio Breakaway Feedback |
| Poll             | 6   |                          |
| Poll Video       | 7   |                          |
| Poll Audio       | 8   |                          |
| Connect          | 11  | Connect Feedback         |
| Disconnect       | 12  |                          |
| Clear Video      | 15  |                          |
| Clear Audio      | 16  |                          |


## Analogs
| an_o                             | I/O       | an_i                               |
|----------------------------------|-----------|------------------------------------|
|                                  | 1         | Communication Status               |
| Model                            | 5         |                                    |
| Output001-XXX	Video Input Select | 101-MAXIO | Output001-XXX Video Input Feedback |
| Output001-XXX	Audio Input Select | 301-MAXIO | Output001-XXX Audio Input Feedback |


## Serials
| serial-o | I/O        | serial-i                                |
|----------|------------|-----------------------------------------|
|          | 1          | Switcher Name                           |
|          | 101-MAXIO  | Input001-XXX Names                      |
|          | 301-MAXIO  | Output001-XXX Names                     |
|          | 501-MAXIO  | InputVideo001-XXX Names                 |
|          | 701-MAXIO  | InputAudio001-XXX Names                 |
|          | 901-MAXIO  | OutputVideo001-XXX Names                |
|          | 1001-MAXIO | OutputAudio001-XXX Names                |
|          | 2001-MAXIO | CurrentOutputVideo001-XXX Names         |
|          | 2201-MAXIO | CurrentOutputAudio001-XXX Names         |


## Model

The Pure Link switcher has two forms of API calls for routes made based on the switcher model. Model PM-128X and PM-256X, input and output # uses three digit format, instead of two digits.
Model analog value 0 = 2 character input & output. Example *255CI01O02<br>
Model analog value 1 = 3 character input & output. Example *255CI001O002<br>


## Development Environment 

| Hardware                    | Firmware                                 | 
|-----------------------------|------------------------------------------|
| Pure Link Media Axis MAX-20 | Firmware: MAX-020-V1.12                  |
| Crestron CP3                | Firmware: 1.603.4242.36971 (Aug 13 2020) |
| Private LAN/172.16.0.X      | N/A                                      |

| Software                 | Version        | 
|--------------------------|----------------|
| Visual Studio 2008       | 9.0.30729.1 SP |
| Microsoft .NET Framework | 3.5 SP1        |
| SIMPL Windows            | 4.14.21.00     |
| SIMPL+ Cross Compiler    | 1.3            | 
| SIMPL Windows Library    | 508            | 
| Device Database          | 200.20.002.00  | 
| Crestron Database        | 202.00.001.00  | 


## Feature Wishlist

• Outbound communication queue<br>
• Source sync detection<br>

## Manufacturer Details

• [PureLink Website](https://www.purelinkav.com/product-category/matrix-switchers/media-axis/)<br>
• Media Axis™ is the world’s first large-scale matrix switching and extension system supporting Ultra HD/4K60 4:4:4 via Native and IP architectures. With its dynamic, proprietary design features and technologies, Media Axis™ provides a truly adaptive solution that works where and how you need it.<br>
• The Media Axis Matrix Switcher supports the following digital interfaces:<br>
✓ HDMI v2.0b (w/scaling)<br>
✓ 12G-SDI (standard scaling)<br>
✓ CATx (HDBaseT) (w/scaling)<br>
✓ Fiber (w/scaling)<br>
✓ IP (10G)<br>
✓ Dante™/AES67 (IP audio)<br>

• Each Media Axis™ Matrix Switcher Chassis is custom-assembled from field-upgradeable Input/Output cards:<br>
✓ The MAX-20 can support a combination of up to (5) I/O cards<br>
✓ The MAX-36 can support a combination of up to (9) I/O cards<br>
✓ The MAX-72 can support a combination of up to (18) I/O cards<br>
✓ The MAX-144 can support a combination of up to (36) I/O cards<br>
✓ The MAX-216 can support a combination of up to (54) I/O cards<br>

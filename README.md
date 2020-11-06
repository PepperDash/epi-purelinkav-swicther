# Pure Link Plugin

The Pure Link Plugin provides device control utilizing the configuration object defined in this document.

## Installation

The *.cplz output of this plugin needs to be loaded to the processor in the corresponding program folder:

```
/USER/program[X]/plugins/
```

## Configuration

An example configuration is provided to assist in implementation.

### Device Configuration

The device configuration provides an example of the Pure Link device, control method, levels, sources, and destinations.

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
				"address": "10.0.10.109",
				"port": 21,
				"autoReconnect": true,
				"autoReconnectIntervalMs": 10000,
				"username": "",
				"password": ""
			}
		},
		"pollTime": 60000,
		"warningTimeout": 120000,
		"errorTimeout": 180000,
		"videoLevel": "V",
		"audioLevel":"A",
		"usbLevel": "C",
		"audioFollowsVideo": false,
		"usbFollowsVideo": false,
		"input": {
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
			}
		},
		"destinations":{
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
| Connect          | 1   | Connect Feedback         |
| Audio Breakaway  | 4   | Audio Breakaway Feedback |
| USB Breakaway    | 5   | USB Breakaway Feedback   |
| Poll             | 6   |                          |
| Poll Labels      | 7   |                          |
| Poll Crosspoints | 8   |                          |
|                  | 11  | Online Feedback          |


## Analogs
| an_o                           | I/O     | an_i                               |
|----------------------------------|---------|------------------------------------|
|                                  | 1       | Communication Status               |
| Output001-XXX	Video Input Select | 101-300 | Output001-XXX Video Input Feedback |
| Output001-XXX	Audio Input Select | 301-500 | Output001-XXX Audio Input Feedback |
| Output001-XXX	USB Input Select   | 301-500 | Output001-XXX USB Input Feedback   |

## Serials
| serial-o | I/O       | serial-i                                |
|----------|-----------|-----------------------------------------|
|          | 1         | Switcher Name                           |
|          | 101-300   | Input001-XXX Names                      |
|          | 301-500   | Output001-XXX Names                     |

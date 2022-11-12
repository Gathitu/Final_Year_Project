An Industry 4.0 Elevator system that uses IoT and Digital Twinning for real-time floor-to-floor control of a physical elevator and visualization of its movement; both offline an online.
The microcontroller used is an ESP32 due to its Bluetooth and WiFi capabilities.

How the physical elevator system works + Integration with an Augmented-reality android app:
https://www.linkedin.com/posts/benson-gathitu_digital-twinning-and-iot-application-in-a-activity-6986590255340531712-04jX?utm_source=share&utm_medium=member_android

Web app preview: https://elevator-iot-server.herokuapp.com/



            OPERATION

• Circuit functionality 

The physical control device is the ESP32. 
There are buttons for each floor and limit 
switches for detecting floor position. A
one digit seven segment LED shows the 
floor position while the RGB led shows if 
the system is offline or online and if the 
elevator is in motion or at a specific floor.
 

• Bluetooth Functionality

Bluetooth is a frequency-hopping radio 
technology that transmits data packets 
within the 2.4 GHz band. These packets 
exchange through one of 79 designated 
Bluetooth channels (each of which is 1 MHz 
in bandwidth).
There are 2 forms of Bluetooth:
 1. Bluetooth Classic
 2. Bluetooth Low Energy

The difference between the two is that 
Bluetooth Classic can handle a lot of data 
but quickly consumes battery life and costs a 
lot more. Bluetooth Low Energy is used for 
applications that do not need to exchange 
large amounts of data and can run on battery 
power for years at a cheaper cost.
Bluetooth Classic is used in audio streaming 
and therefore was used in the project to 
stream audio to a Bluetooth speaker. The 
ESP32 microcontroller provides a Bluetooth 
A2DP (Classic) API which can be used to 
generate sound and send it to a Bluetooth 
sink i.e., a Bluetooth Speaker in our case.


• Offline mode

The ESP32 creates a hotspot and a local 
server that can be accessed by any device 
within its LAN. The local server can be 
accessed using an IP address. If a device 
joins the ESP32 network and one enters the 
IP in a web browser, it serves a webpage. 
The files that make up the webpage are 
stored in ESP32 in a zip file folder in order 
to save on storage space and is then 
unzipped by a script file also stored in the 
ESP32. 


• Online mode



A NodeJS cloud server stores the webpage files and allows 
connection to any device provided the 
device is connected to the internet. The 
ESP32, due to its Wi-Fi capabilities, is 
connected to the internet by connecting it to 
a Wi-Fi connection that has internet access. 
The cloud server has web browser of a 
device that is connected to the internet, 
allowing the device to connect to the cloud 
server. The cloud server in this case acts as 
an intermediary between the device and the 
ESP32. 


• Augmented Reality

Virtual digital twin was designed using 
Siemens NX and Blender softwares and then 
packaged in a web app and an android app. 
The android platform uses augmented 
virtual reality to visualize the digital twin.  

#include <WiFi.h>
#include <WebServer.h>
#include <WebSocketsServer.h>
#include <WebSocketsClient.h>
#include <ArduinoJson.h>

WebServer server(80);

WebSocketsServer socketServer = WebSocketsServer(81);
WebSocketsClient socketClient;

bool initializedWebSockets = false;
enum WebSocketTypes {Server, Client};
//enum WebSocketTypes currentWebSocket = Server;
enum WebSocketTypes currentWebSocket = Client;

//String webPage = "<!DOCTYPE html><html><head><title>Elevator IOT And Digital Twinning</title></head><body style='background-color: #EEEEEE;'><span style='color: #003366;'><h1>Elevator IOT And Digital Twinning</h1><p>-----</p></span></body></html>";
//String webPage = "<!DOCTYPE html><html><head><title>Elevator IOT And Digital Twinning</title></head><body style='background-color: #EEEEEE;'><span style='color: #003366;'><h1>Elevator IOT And Digital Twinning</h1><p>Random number is: <span id='rand'>-</span> </p><p><button type='button' id='BTN_SEND_BACK'>Send info to ESP32</button></p></span></body><script> var Socket; document.getElementById('BTN_SEND_BACK').addEventListener('click', button_send_back); function init() { Socket = new WebSocket('ws://' + window.location.hostname + ':81/'); Socket.onmessage = function(event) { processCommand(event); }; } function button_send_back() { Socket.send('Sending back some random stuff'); } function processCommand(event) { document.getElementById('rand').innerHTML = event.data; console.log(event.data); } window.onload = function(event) { init(); }</script></html>";
String webPage = "<!DOCTYPE html><html><head><title>Elevator IOT And Digital Twinning</title></head><body style='background-color: #EEEEEE;'><span style='color: #003366;'><h1>Elevator IOT And Digital Twinning</h1><p>First Random number is: <span id='rand1'>-</span> </p><p>Second Random number is: <span id='rand2'>-</span> </p><p><button type='button' id='BTN_SEND_BACK'>Send info to ESP32</button></p></span></body><script> var Socket; document.getElementById('BTN_SEND_BACK').addEventListener('click', button_send_back); function init() { Socket = new WebSocket('ws://' + window.location.hostname + ':81/'); Socket.onmessage = function(event) { processCommand(event); }; } function button_send_back() { var msg = {distance: 2,angularVelocity: 36,linearVelocity: 20,floor: 2,motion: 1,direction: 1,};Socket.send(JSON.stringify(msg)); } function processCommand(event) {var obj = JSON.parse(event.data);document.getElementById('rand1').innerHTML = obj.rand1;document.getElementById('rand2').innerHTML = obj.rand2; console.log(obj.rand1);console.log(obj.rand2); } window.onload = function(event) { init(); }</script></html>";

/// The JSON library uses static memory, so this will need to be allocated:
StaticJsonDocument<200> doc_tx;                       // provision memory for about 200 characters
StaticJsonDocument<200> doc_rx;

unsigned int interval = 2000;                                  // send data to the client every 1000ms -> 1s
unsigned long previousMillis = 0;                     // we use the "millis()" command for time reference and this will output an unsigned long

void setup() {

  Serial.begin(115200);
  


}

void loop() {
  if(!initializedWebSockets){
    initializeWebSockets();
    return;
  }
  
  if(currentWebSocket == Server){
    server.handleClient();
    socketServer.loop();
  }else if(currentWebSocket == Client){
    socketClient.loop();
  } 

  unsigned long now = millis();                       // read out the current "time" ("millis()" gives the time in ms since the Arduino started)
  if ((unsigned long)(now - previousMillis) >= interval) { // check if "interval" ms has passed since last time the clients were updated

    String jsonString = "";                           // create a JSON string for sending data to the client
    JsonObject object = doc_tx.to<JsonObject>();      // create a JSON Object
    object["rand1"] = random(100);                    // write data into the JSON object -> I used "rand1" and "rand2" here, but you can use anything else
    object["rand2"] = random(100);
    serializeJson(doc_tx, jsonString);                // convert JSON object to string
    socketServer.broadcastTXT(jsonString);               // send JSON string to clients

    previousMillis = now;                          // reset previousMillis
  }

}

void initializeWebSockets(){
  if(currentWebSocket == Client){
    setUpWiFiStationMode(); 
  }
  if(currentWebSocket == Server){///placed after 1st consition so that failure of connection to WiFi will lead to creation of one
    setUpSoftAccessPoint();  
  }
  initializedWebSockets = true;
}

void setUpSoftAccessPoint() {
  const char* ssid = "NODEMCU WIFI";
  const char* password = "1234567890";

  // Configure IP addresses of the local access point
  IPAddress local_IP(192, 168, 1, 22);
  IPAddress gateway(192, 168, 1, 5);
  IPAddress subnet(255, 255, 255, 0);

  //  Serial.print("Configuring Access Point ... ");
  //  Serial.println(WiFi.softAPConfig(local_IP, gateway, subnet) ? "Ready" : "Failed!");

  Serial.print("Setting up Access Point ... ");
  //  Serial.println(WiFi.softAP(ssid, password) ? "Ready" : "Failed!"); ///Protected network
  Serial.println(WiFi.softAP(ssid) ? "Ready" : "Failed!"); ///Open network

  Serial.print("IP address = ");
  Serial.println(WiFi.softAPIP());

  setUpWebServer();
  setUpWebSocketsServer();
}

void setUpWiFiStationMode() {
  const char* ssid = "Dumb HP";
  const char* password = "123456790";
  WiFi.begin(ssid, password);
  Serial.println("Establishing connection to WIFI " + String(ssid));
  Serial.print("Connecting to WiFi.");
  unsigned int checkCounts = 0;
  while (WiFi.status() != WL_CONNECTED && checkCounts<12) {///try connecting to WiFi for 12 seconds
    delay(1000);
    checkCounts +=1;
    Serial.print(".");
  }
  Serial.print("");
  if(checkCounts <12){
    Serial.println("Connected to WiFi" + String(ssid));
    Serial.print("IP address = ");
    Serial.println(WiFi.localIP());
    setUpWebSocketsClient();
  }else{
    currentWebSocket = Server;
  }
  
}

void setUpWebServer() {
  server.on("/", []() {
    Serial.println("Home Page Accessed");
    server.send(200, "text/html", webPage);
  });

  server.begin();
  Serial.println("WebServer Initialized");
}

void setUpWebSocketsServer() {
  socketServer.begin();
  socketServer.onEvent(manageWebSocketServerEvent);
  Serial.println("Initialized WebSockets Server");
}

void setUpWebSocketsClient() {
  const char* serverIPAddress = "http://192.168.4.1/";
  const unsigned int serverPort = 81;
  socketClient.begin(serverIPAddress, serverPort, "/");
  socketClient.onEvent(manageWebSocketClientEvent);
  socketClient.setReconnectInterval(5000);// if connection failed retry every 5s
  Serial.println("Initialized WebSockets Client");
}



void manageWebSocketServerEvent(byte clientID, WStype_t type, uint8_t * payload, size_t length) {//type: type of message, payload: actual data sent and length(array of characters): length of payload
  switch (type) {                                     // switch on the type of information sent
    case WStype_DISCONNECTED:                         //a client is disconnected
      Serial.println("Client " + String(clientID) + " disconnected");
      break;
    case WStype_CONNECTED:                            // a client is connected
      Serial.println("Client " + String(clientID) + " connected");
      break;
      
    case WStype_TEXT:                                 // if a client has sent data, then type == WStype_TEXT
      DeserializationError error = deserializeJson(doc_rx, payload);// try to decipher the JSON string received
      if (error) {
        Serial.print(F("deserializeJson() failed: "));
        Serial.println(error.f_str());
        return;
      }
      else {///break down JSON
        const unsigned int angularVelocity = doc_rx["angularVelocity"];
        const unsigned int linearVelocity = doc_rx["linearVelocity"];
        const unsigned int currentFloor = doc_rx["floor"];
        const unsigned int motion = doc_rx["motion"];
        const unsigned int elevatorDirection = doc_rx["direction"];
        Serial.println("Received info from user: " + String(clientID));
        Serial.println("Angular Velocity: " + String(angularVelocity));
        Serial.println("Linear Velocity: " + String(linearVelocity));
        Serial.println("Current Floor: " + String(currentFloor));
        Serial.println("Motion: " + String(motion));
        Serial.println("Direction: " + String(elevatorDirection));

        
//         initializedWebSockets = false;]
//         currentWebSocket = Server;
      }
      Serial.println("");
      break;
  }
}

void manageWebSocketClientEvent(WStype_t type, uint8_t * payload, size_t length) {
    switch (type) {                                     // switch on the type of information sent
      case WStype_DISCONNECTED:                         //a client is disconnected
        Serial.println("Disconnected from Server");
        break;
      case WStype_CONNECTED:                            // a client is connected
        Serial.println("Connected to Server");
        break;
        
      case WStype_TEXT:   // if a client has sent data, then type == WStype_TEXT
        DeserializationError error = deserializeJson(doc_tx, payload); // deserialize incoming Json String
        if (error) { // Print error msg if incoming String is not JSON formated
          Serial.print(F("deserializeJson() failed: "));
          Serial.println(error.c_str());
          return;
        }
        else {///break down JSON
          const unsigned int rand1 = doc_tx["rand1"];
          const unsigned int rand2 = doc_tx["rand2"];
          Serial.println("rand1: " + String(rand1));
          Serial.println("rand2: " + String(rand2));
        }
        Serial.println("");
        break;
    }
  
//  if (type == WStype_TEXT) {
//    DeserializationError error = deserializeJson(doc_tx, payload); // deserialize incoming Json String
//    if (error) { // Print error msg if incoming String is not JSON formated
//      Serial.print(F("deserializeJson() failed: "));
//      Serial.println(error.c_str());
//      return;
//    }
//    const unsigned int rand1 = doc_tx["rand1"];
//    const unsigned int rand2 = doc_tx["rand2"];
//    Serial.println("rand1: " + String(rand1));
//    Serial.println("rand2: " + String(rand2));
    
}


  ///Error (E (114) spi_flash: Detected size(4096k) smaller than the size in the binary image header(16384k). Probe failed) occured bcoz
  ///the esp_flash API detected the incorrect flash size configured in the binary header.
  ///Setting a fixed ip Address for the Esp32 results into an error(server cant be accessed by a client).There4 use fixed ip address offered by Esp32 on creation of soft access point
  ///Fixed ip is http://192.168.4.1/

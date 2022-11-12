import 'dart:async';
import 'dart:convert';
import 'dart:math';
import 'package:flutter/material.dart';
import 'package:flutter_unity_widget/flutter_unity_widget.dart';
import 'package:web_socket_channel/io.dart';
import '../constants.dart';
import 'package:socket_io_client/socket_io_client.dart' as io;

class WithARkitScreen extends StatefulWidget {
  @override
  _WithARkitScreenState createState() => _WithARkitScreenState();
}

class CustomPopupMenu {
  CustomPopupMenu({this.title, this.scene});

  String title;
  int scene;
}

class _WithARkitScreenState extends State<WithARkitScreen> {
  ///Widget variables
  UnityWidgetController _unityWidgetController;
  bool acticatePlaneFinderScript = true;
  bool usingPlainWebsockets = true;
  // bool usingPlainWebsockets = false;
  bool showUnityWidget= true;

  ///Websocket Variables
  IOWebSocketChannel channel;
  bool webSocketClientIsConnected = false;
  ///Used by WebSockets
  int groundFloorButtonHasBeenTapped=0;
  int firstFloorButtonHasBeenTapped=0;
  int secondFloorButtonHasBeenTapped=0;
  int stopButtonHasBeenTapped=0;
  int saveNewWifi=0;
  String wiFiUSSDForESPToConnectTo = "";
  String wiFiPassword = "";
  int toggleToOnlineMode=0;
  int refreshCurrentStats = 0;

  void initWebSocketClient(){
    customLog("Connecting to WebSocket Server(ESP32)");
    channel = IOWebSocketChannel.connect(
        'ws://192.168.4.1:81'
      // Uri.parse('wss://192.168.4.3:2005/'),
    );
    channel.stream.listen((jsonString){/// handle data from the server
      // final parsed = jsonDecode(data).cast<Map<String, dynamic>>();
      // List<WebSocketServerData> receivedDataList =
      // parsed.map<WebSocketServerData>((json) =>
      //     WebSocketServerData.fromJson(json)).toList();
      // WebSocketServerData receivedData = receivedDataList.last;

      processElevatorInfoData(jsonString);
      updateConnectionStatus4PlainWebsocket(true);
    },
    onError: (error){
      customLog("Error occurred while listening to Websocket stream "+ error.toString());
      updateConnectionStatus4PlainWebsocket(false);
    },
    cancelOnError: true,
    onDone: (){
      customLog("WebSocketChannel is Done and therefore Closed");
      updateConnectionStatus4PlainWebsocket(false);
    }
    );
  }

  void updateConnectionStatus4PlainWebsocket(bool currentState){
    if(webSocketClientIsConnected != currentState && usingPlainWebsockets){
      webSocketClientIsConnected = currentState;
      setState(() {});
    }
  }

  void processElevatorInfoData(dynamic receivedData){
    if(animationControlMode  == AnimationControlMode.simulation) return;
    try{
      Map<String, dynamic> parsed;
      if(usingPlainWebsockets) parsed = jsonDecode(receivedData) as Map<String, dynamic>;
      else parsed = receivedData;
      WebSocketServerData rData = WebSocketServerData.fromJson(parsed);
      customLog("Received JSON string from esp32 " +rData.toString());
      setNewAnimation(rData.currentFloor,rData.desiredFloor,
          rData.currentFrameFraction,rData.direction,rData.middleToDesiredFloorFraction,
          rData.angularVelocity,rData.linearVelocity);
    }catch(e){
      customLog("ERROR WHILE UNPACKING JSON DATA: $e");
    }
  }

  void groundFloorButtonTapped(){
    groundFloorButtonHasBeenTapped =1;
    sendJSONDataToWebsocketServer();
  }
  void firstFloorButtonTapped(){
    firstFloorButtonHasBeenTapped=1;
    sendJSONDataToWebsocketServer();
  }
  void secondFloorButtonTapped(){
    secondFloorButtonHasBeenTapped=1;
    sendJSONDataToWebsocketServer();
  }
  void stopButtonTapped(){
    stopButtonHasBeenTapped=1;
    sendJSONDataToWebsocketServer();
  }
  void addNewWifiButtonTapped(){
    saveNewWifi=1;
    sendJSONDataToWebsocketServer();
  }
  void toggleOffOnModeButtonTapped(){
    toggleToOnlineMode= 1;
    sendJSONDataToWebsocketServer();
  }
  void requestCurrentStatsRefresh(){
    refreshCurrentStats = 1;
    sendJSONDataToWebsocketServer();
  }

  void sendJSONDataToWebsocketServer(){
    customLog('---Sending JSON DATA');
    if(usingPlainWebsockets){ //Offline mode
      if(!webSocketClientIsConnected){
        customLog('ERROR. WebSocket not connected. ');
        reset();
        return;
      }
    }else{ //Online mode
      if(!socket.connected){
        customLog('ERROR. SocketIO not connected. ');
        reset();
        return;
      }
    }

    var jsonData = {
      "groundFloorButtonHasBeenTapped": groundFloorButtonHasBeenTapped,
      "firstFloorButtonHasBeenTapped": firstFloorButtonHasBeenTapped,
      "secondFloorButtonHasBeenTapped": secondFloorButtonHasBeenTapped,
      "stopButtonHasBeenTapped": stopButtonHasBeenTapped,
      "saveNewWifi": saveNewWifi,
      "newWiFiUSSD": wiFiUSSDForESPToConnectTo,
      "wiFiPassword": wiFiPassword,
      "toggleToOnlineMode": toggleToOnlineMode,
      "refreshCurrentStats": refreshCurrentStats,
    };
    if(usingPlainWebsockets){
      try {
        String jsonString = json.encode(jsonData);
        channel.sink.add(jsonString);
        customLog('---Sent JSON DATA to WebSocket');
      } catch (error) {
        customLog('ERROR Sending JsonData to ESP32 Server: '+error);
      }
    }else{
      try {
        _ensureMainSocketConnected((){
          String jsonString = json.encode(jsonData);
          channel.sink.add(jsonString);
          socket.emit(clientInfoUpdateEvent, jsonString);
          customLog('---Sent JSON DATA to SocketIO');
        });
      } catch (error) {
        customLog('ERROR Sending JsonData to CloudServer: '+error);
      }
    }

    reset();
  }

  void reset() {
    groundFloorButtonHasBeenTapped=0;
    firstFloorButtonHasBeenTapped=0;
    secondFloorButtonHasBeenTapped=0;
    stopButtonHasBeenTapped=0;
    saveNewWifi = 0;
    wiFiUSSDForESPToConnectTo = "";
    wiFiPassword = "";
    toggleToOnlineMode=0;
    refreshCurrentStats = 0;
  }

  ///socket-io variables
  io.Socket socket;
  String acknowledgeDeviceRequestEvent= 'acknowledgeDeviceRequest';
  String acknowledgeDeviceResponseEvent= 'acknowledgeDeviceResponse';
  String clientInfoUpdateEvent= 'clientInfoUpdate';
  String elevatorInfoUpdateEvent = 'elevatorInfoUpdate';
  String webpageID = "WEBPAGE";

  void initSocketIO(){
    socket = io.io("https://elevator-iot-server.herokuapp.com/",
        io.OptionBuilder().setTransports(['websocket'])
            // .disableReconnection()
            .disableAutoConnect()
            .build()
    );
    _ensureMainSocketConnected((){});
  }

  void updateConnectionStatus4SocketIO(bool currentState){
    if(webSocketClientIsConnected != currentState && !usingPlainWebsockets){
      webSocketClientIsConnected = currentState;
      setState(() {});
    }
  }
  void _ensureMainSocketConnected(Function onConnected) {
    if (socket.connected) {
      onConnected();
      updateConnectionStatus4SocketIO(true);
      return;
    }
    customLog("Connecting to socketIO Server");
    socket
      ..on('connect', (dynamic _) {
        customLog('SocketIO Connected');
        updateConnectionStatus4SocketIO(true);
      })
      ..on('disconnect', (dynamic reason) {
        customLog('SocketIO Disconnected');
        updateConnectionStatus4SocketIO(false);

      })
      ..on(acknowledgeDeviceRequestEvent,(dynamic data){
        // customLog("Received acknowledgeDeviceRequestEvent : $data");
        socket.emit(acknowledgeDeviceResponseEvent, webpageID);
      })
      ..on(elevatorInfoUpdateEvent,(dynamic data){
        processElevatorInfoData(data);
      })
      ..connect();
    // socket.connect();
  }

  void disconnectSocketIO() {
    if (socket == null || !socket.connected) return;
    socket.disconnect();
  }

  @override
  void initState() {
    super.initState();
    if(usingPlainWebsockets) initWebSocketClient();
    else initSocketIO();
  }

  void dispose(){
    if(randomElevPropertiesTimer != null){
      if(randomElevPropertiesTimer.isActive) randomElevPropertiesTimer.cancel();
    }
    if(initElevPositionInDigitalTwinModeTimer != null){
      if(initElevPositionInDigitalTwinModeTimer.isActive)
        initElevPositionInDigitalTwinModeTimer.cancel();
    }
    disconnectSocketIO();
    channel.sink.close();
    super.dispose();
  }

  Future<void> toggleSocketsToBeUsed() async{
    if(usingPlainWebsockets){
      // await channel.sink.close(); //Takes a long time; probably an internal error with no error log
      //Anyway when toggled back to offline mode, stream is re-assigned to a new Websocket instance
      initSocketIO();
    }else{ //is currently using SocketIO
      disconnectSocketIO();
      initWebSocketClient();
    }
    setState(() {
      usingPlainWebsockets = !usingPlainWebsockets;
      webSocketClientIsConnected = false;
    });
    if(animationControlMode == AnimationControlMode.digitalTwin)
      requestCurrentStatsRefresh();
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Material(
        color: Colors.black,
        child: Stack(
          children: <Widget>[
            // if(showUnityWidget)
              UnityWidget(
              onUnityCreated: onUnityCreated,
              // onUnityViewCreated: onUnityCreated,
              // isARScene: false,
              onUnityMessage: onUnityMessage,
            ),
            Align(
              alignment: Alignment.topCenter,
              child: Padding(
                padding: EdgeInsets.only(top: 35.0),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceAround,
                      children: [
                        ElevatedButton(
                          child: Text(acticatePlaneFinderScript ? "Lock Position": "Update Position"),
                          onPressed: () {
                            togglePlaneFinderScriptActivity();
                            setState(() {
                              acticatePlaneFinderScript = !acticatePlaneFinderScript;
                            });
                          },
                        ),
                        ElevatedButton(
                          child: Text(usingPlainWebsockets? "Go online": "Go offline",
                            // style:
                            // TextStyle(color: webSocketClientIsConnected?
                            // Colors.green : Colors.black),
                          ),
                          // color: webSocketClientIsConnected? Colors.green: Colors.white,
                          onPressed: () async{
                            await toggleSocketsToBeUsed();
                          },
                        ),
                        ElevatedButton(
                          child: Text(animationControlMode == AnimationControlMode.simulation?
                          "Digital Twin Mode":"Simulate",
                            //   style:
                            // TextStyle(color: animationControlMode == AnimationControlMode.simulation?
                            // Colors.green: Colors.black),
                          ),
                          // style: ButtonStyle(col),
                          // color: animationControlMode == AnimationControlMode.simulation?
                          //   Colors.green : Colors.white,
                          onPressed: changeAnimationControlMode,
                        ),
                      ],
                    ),
                    Container(
                      height: 20.0,
                      width: double.infinity,
                      color: webSocketClientIsConnected?
                      Colors.green : Colors.redAccent,
                      alignment: Alignment.center,
                      child: Text(webSocketClientIsConnected? "Connected": "Disconnected",
                        style:
                        TextStyle(color: Colors.white),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  void setNewAnimation(int receivedCurrentFloor,int receivedDesiredFloor,
      double currentFrameFraction,direction,double receivedFloor0To1Fraction,
      double angularVelocity, double linearVelocity){

    String sep = "_";//separator
    String concatenated = "$receivedCurrentFloor"+"$sep"+"$receivedDesiredFloor"+"$sep"
        +"$currentFrameFraction"+"$sep"+"$direction"+"$sep"+"$receivedFloor0To1Fraction"
        +"$sep"+"$angularVelocity"+"$sep"+"$linearVelocity";

    if(_unityWidgetController == null){
      receivedElevProperties = concatenated;
      initElevPositionInDigitalTwinMode();
      return;
    }

    sendElevPropertiesToUnity(concatenated);
  }
  void sendElevPropertiesToUnity(String properties){
    _unityWidgetController?.postMessage(
      'elevator',
      'setCurrentAnimationFrameUsingFlutter',
      properties
    );
    // if(_unityWidgetController != null) customLog('''Sent Elev Properties to Unity:
    //   $properties
    // ''');
  }

  ///Elev properties are sent when unity widget hasnt even loaded. So these account 4 that
  String receivedElevProperties = "";
  Timer initElevPositionInDigitalTwinModeTimer;
  void initElevPositionInDigitalTwinMode(){
    if(initElevPositionInDigitalTwinModeTimer != null &&
        initElevPositionInDigitalTwinModeTimer.isActive) return;
    initElevPositionInDigitalTwinModeTimer = Timer.periodic(Duration(seconds: 1), (timer) {
      if(_unityWidgetController != null){
        sendElevPropertiesToUnity(receivedElevProperties);
        timer.cancel();
      }
    });
  }

  //todo: simulate ESP32 sending random Elevator Properties
  bool simulatingSendingRandomElevProperties = false;
  Timer randomElevPropertiesTimer;
  int currentFloor = 0;
  int desiredFloor = 0;
  String direction = "Stationary";
  double currentFraction =0.0;
  int maxCount = 4; ///seconds for simulating mvt
  void simulateSendingRandomElevProperties(int newFloor){
    if(currentFloor == newFloor) return;
    int currentCount = 0;
    bool mvtFinished = false;
    desiredFloor = newFloor;
    customLog('Simulating Elev properties; currentFloor: $currentFloor ; desiredFloor: $desiredFloor');
    randomElevPropertiesTimer = Timer.periodic(Duration(seconds: 1), (timer) {
      if(!mvtFinished) {
        currentCount +=1;
        if(currentFloor<desiredFloor) direction = "Up";
        else if(currentFloor>desiredFloor) direction = "Down";
        else direction = "Stationary";
        currentFraction = (currentCount/maxCount).toDouble();
        // currentFraction = 1.0;    mvtFinished = true;
        // customLog("Flutter current fraction is $currentFraction");
        setNewAnimation(currentFloor,desiredFloor,
            currentFraction,direction,0.5,0.0,0.0); //floor0To1Fraction is 0.5 and linear and angular velocity is 0
      }else{
        setNewAnimation(currentFloor,desiredFloor,
            currentFraction,direction,0.5,0.0,0.0); //floor0To1Fraction is 0.5
        timer.cancel();
        return;
      }

      if(currentCount >= maxCount || mvtFinished){
        mvtFinished = true;
        currentCount = 0;
        currentFloor = desiredFloor;
        direction = "Stationary";
      }

    });
  }

  void moveAnimationToGroundFloor() {
    if(animationControlMode == AnimationControlMode.simulation){
      _unityWidgetController?.postMessage(
        'elevator',
        'moveToGroundFloor',
        '',
      );
    }else {
      if(!simulatingSendingRandomElevProperties) groundFloorButtonTapped();
      else{
        if(randomElevPropertiesTimer == null || (randomElevPropertiesTimer != null && !randomElevPropertiesTimer.isActive))
          simulateSendingRandomElevProperties(0);
      }
    }
  }
  void moveAnimationToFirstFloor() {
    if(animationControlMode == AnimationControlMode.simulation){
      _unityWidgetController?.postMessage(
        'elevator',
        'moveToFirstFloor',
        '',
      );
    }else {
      if(!simulatingSendingRandomElevProperties) firstFloorButtonTapped();
      else{
        if(randomElevPropertiesTimer == null || (randomElevPropertiesTimer != null && !randomElevPropertiesTimer.isActive))
          simulateSendingRandomElevProperties(1);
      }
    }
  }
  void moveAnimationToSecondFloor() {
    // customLog("Moving to 2nd floor");
    if(animationControlMode == AnimationControlMode.simulation){
      _unityWidgetController?.postMessage(
        'elevator',
        'moveToSecondFloor',
        '',
      );
    }else {
      if(!simulatingSendingRandomElevProperties) secondFloorButtonTapped();
      else{
        if(randomElevPropertiesTimer == null || (randomElevPropertiesTimer != null && !randomElevPropertiesTimer.isActive))
          simulateSendingRandomElevProperties(2);
      }
    }
  }
  void stopAnimation() {
    if(animationControlMode == AnimationControlMode.simulation){
      _unityWidgetController?.postMessage(
        'elevator',
        'stopElevatorAnimation',
        '',
      );
    }else {
      if(!simulatingSendingRandomElevProperties) stopButtonTapped();
      else{
        if(randomElevPropertiesTimer != null && !randomElevPropertiesTimer.isActive){ randomElevPropertiesTimer.cancel(); }
      }
    }
  }
  void togglePlaneFinderScriptActivity() {
    _unityWidgetController?.postMessage(
      'elevator',
      'togglePlaneFinderScriptActivity',
      // 'toggleStatsViewVisibility',
      '',
    );
  }
  
  // AnimationControlMode animationControlMode  = AnimationControlMode.digitalTwin;
  AnimationControlMode animationControlMode  = AnimationControlMode.simulation;
  void changeAnimationControlMode() {
    if(animationControlMode == AnimationControlMode.digitalTwin) 
      animationControlMode  = AnimationControlMode.simulation;
    else {
      animationControlMode  = AnimationControlMode.digitalTwin; //requestCurrentStatsRefresh() is called by unity function
    }
    _unityWidgetController?.postMessage(
      'elevator',
      'changeAnimationControlMode',
      animationControlMode == AnimationControlMode.digitalTwin ? "1": "0",
    );
    setState(() {});
  }

  void onUnityMessage(dynamic message) async{
    String unityMessage = message.toString();
    customLog('Received message from unity: $unityMessage');
    if(unityMessage == "0") moveAnimationToGroundFloor();
    else if(unityMessage == "1") moveAnimationToFirstFloor();
    else if(unityMessage == "2") moveAnimationToSecondFloor();
    else if(unityMessage == "STOP") stopAnimation();
    else if(unityMessage == "request_elev_properties") requestCurrentStatsRefresh();
    return;
    // if(unityMessage== "0") groundFloorButtonTapped();
    // else if(unityMessage == "1") firstFloorButtonTapped();
    // else if(unityMessage == "2") secondFloorButtonTapped();
    // else if(unityMessage == "STOP") stopButtonTapped();
    // else if(unityMessage == "request_elev_properties") requestCurrentStatsRefresh();
  }

  // Callback that connects the created controller to the unity controller
  void onUnityCreated(controller) {
    this._unityWidgetController = controller;
    customLog("Unity Widget Loaded Successfully");
  }
}


void customLog(String log){
  print("CLOG: "+log);
}

class WebSocketServerData {
  final int currentFloor;
  final int desiredFloor;
  final double currentFrameFraction;
  final String direction;
  final String motion;
  final double angularVelocity;
  final double linearVelocity;
  final double middleToDesiredFloorFraction;
  final int cantConnectToNewWifi;

  const WebSocketServerData ({
    @required this.currentFloor,
    @required this.desiredFloor,
    @required this.currentFrameFraction,
    @required this.direction,
    @required this.motion,
    @required this.angularVelocity,
    @required this.linearVelocity,
    @required this.middleToDesiredFloorFraction,
    this.cantConnectToNewWifi,
  });

  factory WebSocketServerData.fromJson(Map<String, dynamic> json) {
    return WebSocketServerData (
      currentFloor: json['currentFloor'] as int,
      desiredFloor: json['desiredFloor'] as int,
      currentFrameFraction: findDoubleValueOfProperty(json, 'currentFrameFraction'),
      // currentFrameFraction: json['currentFrameFraction'] as double
      direction: json['direction'] as String,
      motion: json['motion'] as String,
      angularVelocity: findDoubleValueOfProperty(json, 'angularVelocity'),
      linearVelocity: findDoubleValueOfProperty(json, 'linearVelocity'),
      middleToDesiredFloorFraction: findDoubleValueOfProperty(json, 'middleToDesiredFloorFraction'),
      cantConnectToNewWifi: json['cantConnectToNewWifi'] as int,
    );
  }

  //sent doubles will be rounded to nearest int e.g 1.0 to 1; So convert them to type double
  static double findDoubleValueOfProperty(Map<String, dynamic> json, String requiredProperty){
    try{
      return json[requiredProperty] as double;
    }catch(e){
      int result = json[requiredProperty] as int;
      return result.toDouble();
    }
  }

  @override
  String toString() {
    return '''
      currentFloor : $currentFloor,
      desiredFloor : $desiredFloor,
      currentFrameFraction : $currentFrameFraction,
      direction : $direction,
      motion : $motion,
      angularVelocity : $angularVelocity,
      double linearVelocity : $linearVelocity,
      middleToDesiredFloorFraction : $middleToDesiredFloorFraction,
    ''';
  }
}

  enum AnimationControlMode { digitalTwin, simulation }

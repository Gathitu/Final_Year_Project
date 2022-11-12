var Socket;
var ground_Floor_status=0;
var first_Floor_status=0;
var second_Floor_status=0;
var start_status=0;
var stop_status=0;

function initClientSocket() {
    Socket = new WebSocket('ws://' + window.location.hostname + ':81/');
    Socket.onmessage = function(event) {
      processServerCommand(event);
    };
}

function processServerCommand(event) {
    var obj = JSON.parse(event.data);
    document.getElementById('rand1').innerHTML = obj.rand1;
    document.getElementById('rand2').innerHTML = obj.rand2;
  }

// function button_send_back() {
//     var msg = {
//       distance: 2,
//       angularVelocity: 36,
//       linearVelocity:  20,
//       floor: 2,
//       motion: 1,
//       direction: 1,
//     };
//     try {
//       Socket.send(JSON.stringify(msg));
//     } catch (error) {
      
//     }
//   }

 

/*var currentFloor_data=0;
var motion_data=0;
var direction_data=0;
var angularVelocity_data=0;
var linearVelocity_data=0;
connection.onmessage= function(event){
    var full_data=event.data;
    console.log(full_data);
    var data= JSON.parse(full_data);
    currentFloor_data = data.currentFloor;
    motion_data = data.motion;
    direction_data = data.direction;
    angularVelocity_data = data.angularVelocity;
    linearVelocity_data = data.linearVelocity;

    document.getElementById("currentFloor_value").innerHTML= currentFloor_data;
    document.getElementById("motion_value").innerHTML= motion_data;
    document.getElementById("direction_value").innerHTML= direction_data;
    document.getElementById("angularVelocity_meter").value=angularVelocity_data;
    document.getElementById("angularVelocity_value").innerHTML=angularVelocity_data ;
    document.getElementById("linearVelocity_meter").value= linearVelocity_data;
    document.getElementById("linearVelocity_value").innerHTML= linearVelocity_data ;
    
}*/
function ground_Floor()
{
    ground_Floor_status =1;
    console.log("Ground");
    send_data();
}

function first_Floor()
{
    first_Floor_status=1;
    console.log("Floor 1");
    send_data();
}

function second_Floor(){
    second_Floor_status=1;
    console.log("Floor 2");
    send_data();
}

function start(){
    start_status=1;
    console.log("Start");
    send_data();
}

function stop(){
    stop_status=1;
    console.log("Stop");
    send_data();
}

function send_data(){
    var full_data= '{"currentFloor":'+ground_Floor_status+','+first_Floor_status+','+second_Floor_status+','+start_status+','+stop_status+'}';
    console.log(full_data);
    try {
        Socket.send(full_data);
    } catch (error) {}
    reset();
}

function reset() {
    ground_Floor_status=0;
    first_Floor_status=0;
    second_Floor_status=0;
    start_status=0;
    stop_status=0;
}
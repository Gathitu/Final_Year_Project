using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class elevatorFloorController : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] AnimationClip elevatorMvtAnimation;
    [SerializeField] UnityMessageManager unityMessageManager;
    // private UnityMessageManager unityMessageManager;
    [SerializeField] GameObject statsView;
    [SerializeField] GameObject planeFinderScript;
    [SerializeField] Text currentFloorTextView,directionTextView,angularVelocityTextView,linearVelocityTextView;

    const string animBaseLayer = "Base Layer";
    const string elevatorMvtAnimationName = "full_animation";
    // const string elevatorReverseMvtAnimationName = "full_animation_reverse";
    const string elevatorMvtAnimationHashName = animBaseLayer+"."+elevatorMvtAnimationName;
    // const string elevatorReverseMvtAnimationHashName = animBaseLayer+"."+elevatorReverseMvtAnimationName;
    int elevatorMvtAnimationHash = Animator.StringToHash(elevatorMvtAnimationHashName);
    // int elevatorReverseMvtAnimationHash = Animator.StringToHash(elevatorReverseMvtAnimationHashName);
    Dictionary<int, AnimationClip> hashToClip = new Dictionary<int, AnimationClip>();

    const int totalAnimationFrames = 130;
    const int Floor0UpFrame = 0;
    const int Floor1UpFrame = 24;
    const int Floor2Frame = 64;
    const int Floor1DownFrame = 106;
    const int Floor0DownFrame = totalAnimationFrames;
    // float elevatorCurrentFrameInFraction = 0.0f;
    float elevatorCurrentFrameInFraction = (float) Floor0UpFrame;
    float floor0UpFrameInFraction;
    float floor0DownFrameInFraction;
    float floor1UpFrameInFraction;
    float floor1DownFrameInFraction;
    float floor2FrameInFraction;
    int currentFloor = 0;
    int desiredFloor = 0;
    DirectionOfMotion directionOfMotion = DirectionOfMotion.up;//1=up  while -1=down 
    bool elevatorIsAtASpecificFloor = true;
    bool elevatorMotionIsInReverse = false;
    int noOfCompletedAnimationLoop=0; //no of loops that have been completed
    bool enteredAnotherAnimationLoop = false;
    bool controllingElevPosInSimModeUsingFlutter = false; //set to true if you want to using move elevator in simulation mode using flutter
    ///DIGITAL TWIN CONTROL
    float previouslyReceievedFrameFraction = 0.0f;//esp32 will send a value of 0 if at groundfloor
    bool objectHasBeenPlaced = false;
    string receivedAnimationDirection = "Stationary";
    // AnimationControlMode animationControlMode  = AnimationControlMode.digitalTwin;
    AnimationControlMode animationControlMode  = AnimationControlMode.simulation;
    bool animationIsPlaying = false;
    string receivedElevatorPositionInfo = ""; //Is DEPRECATED. Was used for setting elev position when content has been placed using info received by esp32-flutter 
    String sep = "_";//received elevator properties separator; just wanted the variable name to be short
    DateTime animationStartTime= DateTime.UtcNow;
    float animationStartFrame = 0.0f;
    float digitalTwinMvtAnimationSteps = 0.0f; //Is DEPRECATED.

    void Awake()
    {
        hashToClip.Add(elevatorMvtAnimationHash, elevatorMvtAnimation);
        // hashToClip.Add(elevatorReverseMvtAnimationHash, elevatorMvtAnimation);
        pauseElevatorAnimation();  //animation starts automatically when game awakes
    }

    void Start(){
        // unityMessageManager = GetComponent<UnityMessageManager>();
        floor0UpFrameInFraction = (float)Floor0UpFrame/(float)totalAnimationFrames;
        floor0DownFrameInFraction = (float)Floor0DownFrame/(float)totalAnimationFrames;
        floor1UpFrameInFraction = (float)Floor1UpFrame/(float)totalAnimationFrames;
        floor1DownFrameInFraction = (float)Floor1DownFrame/(float)totalAnimationFrames;
        floor2FrameInFraction = (float)Floor2Frame/(float)totalAnimationFrames;
        playAnimationAtASpecificTime(elevatorMvtAnimationHashName,0, floor0UpFrameInFraction,false);
        // toggleStatsViewVisibility();

            // String concatenated = "1"+sep+"1"+sep
            //     +"1"+sep+"Stationary"+sep+"0.4"
            //     +sep+"0.0"+sep+"0.0";
            // float receivedResult = getCurrentFrameFraction(concatenated);//Fraction of floor0 to floor2 that physical model is in
            // customLog("Received Current Frame Fraction: " + receivedResult.ToString());

        // float a = 0.00002f;
        // float b = 0f;
        // float x = getCurrentAnimatorNormalizedTime(animator);
        // float y = fractionOf(Floor1UpFrame);
        // if(x>=a) Debug.Log("ERROR 1");
        // if(x >=y ) Debug.Log("ERROR 2");

        // float c = Mathf.Max(a,x);
        // if(c==b) Debug.Log("ERROR 3");
        // float d = Mathf.Max(x,y);
        // if(d==x) Debug.Log("ERROR 4");

    }


    AnimationClip getClipFromHash(int hash){
        AnimationClip clip;
        if(hashToClip.TryGetValue(hash, out clip))
            return clip;
        else
            return null; ///Is an empty animator object i.e Animator.final_State; not an animation
    }

    ///Returns a float between 0.0 and 1.0
    float getCurrentAnimatorNormalizedTime(Animator anim, int layer=0,bool logTime = false){
        AnimatorStateInfo animState = anim.GetCurrentAnimatorStateInfo(layer);
        int currentAnimHash = animState.fullPathHash; //Get the current animation hash
        AnimationClip animClip = getClipFromHash(currentAnimHash); //Convert the animation hash to animation clip
        // if(animClip == null) {elevatorAnimationDone();   return 0;} ///This was important when elev animation went to an empty animation object/didnt loop
        if(animClip == null) return 0.0f; ///This was important when elev animation went to an empty animation object/didnt loop

        if(logTime) {
            // customLog("Animation total time is "+animClip.length.ToString());
            // float time = animState.normalizedTime * 1000.0f;
            float time = animState.normalizedTime;
            customLog("Current Normalized time is "+time.ToString());
        }
        // float currentTime = animClip.length * animState.normalizedTime;
        float normalizedTime = Mathf.Abs(animState.normalizedTime); ///value cld be negative
        int normalizedTimeInIntegers = (int) normalizedTime;
        bool enteredReverseAnimationLoop = animState.normalizedTime < 0.0f;  ///where animState.normalizedTime is negative
        if(noOfCompletedAnimationLoop != normalizedTimeInIntegers || enteredReverseAnimationLoop) {
            ///TODO: Find a different way coz if this function runs in update function,it will be caught in a non-ending loop,speed is changed,normalized time changes backwards,speed is then changed again
            //But for current appllication,it works
            elevatorAnimationDone(normalizedTimeInIntegers); 
            enteredAnotherAnimationLoop = true;
        }
        float nTime = normalizedTime>=1.0f ? (float)normalizedTime%(float) normalizedTimeInIntegers : normalizedTime;
        // customLog("Current Normalized time is "+nTime.ToString());
        elevatorCurrentFrameInFraction = nTime;
        return nTime;
    }

    bool firstCheckCompleted  = false;
    bool secondCheckCompleted  = false;
    void testElevMovement(){ //If doesnt work, uncomment pauseElevatorAnimation(); in awake() function
        if(!firstCheckCompleted){
            firstCheckCompleted = true;
            animationIsPlaying = true; //set on init
        }

        if(!animationIsPlaying) return;
        float animationNormalizedTime = getCurrentAnimatorNormalizedTime(animator,0,true); //is in decimal form
        float tsResult = 0.0f;

        // if(animationNormalizedTime >= floor2FrameInFraction && !secondCheckCompleted ) {
        //     secondCheckCompleted  = true;
        //     // animationStartFrame = animationNormalizedTime;
        //     playReverseAnimationInDigitalTwinMode();
        // }
        if(secondCheckCompleted){
            if(animationNormalizedTime <= floor0UpFrameInFraction || enteredAnotherAnimationLoop) pauseElevatorAnimationInDigitalTwinMode();
            tsResult = animationStartFrame - (float)(timeSpan()/10000.0f);
        }
        // else tsResult = animationStartFrame + (float)(timeSpan()/10000.0f);
        ///Used v=u+at;     in init, u(animationStartFrame)=0;    Tuned according to frame brought about by automatic motion in unity to get a  
        else tsResult = animationStartFrame + (float)(0.16047712994* (float)(timeSpan()/1000.0f) ); //v=u+at
        customLog("ts: "+ tsResult.ToString());
        // float t = (float)(timeSpan()/1000.0f);
        // customLog("t: "+ t.ToString());
    }

    void Update()
    {
        // testElevMovement();
        digitalTwinMovement();
        setPositionAfterDeafModeTimer();
        
        //Bugs when using code meant for simulation mode, so using digitalTwinMode code instead
        // if(animationControlMode == AnimationControlMode.digitalTwin) digitalTwinMovement();
        // else pauseElevatorAnimationOnLimitReached();
    }

    void pauseElevatorAnimationOnLimitReached(){
        if(!elevatorIsAtASpecificFloor){
            float animationNormalizedTime = getCurrentAnimatorNormalizedTime(animator); //is in decimal form
            if(!elevatorMotionIsInReverse){
                if(desiredFloor == 1){
                    if(directionOfMotion==DirectionOfMotion.up){ ///from ground floor
                        if(animationNormalizedTime >= floor1UpFrameInFraction) pauseElevatorAnimation();
                    }else{///from 2nd floor
                        if(animationNormalizedTime >= floor1DownFrameInFraction) pauseElevatorAnimation();
                    }
                }
                else if(desiredFloor == 2){
                    if(animationNormalizedTime >= floor2FrameInFraction) pauseElevatorAnimation();
                }
                else if(desiredFloor == 0){
                    if(enteredAnotherAnimationLoop || animationNormalizedTime >= floor0DownFrameInFraction) { //animationNormalizedTime is always absolute so enteredAnotherAnimationLoop is used
                        pauseElevatorAnimation();
                        playAnimationAtASpecificTime(elevatorMvtAnimationHashName,0, Floor0DownFrame,false);
                        directionOfMotion=DirectionOfMotion.up; 
                        elevatorMotionIsInReverse = true;
                    }
                }
            }else{
                if(desiredFloor == 1){
                    if(directionOfMotion==DirectionOfMotion.up){ ///from ground floor
                        if(animationNormalizedTime <= floor1DownFrameInFraction) pauseElevatorAnimation();
                    }else{///from 2nd floor
                        if(animationNormalizedTime <= floor1UpFrameInFraction) pauseElevatorAnimation();
                    }
                }
                else if(desiredFloor == 2){
                    if(animationNormalizedTime <= floor2FrameInFraction) pauseElevatorAnimation();
                }
                else if(desiredFloor == 0){
                    if(enteredAnotherAnimationLoop || animationNormalizedTime <= floor0UpFrameInFraction){  //animationNormalizedTime is always absolute so enteredAnotherAnimationLoop is used
                        pauseElevatorAnimation();
                        playAnimationAtASpecificTime(elevatorMvtAnimationHashName,0, Floor0UpFrame,false);
                        directionOfMotion=DirectionOfMotion.up;
                        elevatorMotionIsInReverse = false;
                    }
                }

            }
 
        }

    }

    public void changeAnimationControlMode(string newMode){
        if(newMode== "0" && animationControlMode == AnimationControlMode.simulation) return;
        else if(newMode== "1" && animationControlMode == AnimationControlMode.digitalTwin) return;
        if(newMode== "1") {
            animationControlMode = AnimationControlMode.digitalTwin;
            customLog("Now in Digital Twin Mode");
            ///CurrentElevatorPosition will be requested from ESP32 by flutter
        }
        else {
            animationControlMode =AnimationControlMode.simulation;
            customLog("Now in Simulation Mode");
        }
        resetAnimationSpeed();
        pauseElevatorAnimationInDigitalTwinMode();
        elevatorCurrentFrameInFraction = floor0UpFrameInFraction;///the frame cld be past floor2
        playAnimationAtASpecificTime(elevatorMvtAnimationHashName,0, elevatorCurrentFrameInFraction,false);
        previouslyReceievedFrameFraction = 0.0f;
        directionOfMotion=DirectionOfMotion.up;
        receivedAnimationDirection = "Stationary";
        if(animationControlMode==AnimationControlMode.digitalTwin) sendMessageToFlutter("request_elev_properties");
    }

    public void onContentPlaced(){
        objectHasBeenPlaced = true;
        customLog("Object has been placed");
        if(animationControlMode==AnimationControlMode.digitalTwin) {
            sendMessageToFlutter("request_elev_properties");
            // setCurrentAnimationFrame(receivedElevatorPositionInfo);
        }
    }
    // OnTargetFound not really needed
    ///when camera is turned to another direction,Target is lost but when camera is returned to where content had been placed, onContentPlaced() is not called
    public void onTargetLost(){
        // objectHasBeenPlaced = false; //due to reason above
        customLog("Target lost");
    }

    public void toggleStatsViewVisibility(){
        statsView.SetActive(!statsView.activeSelf);
        customLog("Stats View Visibility Toggled");
    }

    public void togglePlaneFinderScriptActivity(){
        planeFinderScript.SetActive(!planeFinderScript.activeSelf);
        customLog("Plane-Finder Script Activity Toggled");
    }

    ///-----------------------------DIGITAL TWIN CONTROL-----------------------------------------------------------------------------------------------------
    void updateStatsView(string receivedInfo){
        string[] splitArray = receivedInfo.Split(char.Parse(sep));
        if(splitArray.Length != 7) {
            customLog("Split array length is not 7. Contents:");
            for(int i=0; i<splitArray.Length; i++) customLog(splitArray[i]);
            return;
        }
        currentFloorTextView.text = splitArray[0];
        directionTextView.text = splitArray[3];
        angularVelocityTextView.text = splitArray[5] + " RPM";
        linearVelocityTextView.text = splitArray[6] + " m/s";
    }  
    float getCurrentFrameFraction(string receivedInfo){
        try{
            int receivedCurrentFloor, receivedDesiredFloor;
            float currentFrameFraction, middleToDesiredFloorFraction;
            string direction;
            string[] splitArray = receivedInfo.Split(char.Parse(sep));
            if(splitArray.Length != 7) {
                customLog("Split array length is not 7. Contents:");
                for(int i=0; i<splitArray.Length; i++) customLog(splitArray[i]);
                return previouslyReceievedFrameFraction;
            }

            receivedCurrentFloor = int.Parse(splitArray[0]);
            receivedDesiredFloor = int.Parse(splitArray[1]);
            currentFrameFraction = float.Parse(splitArray[2]);//it is a fraction (current mvt/total mvt) of the physical model motion
            direction = splitArray[3]; 
            receivedAnimationDirection = direction;
            middleToDesiredFloorFraction= float.Parse(splitArray[4]); //IF SPLIT ARRAY IS UPDATED, CHANGE REQUIRED LENGTH IN ABOVE IF FUNCTION
            //Note that receivedCurrentFrameFraction is a fraction of time taken from one floor to another
            customLog("Received Current floor is " + receivedCurrentFloor.ToString() + " " +
             "Received Desired floor is " + receivedDesiredFloor.ToString() + " " +
             "Received Direction is " + direction.ToString()
             );
            
            //update textViews in StatsView
            currentFloorTextView.text = splitArray[0];
            directionTextView.text = splitArray[3];
            angularVelocityTextView.text = splitArray[5] + " RPM";
            linearVelocityTextView.text = splitArray[6] + " m/s";//IF SPLIT ARRAY IS UPDATED, CHANGE REQUIRED LENGTH IN ABOVE IF FUNCTION
            
            float receivedResult = 0.0f;///cannot be null
            
            float floor1DownFraction = (float)(Floor1UpFrame-Floor0UpFrame)/(float)Floor2Frame;
            // float floor2DownFraction = (float)(Floor2Frame-Floor0UpFrame)/(float)Floor2Frame;  = 1.0
            float floor1UpFraction = (float)(Floor2Frame-Floor1UpFrame)/(float)Floor2Frame;
            if(receivedCurrentFloor==0)  {
                receivedResult = (float)Floor0UpFrame/(float)Floor2Frame;
                if(receivedDesiredFloor == 0){
                    if(direction == "Up") { customLog("ERROR: Direction cannot be upwards"); return receivedResult;}
                    else if(direction == "Down") receivedResult += ((float)((1.0-currentFrameFraction)*floor1DownFraction));//get remaining time to return back to bottom
                    ///else if Stationary, no change
                } 
                else if(receivedDesiredFloor == 1){
                    if(direction == "Down") { customLog("ERROR: Direction cannot be downwards"); return receivedResult;}
                    else if(direction == "Up") receivedResult += ((float)(currentFrameFraction*floor1DownFraction));///relative position physical model is 
                    ///else if Stationary, no change
                }
                else if(receivedDesiredFloor == 2){
                    if(direction == "Down") { customLog("ERROR: Direction cannot be downwards"); return receivedResult;}
                    else if(direction == "Up") {
                        ///If receivedCurrentFrameFraction == 0.49 and receivedFloor0To1Fraction==0.5, fraction should be floor0Framefraction + (0.49/0.5)*floor1DownFraction
                        float receivedFloor0To1Fraction = middleToDesiredFloorFraction;
                        float actualFractionFromFloor0To2 = receivedFloor0To1Fraction;
                        customLog("From floor2 to 0, direction is up, currentFrameFraction: " + currentFrameFraction.ToString() + ",,receivedFloor0To1Fraction:"+ receivedFloor0To1Fraction.ToString());
                        if(currentFrameFraction<=receivedFloor0To1Fraction){
                            actualFractionFromFloor0To2= (float)currentFrameFraction/receivedFloor0To1Fraction;//actual fraction 
                            receivedResult += (float)(actualFractionFromFloor0To2*floor1DownFraction);///relative position physical model is 
                            customLog("Case1: ActualFractionFromFloor0To2 : " + actualFractionFromFloor0To2.ToString());
                        }
                        else if(currentFrameFraction>receivedFloor0To1Fraction){
                            actualFractionFromFloor0To2= (float)(currentFrameFraction - receivedFloor0To1Fraction)/(float)(1.0-receivedFloor0To1Fraction);
                            receivedResult += (((float)Floor1UpFrame/(float)Floor2Frame) +(float)(actualFractionFromFloor0To2*floor1UpFraction));///relative position physical model is 
                            customLog("Case2: ActualFractionFromFloor0To2 : " + actualFractionFromFloor0To2.ToString());
                        }
                    }
                    ///else if Stationary, no change
                }
            }
            else if(receivedCurrentFloor==1)  {
                receivedResult = (float)Floor1UpFrame/(float)Floor2Frame;
                if(receivedDesiredFloor == 0){
                    if(direction == "Up") { customLog("ERROR: Direction cannot be upwards"); return receivedResult;}
                    else if(direction == "Down")receivedResult -= ((float)(currentFrameFraction*floor1DownFraction));///relative position physical model is 
                }
                else if(receivedDesiredFloor == 1){
                    if(direction == "Up") receivedResult -= ((float)((1.0-currentFrameFraction)*floor1DownFraction));//get remaining time to return back to floor1
                    else if(direction == "Down")receivedResult += ((float)((1.0-currentFrameFraction)*floor1UpFraction));//get remaining time to return back to floor1
                }
                else if(receivedDesiredFloor == 2){
                    if(direction == "Down") { customLog("ERROR: Direction cannot be downwards"); return receivedResult;}
                    else if(direction == "Up") receivedResult += ((float)(currentFrameFraction*floor1UpFraction));///relative position physical model is 
                }
            }
            else if(receivedCurrentFloor==2) {
                receivedResult = 1.0f;
                if(receivedDesiredFloor == 2){
                    if(direction == "Down") { customLog("ERROR: Direction cannot be downwards"); return receivedResult;}
                    else if(direction == "Up") receivedResult -= ((float)((1.0-currentFrameFraction)*floor1UpFraction));//get remaining time to return back to top
                } 
                else if(receivedDesiredFloor == 1){
                    if(direction == "Up") { customLog("ERROR: Direction cannot be upwards"); return receivedResult;}
                    else if(direction == "Down") receivedResult -= ((float)(currentFrameFraction*floor1UpFraction));///relative position physical model is 
                }
                else if(receivedDesiredFloor == 0){
                    if(direction == "Up") { customLog("ERROR: Direction cannot be upwards"); return receivedResult;}
                    else if(direction == "Down") {
                        float receivedFloor2To1Fraction = middleToDesiredFloorFraction;
                        float actualFractionFromFloor2To0 =  receivedFloor2To1Fraction;
                        ///If receivedCurrentFrameFraction == 0.49 and receivedFloor2To1Fraction==0.5, fraction should be floor2Framefraction - (0.49/0.5)*floor1UpFraction
                        customLog("From floor0 to 2, direction is down, currentFrameFraction : " + currentFrameFraction.ToString() + ",, receivedFloor2To1Fraction:"+  receivedFloor2To1Fraction.ToString());
                        if(currentFrameFraction<= receivedFloor2To1Fraction){
                            actualFractionFromFloor2To0= (float)currentFrameFraction/(float) receivedFloor2To1Fraction;//actual fraction 
                            receivedResult -= (float)(actualFractionFromFloor2To0*floor1UpFraction);///relative position physical model is 
                            customLog("Case1: ActualFractionFromFloor2To0 : " + actualFractionFromFloor2To0.ToString());
                        }
                        else if(currentFrameFraction> receivedFloor2To1Fraction){
                            actualFractionFromFloor2To0= (float)(currentFrameFraction -  receivedFloor2To1Fraction)/(float)(1.0- receivedFloor2To1Fraction);
                            receivedResult -= ((float)(1.0-((float)Floor1UpFrame/(float)Floor2Frame)) + (float)(actualFractionFromFloor2To0*floor1DownFraction));///relative position physical model is 
                            customLog("Case2: ActualFractionFromFloor2To0 : " + actualFractionFromFloor2To0.ToString());
                        }
                    }
                }
            }
            return receivedResult;

        }catch(Exception e){
            customLog("Error parsing received concatenated string. "+e);
            return previouslyReceievedFrameFraction;
        } 
    }

    float totalFramesToBeWorkedWith(){
        return (float)(floor2FrameInFraction-floor0UpFrameInFraction); //In this case, we are working with the first part of the animation
    } 
    public void setCurrentAnimationFrameUsingFlutter(string receivedInfo){
        setCurrentAnimationFrame(receivedInfo);
    }
    ///Used both in Digital Twin Control and Simulation Mode 
    void setCurrentAnimationFrame(string receivedInfo,bool isSimulationShortcut = false, float shortcutFrameFraction = 0.0f) {
        if(animationControlMode == AnimationControlMode.digitalTwin && receivedInfo == "") return;
        try{
            if(!objectHasBeenPlaced){
                // receivedElevatorPositionInfo  = receivedInfo;
                customLog("Received Elev properties but object has not yet been placed");
                return;
            }
            if(animationIsPlaying) {
                if(animationControlMode == AnimationControlMode.digitalTwin){
                    customLog("Received Elev properties but Animation is playing");
                    // updateStatsView(receivedInfo);
                    // return;  //dont return here as if elev has reached destination floor, we need to acknowledge that in system
                }else{
                    if(!isSimulationShortcut) return;
                }
            }
            float receivedFrameFraction = 0.0f;
            float transformedFrameFraction = 0.0f;
            if(animationControlMode == AnimationControlMode.digitalTwin) {
                receivedFrameFraction = getCurrentFrameFraction(receivedInfo);//Fraction of floor0 to floor2 that physical model is in
                if(animationIsPlaying){
                     if(receivedAnimationDirection == "Stationary" && setPosAfterDeafModeTimerCallback ==null){
                        setPosAfterDeafModeTimerCallback = () => setCurrentAnimationFrame(receivedInfo,isSimulationShortcut,shortcutFrameFraction);
                        customLog("Waiting until animation stops playing to set Desired floor");
                    }else return;
                }
                // transformedFrameFraction = (float)floor0UpFrameInFraction + (float) (receivedFrameFraction* totalFramesToBeWorkedWith()); //Makes no sense
                transformedFrameFraction = (float) (receivedFrameFraction* totalFramesToBeWorkedWith()); //convert to unity animation fraction e.g floor2 = diff *1.0
            }
            else  {
                transformedFrameFraction = shortcutFrameFraction;
                receivedFrameFraction = shortcutFrameFraction;//Assignment not necessary though
            }
            // customLog("Received Current Frame Fraction: " + transformedFrameFraction.ToString());

            if(previouslyReceievedFrameFraction != transformedFrameFraction){
                previouslyReceievedFrameFraction = transformedFrameFraction;
                customLog("Received Current Frame Fraction: " + receivedFrameFraction.ToString() + " " +
                    "After matching to Unity Animation, current animation fraction is " + transformedFrameFraction.ToString() + " " +
                    "While Program Current Frame Fraction is " + elevatorCurrentFrameInFraction.ToString()
                ); //todo: to help solve why elev doesnt move when from floor2 to 0 is called 
                ///if stop button is received, the last currentframe is the one that will be stopped at 
                ///First check if elev was previously moving in different direction as the required one and change direction 
                ///In DigitalTwinControl, elevatorMotionIsInReverse means going from floor2 back to floor1 or 0
                if((elevatorMotionIsInReverse && (transformedFrameFraction > elevatorCurrentFrameInFraction)) ||  ///was going down, now going up
                    (!elevatorMotionIsInReverse && (transformedFrameFraction < elevatorCurrentFrameInFraction))) ///was going up, now going down
                    playReverseAnimationInDigitalTwinMode();
                else{///continue with previous motion if received frame is different from the previously received frame
                    bool startMotion = false;
                    if(receivedAnimationDirection == "Up" && (transformedFrameFraction > elevatorCurrentFrameInFraction)) startMotion = true;
                    else if(receivedAnimationDirection == "Down" && (elevatorCurrentFrameInFraction > transformedFrameFraction)) startMotion = true;  
                    else if(receivedAnimationDirection == "Stationary"){ ///e.g if receiving elev properties on init
                        if((elevatorMotionIsInReverse && (transformedFrameFraction < elevatorCurrentFrameInFraction)) ||  ///was going down, now going up
                            (!elevatorMotionIsInReverse && (transformedFrameFraction > elevatorCurrentFrameInFraction))) startMotion = true;
                    }
                    if(startMotion) playAnimationInDigitalTwinMode();
                }
                elevatorMotionIsInReverse = elevatorCurrentFrameInFraction > transformedFrameFraction;
            }
        }catch(Exception e){
            customLog("Error on receiving current animaton frame. "+e);
        }
    }

    ///Allow automatic animation play but pause when desired position has been reached;    Downside is unity player sometimes jump frames which makes result buggy 
    // void digitalTwinMovement(){
    //     if(!animationIsPlaying) return;
    //     float animationNormalizedTime = getCurrentAnimatorNormalizedTime(animator); //is in decimal form
    //     if(!elevatorMotionIsInReverse){//going up
    //         if(animationNormalizedTime >= previouslyReceievedFrameFraction) pauseElevatorAnimationInDigitalTwinMode();
    //     }
    //     else{//going down
    //         if(animationNormalizedTime <= previouslyReceievedFrameFraction) pauseElevatorAnimationInDigitalTwinMode();
    //         else if(enteredAnotherAnimationLoop){  
    //             pauseElevatorAnimationInDigitalTwinMode();
    //             playAnimationAtASpecificTime(elevatorMvtAnimationHashName,0, Floor0UpFrame,false);
    //             directionOfMotion=DirectionOfMotion.up;
    //             elevatorMotionIsInReverse = false;
    //         }
    //     }
    // }
    
    ///Set current frame manually;
    void digitalTwinMovement(){ 
        if(!animationIsPlaying) return;
        float animationNormalizedTime = getCurrentAnimatorNormalizedTime(animator); //is in decimal form
        if(!elevatorMotionIsInReverse){//going up
            if(animationNormalizedTime >= previouslyReceievedFrameFraction) pauseElevatorAnimationInDigitalTwinMode();
            else {
                setCurrentFrame(false);
            }
        }
        else{//going down
            if(animationNormalizedTime <= previouslyReceievedFrameFraction) pauseElevatorAnimationInDigitalTwinMode();
            else if(enteredAnotherAnimationLoop){  
                pauseElevatorAnimationInDigitalTwinMode();
                playAnimationAtASpecificTime(elevatorMvtAnimationHashName,0, Floor0UpFrame,false);
                directionOfMotion=DirectionOfMotion.up;
                elevatorMotionIsInReverse = false;
            }
            else{
                setCurrentFrame(true);
            }
        }
    }

    void setCurrentFrame(bool isMvtInReverse){
        ///Used v=u+at;     in init, u(animationStartFrame)=0;    Tuned according to frame brought about by automatic motion in unity to get a  
        float newFrame = 0.0f;
        // float at = (float)(0.16047712994* (float)(timeSpan()/1000.0f) );
        float at = (float)(0.30047712994* (float)(timeSpan()/1000.0f) );
        if(!isMvtInReverse) newFrame = animationStartFrame + at;
        else newFrame = animationStartFrame - at;
        // float newFrame = (float)(animationNormalizedTime - digitalTwinMvtAnimationSteps); /using constant animation steps
        customLog("New frame: " +newFrame.ToString());
        float t = (float)(timeSpan()/1000.0f);
        customLog("t: "+ t.ToString() +"     "+"at: "+ at.ToString());
        playAnimationAtASpecificTime(elevatorMvtAnimationHashName,0, newFrame,false);
    }

    float timeSpan(){///difference bwtween now and when elev started moving
        DateTime now = DateTime.UtcNow;
        TimeSpan ts = now.Subtract(animationStartTime);
        double tsInMs = ts.TotalMilliseconds;
        return (float) tsInMs;
    }
    ///----------------------------------------------------------------------------------------------------------------------------------


    float fractionOf(int lastFrame){
        float result = (float)lastFrame/(float)totalAnimationFrames;
        customLog("Limit Normalized time is "+result.ToString());
        // customLog("Current Calculated Fraction time is "+result.ToString());
        return result;
    }


    bool isAtSpecifiedFloor(int floor){
        ///maximum frame that elevator cld have stopped at when made to stop at first floor(wont stop at exactly e.g floor1DownFrameInFraction )
        float midFloorFraction0_1 = (floor1UpFrameInFraction-floor0UpFrameInFraction)/2;
        float midFloorFraction1_2 = floor1UpFrameInFraction + ((floor2FrameInFraction-floor1UpFrameInFraction)/2);
        float midFloorFraction2_1 = floor2FrameInFraction+((floor1DownFrameInFraction-floor2FrameInFraction)/2);
        float midFloorFraction1_0 = floor1DownFrameInFraction + ((floor0DownFrameInFraction-floor1DownFrameInFraction)/2);

        if(floor==0) return elevatorIsAtASpecificFloor && 
            (elevatorCurrentFrameInFraction < midFloorFraction0_1 || elevatorCurrentFrameInFraction > midFloorFraction1_0);
        else if(floor==1) {
            ///For better accuracy, more code is needed but for now, this works
            return elevatorIsAtASpecificFloor && 
            (elevatorCurrentFrameInFraction>= midFloorFraction0_1 && elevatorCurrentFrameInFraction<= midFloorFraction1_2) ||
            (elevatorCurrentFrameInFraction>= midFloorFraction2_1 && elevatorCurrentFrameInFraction<= midFloorFraction1_0);
        }else{//floor==2
            return elevatorIsAtASpecificFloor && 
            (elevatorCurrentFrameInFraction > midFloorFraction1_2 && elevatorCurrentFrameInFraction < midFloorFraction2_1);
        }
    }

    public void moveToFirstFloor(){
        setCurrentAnimationFrame("",true,floor1UpFrameInFraction);
        return;
        //Bugs when using code meant for simulation mode, so using digitalTwinMode code instead
        
        if(isAtSpecifiedFloor(1)) {
            customLog("CANNOT MOVE TO FIRST FLOOR. 1:"+elevatorCurrentFrameInFraction.ToString()+" 2:"+floor1DownFrameInFraction.ToString()
                +" 2:"+floor1UpFrameInFraction.ToString() );
            pauseElevatorAnimation();
            return;
        }

        if(elevatorCurrentFrameInFraction<floor2FrameInFraction){
            if(elevatorCurrentFrameInFraction<floor1UpFrameInFraction){
                if(directionOfMotion==DirectionOfMotion.up){
                    playAnimation();
                }else{
                    playAnimationInReverse(false); ///return animation to default direction
                }
                directionOfMotion = DirectionOfMotion.up;
            }
            else if(elevatorCurrentFrameInFraction>floor1UpFrameInFraction){
                if(directionOfMotion==DirectionOfMotion.up){
                    playAnimationInReverse();
                }else{
                    playAnimation(true); //continue moving in reverse
                }
                directionOfMotion = DirectionOfMotion.down;
            }else if(elevatorCurrentFrameInFraction==floor1UpFrameInFraction) pauseElevatorAnimation();
        }
        else if(elevatorCurrentFrameInFraction>= floor2FrameInFraction){ ///follow the animation flow if they are equal
            if(elevatorCurrentFrameInFraction<floor1DownFrameInFraction){
                if(directionOfMotion==DirectionOfMotion.up){
                    playAnimationInReverse(false); ///return animation to default direction
                }else{
                    playAnimation();
                }
                directionOfMotion = DirectionOfMotion.down;
            }
            else if(elevatorCurrentFrameInFraction>floor1DownFrameInFraction){
                if(directionOfMotion==DirectionOfMotion.up){
                    playAnimation(true); //continue moving in reverse
                }else{
                    playAnimationInReverse();
                }
                directionOfMotion = DirectionOfMotion.up;
            }else if(elevatorCurrentFrameInFraction==floor1DownFrameInFraction) pauseElevatorAnimation();
        }

        desiredFloor = 1;
    }


    public void moveToSecondFloor(){
        setCurrentAnimationFrame("",true,floor2FrameInFraction);
        return;
        //Bugs when using code meant for simulation mode, so using digitalTwinMode code instead
        
        if(isAtSpecifiedFloor(2)) {
            customLog("CANNOT MOVE TO SECOND FLOOR. 1:"+elevatorCurrentFrameInFraction.ToString()+" 2:"+floor2FrameInFraction.ToString() );
            pauseElevatorAnimation();
            return;
        }

        if(elevatorCurrentFrameInFraction<floor2FrameInFraction){
            if(directionOfMotion==DirectionOfMotion.up){
                playAnimation();
            }else{
                playAnimationInReverse(false); ///return animation to default direction
            }
            directionOfMotion = DirectionOfMotion.up;
        }
        else if(elevatorCurrentFrameInFraction> floor2FrameInFraction){ 
            if(directionOfMotion==DirectionOfMotion.up){
                playAnimation(true); //continue moving in reverse
            }else{
                playAnimationInReverse();
            }
            directionOfMotion = DirectionOfMotion.up;
        }else if(elevatorCurrentFrameInFraction == floor2FrameInFraction) pauseElevatorAnimation();

        desiredFloor = 2;
    }

    public void moveToGroundFloor(){
        setCurrentAnimationFrame("",true,floor0UpFrameInFraction);
        return;
        //Bugs when using code meant for simulation mode, so using digitalTwinMode code instead
        
        if(isAtSpecifiedFloor(0) || elevatorCurrentFrameInFraction == floor0UpFrameInFraction 
                ||elevatorCurrentFrameInFraction == floor0DownFrameInFraction) {
            customLog("CANNOT MOVE TO GROUND FLOOR. 1:"+elevatorIsAtASpecificFloor.ToString()+" 2:"+elevatorCurrentFrameInFraction.ToString() );
            pauseElevatorAnimation();
            return;
        }

        if(elevatorCurrentFrameInFraction<floor2FrameInFraction){
            if(directionOfMotion==DirectionOfMotion.up){
                playAnimationInReverse();
            }else{
                playAnimation(true); //continue moving in reverse
            }
            directionOfMotion = DirectionOfMotion.down;
        }
        else if(elevatorCurrentFrameInFraction>= floor2FrameInFraction){ ///follow the animation flow if they are equal
            if(directionOfMotion==DirectionOfMotion.up){
                playAnimationInReverse(false); ///return animation to default direction
            }else{
                playAnimation();
            }
            directionOfMotion = DirectionOfMotion.down;
        }

        desiredFloor = 0;
    }
    

    public void stopElevatorAnimation(){ //stop the motion ONLY; dont change fields
        pauseElevatorAnimationInDigitalTwinMode();  //Bugs when using code meant for simulation mode, so using digitalTwinMode code instead
        // pauseElevatorAnimation(true);
    }

    public void groundFloorButtonPressed(){
        if(animationControlMode == AnimationControlMode.simulation && !controllingElevPosInSimModeUsingFlutter){
            moveToGroundFloor();
        }else{
            sendMessageToFlutter("0");
        }
    }

    public void firstFloorButtonPressed(){
        if(animationControlMode == AnimationControlMode.simulation && !controllingElevPosInSimModeUsingFlutter){
            moveToFirstFloor();
        }else{
            sendMessageToFlutter("1");
        }
    }

    public void secondFloorButtonPressed(){
        if(animationControlMode == AnimationControlMode.simulation && !controllingElevPosInSimModeUsingFlutter){
            moveToSecondFloor();
        }else{
            sendMessageToFlutter("2");
        }
    }

    public void stopButtonPressed(){
        if(animationControlMode == AnimationControlMode.simulation && !controllingElevPosInSimModeUsingFlutter){
            stopElevatorAnimation();
        }else{
            sendMessageToFlutter("STOP");
        }
    }

    void playAnimationAtASpecificTime(string animationHashName,int layer,float startTime,bool playAnimation=true){
        animator.Play(animationHashName,layer,startTime);
        if(playAnimation) animator.speed = 1;
    }
    void playAnimation(bool movingInReverse = false){
        animator.speed = 1;
        elevatorIsAtASpecificFloor = false;
        if(!movingInReverse) elevatorMotionIsInReverse = false; ///in some cases,reverse motion is true e.g if ground button pressed and it is at floor1Up frame moving downwards
        else elevatorMotionIsInReverse = true;
        customLog("Playing Animation");
    }

    void playAnimationInDigitalTwinMode(){
        ///comment out this line if setting current frame manually
        // animator.speed = 1;

        animationIsPlaying = true;
        calcDigitalTwinMvtAnimationVariables();
        customLog("Playing Animation");
    }

    void calcDigitalTwinMvtAnimationVariables(){
        animationStartTime = DateTime.UtcNow;
        animationStartFrame = elevatorCurrentFrameInFraction;
        // float diff = Mathf.Abs((float) previouslyReceievedFrameFraction-elevatorCurrentFrameInFraction);
        // digitalTwinMvtAnimationSteps = (float)(diff / (float)(Time.deltaTime * 1000f));//Time.deltaTime is in seconds
        // digitalTwinMvtAnimationSteps = 0.0009529f; //tuning to match that of modelViewer (used in website)
    }

    void playAnimationInReverse(bool movingInReverse = true){
        float currentSpeed = animator.GetFloat("CustomSpeed");
        float newSpeed = -1.0f * currentSpeed;
        ///animator.speed is changed by changing its custom multiplier set in parameters in animator window. This is bcoz animator.speed cant be a negative value
        animator.SetFloat("CustomSpeed",newSpeed); 
        animator.speed = 1;

        elevatorIsAtASpecificFloor = false;
        if(movingInReverse) elevatorMotionIsInReverse = true; ///in some cases,reverse motion is false e.g if ground button pressed and it is was at floor1Down frame moving upwards
        else elevatorMotionIsInReverse = false;
        customLog("Playing Animation in Reverse");
    }

    void playReverseAnimationInDigitalTwinMode(){
        ///comment out this block if setting current frame manually
        // float currentSpeed = animator.GetFloat("CustomSpeed");
        // float newSpeed = -1.0f * currentSpeed;
        // ///animator.speed is changed by changing its custom multiplier set in parameters in animator window. This is bcoz animator.speed cant be a negative value
        // animator.SetFloat("CustomSpeed",newSpeed); 
        // animator.speed = 1;

        animationIsPlaying = true;
        calcDigitalTwinMvtAnimationVariables();
        customLog("Playing Animation in Reverse");
    }

    void resetAnimationSpeed(){
        float currentSpeed = animator.GetFloat("CustomSpeed");
        customLog("Current speed: " +currentSpeed.ToString());
        if(currentSpeed == 1.0f) return;
        else {
            float newSpeed = -1.0f * currentSpeed;
            animator.SetFloat("CustomSpeed",newSpeed); 
        }
    }

    void pauseElevatorAnimation(bool stoppingAnimationOnCommand =false){
        animator.speed = 0;
        if(!stoppingAnimationOnCommand){//if one clicks stop button dont set current floor
            currentFloor = desiredFloor; 
            elevatorIsAtASpecificFloor = true;
            enteredAnotherAnimationLoop = false;
        } 
        customLog("Paused Animation");
    }

    void pauseElevatorAnimationInDigitalTwinMode(){
        ///comment out this block if setting current frame manually
        // animator.speed = 0;

        enteredAnotherAnimationLoop = false;
        animationIsPlaying = false;
        customLog("Paused Animation");
    }


    void elevatorAnimationDone(int completedLoops){
        currentFloor = 0;
        customLog("RESETTING ANIMATION------");
        noOfCompletedAnimationLoop = (int) completedLoops;
        // customLog("Current speed is "+ currentSpeed.ToString());
        // customLog("New speed to be "+ newSpeed.ToString());
    }

    void sendMessageToFlutter(string message){
        try{
            unityMessageManager.SendMessageToFlutter(message);
            customLog("Sent message to Flutter");
        }catch(Exception e){
            customLog("ERROR Sending message to Flutter. "+e);
            // Debug.LogException(e,this);
        }
    }

    ///CUSTOM TIMER
    ///used to set the desired floor if frame fraction of 1 is recieved when animationIsPlaying
    float setPositionAfterDeafModeInterval = 20.0f/1000.0f; //is in seconds
    Action? setPosAfterDeafModeTimerCallback = null;
    void setPositionAfterDeafModeTimer(){
        if(setPosAfterDeafModeTimerCallback == null) return;
        setPositionAfterDeafModeInterval -= Time.deltaTime;
        if(setPositionAfterDeafModeInterval < 0){
            if(!animationIsPlaying) {
                setPosAfterDeafModeTimerCallback?.Invoke();
                setPosAfterDeafModeTimerCallback = null;
            }
            setPositionAfterDeafModeInterval = 20.0f/1000.0f;
        }
    }
    ///For more info about Actions, you can also research more on delegates -> https://stackoverflow.com/questions/25690325/a-delegate-for-a-function-with-variable-parameters/25690479#25690479
    ///Delegate allows storage of an array of functions

    void customLog(string log){
        Debug.Log("CLOG: "+log);
    }

    // void customLog(float logTypeFloat){
    //     Debug.Log(logTypeFloat);
    // }

}

enum DirectionOfMotion { up, down }
enum AnimationControlMode { digitalTwin, simulation }
//TODO: Consider if elevator is stopped then a floor button is tapped

///To duplicate animations from .fbxx file to parent folder, click Ctrl+D
///Create a new Animation controller and add the duplicated animations to the animator window
///To view each frame of an animation, go to Animation Window, then select the prefab that contains the .glb file.

///If you want a certain type after a mathematical operation, Cast all operands as the desired type e.g
// float result = (float)lastFrame/(float)totalAnimationFrames; //Both lastFrame and totalAnimationFrames were of type int
///C# considers null as a type so e.g int a, so if(a==null) is ALWAYS false as type int can never be type null but somehow works in other types
//Above statement is because C# considers type nullable
//No Timer package in Unity so customize yours 

//Note that receivedCurrentFrameFraction is a fraction of time taken from one floor to another
///Decided to use Newton's 1st law of motion as automatic motion using unity animation was glitchy

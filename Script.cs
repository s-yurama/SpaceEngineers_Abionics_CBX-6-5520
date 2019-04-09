// --------
// Settings
// --------
// custome data module id
const string CUSTOM_DATA_ID_MODULE = "Maneuver";
const string CUSTOM_DATA_ID_HEADLIGHT = "Headlight";
const string CUSTOM_DATA_ID_BAY_GEAR = "LandingGearBay";

// --------
// Messages
// --------
// Error
const string ERROR_UPDATE_TYPE_INVALID = "Invalid update types.";
const string ERROR_BLOCKS_NOT_FOUND    = "Loading blocks is failure.";
const string ERROR_COCKPIT_NOT_FOUND   = "Identified Cockpit Not Found.";

// --------
// Class
// --------
Blocks       blocks;
Vessel       vessel;
ErrorHandler error;

// --------
// run interval
// --------
const double EXEC_FRAME_RESOLUTION = 30;
const double EXEC_INTERVAL_TICK = 1 / EXEC_FRAME_RESOLUTION;
double currentTime = 0;

// --------
// update interval
// --------
const int UPDATE_INTERVAL = 10;
double updateTimer = 0;

public Program()
{
    // The constructor, called only once every session and
    // always before any other method is called. Use it to
    // initialize your script. 
    //     
    // The constructor is optional and can be removed if not
    // needed.
    // 
    // It's recommended to set RuntimeInfo.UpdateFrequency 
    // here, which will allow your script to run itself without a 
    // timer block.

    updateTimer = UPDATE_INTERVAL;
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
}

public void Save()
{
    // Called when the program needs to save its state. Use
    // this method to save your state to the Storage field
    // or some other means. 
    // 
    // This method is optional and can be removed if not
    // needed.
}

public void Main(string argument, UpdateType updateSource)
{
    // The main entry point of the script, invoked every time
    // one of the programmable block's Run actions are invoked,
    // or the script updates itself. The updateSource argument
    // describes where the update came from.
    // 
    // The method itself is required, but the arguments above
    // can be removed if not needed.

    if ( error == null ) {
        error = new ErrorHandler(this);
    }

    // check updateTypes
    if( (updateSource & ( UpdateType.Update1 | UpdateType.Update10 | UpdateType.Update100 )) != 0 ) {
        currentTime += Runtime.TimeSinceLastRun.TotalSeconds;
        if (currentTime < EXEC_INTERVAL_TICK) {
            return;
        }
        
        procedure();

        currentTime = 0;
    } else if ( (updateSource & UpdateType.Once) != 0) {
        if (argument == "gearDown") {
        
        }
        if (argument == "gearUp") {
        
        }
    } else {
        error.add(ERROR_UPDATE_TYPE_INVALID);
    }
    error.echo();
}

/**
 * main control procedure
 */
private void procedure()
{
    updateTimer += currentTime;

    if (updateTimer < UPDATE_INTERVAL) {
        Echo($"next refresh: {UPDATE_INTERVAL - updateTimer:0}");
    } else {    
        updateTimer = 0;
        Echo("updating...");

        blocks = new Blocks(GridTerminalSystem, Me.CubeGrid, error);
    }

    if( error.isExists() ) {
        return;
    }

    if ( vessel == null ) {
        vessel = new Vessel();
    }
    vessel.setCockpit(blocks.getCockpit());
    maneuverControl();
    //fireControl();
    //damageControl();
    //ECM();
}

/**
 * maneuvering control
 */ 
private void maneuverControl()
{   
    //double velocity = chassis.getVelocity();
    syncHandBrakes();
}

/**
 *  sync hand brake between Maincockpit and GearRemoteControl
 */
private void syncHandBrakes()
{
    bool isHandBraked = vessel.isHandBraked();
    
    foreach(IMyRemoteControl remoteControlGear in blocks.remoteControlGearList ) {
        Echo("updating handbrakes");
        Echo(isHandBraked.ToString());
        Echo(remoteControlGear.HandBrake.ToString());
        remoteControlGear.HandBrake = isHandBraked;
        Echo(remoteControlGear.HandBrake.ToString());
        //if (remoteControlGear.HandBrake != isHandBraked) {
        //    remoteControlGear.Apply("Handbrake");
        //}
    }
}

/**
 * FCS
 */
private void fireControl()
{
}

/**
 * Tank Chassis Class 
 */
private class Vessel
{
    // primary ship controller of vessel
    public IMyShipController primaryController {get; set;}

    // translation speed value
    public float Left    {get; set;}
    public float Up      {get; set;}
    public float Forward {get; set;}

    // rolling value
    public float Pitch   {get; set;}
    public float Roll    {get; set;}
    public float Yaw     {get; set;}
    
    public void setCockpit(IMyShipController Controller)
    {
        this.primaryController = Controller;
        getControl();
    }
    
    public void getControl()
    {
       updateTranslationControl();
       updateYawPitchRoll();
    }

    public void updateTranslationControl()
    {
        Vector3 translationVector = this.primaryController.MoveIndicator;

        Left    = translationVector.GetDim(0);
        Up      = translationVector.GetDim(1);
        Forward = translationVector.GetDim(2);
    }

    public void updateYawPitchRoll()
    {
        Vector2 yawPitchVector = this.primaryController.RotationIndicator;

        Pitch = yawPitchVector.X;                     // turn Up Down
        Yaw   = yawPitchVector.Y;                     // turn Left Right
        Roll  = this.primaryController.RollIndicator; // roll
    }

    public double getVelocity()
    { 
       return this.primaryController.WorldMatrix.Forward.Dot(this.primaryController.GetShipVelocities().LinearVelocity);
    }

    public bool isHandBraked()
    {
        return this.primaryController.HandBrake;
    }
}

private class Blocks
{
    private IMyGridTerminalSystem currentGrid;
    private IMyCubeGrid           cubeGrid;
    private ErrorHandler          error;

        
    public List<IMyLightingBlock> headLightList;
    public List<IMyTerminalBlock> ownedGridBlocksList;

    public List<IMyShipController> cockpitList;
        
    public List<IMyRemoteControl> remoteControlGearList;

    public List<IMyPistonBase> pistonGearList;
        
    public List<IMyMotorStator> rotorGearBay;
    
    //public List<IMyMotorStator> rotorElv;
    //public List<IMyMotorStator> rotorLadder;

    public bool isUnderControl = false;

    // default constructor
    public Blocks(
        IMyGridTerminalSystem currentGrid,
        IMyCubeGrid cubeGrid,
        ErrorHandler error
    )
    {
        this.currentGrid = currentGrid;
        this.cubeGrid    = cubeGrid;
        this.error       = error;

        this.cockpitList           = new List<IMyShipController>();
        this.remoteControlGearList = new List<IMyRemoteControl>();
        this.headLightList         = new List<IMyLightingBlock>();
        this.rotorGearBay          = new List<IMyMotorStator>();
        this.ownedGridBlocksList   = new List<IMyTerminalBlock>();

        this.error.clear();

        this.updateOwenedGridBlocks();

        if( ! this.isOwened() ) { 
            error.add(ERROR_BLOCKS_NOT_FOUND);
            return;
        }

        this.clear();

        this.assign();
    }

    public IMyShipController getCockpit() {
        // if exists Undercontrol cockpit, then use it.
        foreach(IMyShipController controller in this.cockpitList){
            if ( controller.IsFunctional && controller.IsUnderControl ) {
                this.isUnderControl = true;
                return controller;
            }
        }
        this.isUnderControl = false;

        return this.cockpitList[0];
    }

    /**
     * update owned grid blocks from connected grids.
     *
     * this is neccessary if you put cockpit on subgrid (ex: turret)
     * and removing duplicate blocks between rotor connected grids,
     * and seperate base and head.
     * (cubeGrid is rotorBase, topGrid is rotorHead.)
     */
    private void updateOwenedGridBlocks()
    {
        currentGrid.GetBlocksOfType<IMyMechanicalConnectionBlock>(this.ownedGridBlocksList);

        HashSet<IMyCubeGrid> CubeGridPair = new HashSet<IMyCubeGrid>();
        CubeGridPair.Add(this.cubeGrid);

        IMyMechanicalConnectionBlock connectionBlock;

        // get all of CubeGrid by end of it
        bool isExists = true;
        while( isExists ) {
            isExists = false;
            for( int i = 0; i < this.ownedGridBlocksList.Count; i++ ) {
                connectionBlock = this.ownedGridBlocksList[i] as IMyMechanicalConnectionBlock;

                if ( CubeGridPair.Contains(connectionBlock.CubeGrid) || CubeGridPair.Contains(connectionBlock.TopGrid) ) {
                    CubeGridPair.Add(connectionBlock.CubeGrid);
                    CubeGridPair.Add(connectionBlock.TopGrid);
                    this.ownedGridBlocksList.Remove(this.ownedGridBlocksList[i]);
                    isExists = true;
                }
            }
        }
        
        this.ownedGridBlocksList.Clear();
        
        //get filtered block
        currentGrid.GetBlocksOfType<IMyTerminalBlock>(
            this.ownedGridBlocksList,
            owenedBlock => CubeGridPair.Contains(owenedBlock.CubeGrid)
        );
    }

    private bool isOwened()
    {
        if (this.ownedGridBlocksList.Count > 0) {
            return true;
        }
        return false;
    }

    public bool isHeadLightOn()
    { 
        foreach(IMyLightingBlock light in this.headLightList){
            if ( light.IsWorking ) {
                return true;
            }
        }
        return false;
    }

    private void clear()
    {
        this.cockpitList.Clear();
        this.remoteControlGearList.Clear();
        this.headLightList.Clear();
        this.rotorGearBay.Clear();
    }

    private void assign()
    {
        List<IMyLightingBlock> lightList = new List<IMyLightingBlock>();
        List<IMyMotorStator>   rotorList = new List<IMyMotorStator>();

        foreach ( IMyTerminalBlock block in this.ownedGridBlocksList ) {
            if ( true
                && block is IMyRemoteControl
                && block.CustomData.Contains(CUSTOM_DATA_ID_MODULE)
                && block.IsFunctional
            ) {
                this.remoteControlGearList.Add(block as IMyRemoteControl);
                continue;
            }
            if ( true 
                && block is IMyShipController
                && block.CustomData.Contains(CUSTOM_DATA_ID_MODULE)
                && block.IsFunctional
            ) {
                this.cockpitList.Add(block as IMyShipController);
                continue;
            }
            if ( true
                && block is IMyLightingBlock
                && block.CustomData.Contains(CUSTOM_DATA_ID_MODULE)
                && block.IsFunctional
            ) {
                lightList.Add(block as IMyLightingBlock);
                continue;
            }
            if ( true 
                && block is IMyMotorStator
                && block.CustomData.Contains(CUSTOM_DATA_ID_MODULE)
                && block.IsFunctional
            ) {
                rotorList.Add(block as IMyMotorStator);
                continue;
            }
        }

        if( ! this.isExistsCockpit() ) { 
            this.error.add(ERROR_COCKPIT_NOT_FOUND);
            return;
        }

        var centerOfMass = this.getCockpit().CenterOfMass;

        foreach ( IMyLightingBlock light in lightList ) {
            if ( light.CustomData.Contains(CUSTOM_DATA_ID_HEADLIGHT) ) {
                headLightList.Add(light);
                continue;
            }
        }

        foreach ( IMyMotorStator rotor in rotorList ) {
            if ( rotor.CustomData.Contains(CUSTOM_DATA_ID_BAY_GEAR) ) {
                rotorGearBay.Add(rotor);
                continue;
            }
        }
    }

    private bool isExistsCockpit()
    {
        if ( this.cockpitList.Count > 0 ) {
            return true;
        }
        return false;
    }
}

private class ErrorHandler
{
    private Program program;
    private List<string> errorList = new List<string>();

    public ErrorHandler(Program program)
    {
        this.program = program;
    }

    public bool isExists()
    {
        if ( this.errorList.Count > 0 ) {
            return true;
        }
        return false;
    }

    public void add(string error)
    {
        this.errorList.Add(error);
    }

    public void clear()
    {
        this.errorList.Clear();
    }

    public void echo()
    { 
        foreach ( string error in this.errorList ) {
            this.program.Echo("Error: " + error);
        }
    }
}
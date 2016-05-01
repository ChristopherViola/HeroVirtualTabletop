#Include TargetedModel.ahk
#Include CommandParser.ahk
class 3DPositioner{
	IncrementCrowdAccordingToLocationInstructions(memoryInstanceDestinationMap, locationInstructions, distance,charactersBeingMoved:=1){
		;charactersBeingMoved:=memoryInstanceDestinationMap.MaxIndex
		increment := distance /20 ;*  (CharactersBeingMoved)
		if (increment < .15){
			increment:=.15 ;*  (CharactersBeingMoved)
		}
		while(counter < (distance  * 8.5 )){ ;* charactersBeingMoved)){
			if(charactersBeingMoved == 1){
				sleep 25
			}
			else
			{
				sleep ( 200 / charactersBeingMoved )
			}
			counter+= increment
			for key, entry in memoryInstanceDestinationMap {
				memoryInstance:=entry.Instance
				currentPositonx:=memoryInstance.X
				currentPositonz:=memoryInstance.Z
				currentPositony:=memoryInstance.y
				
				destination:= entry.destination
				if(destination <> ""){
					if(abs( destination.x - currentPositonx) > 2.5 ) {
						memoryInstance.x:=currentPositonx+(increment * locationInstructions.X)
					}
					if(abs( destination.z - currentPositonz) > 2.5 ){
						memoryInstance.z:=currentPositonz+(increment * locationInstructions.z)
					}
					if(abs(  destination.y - currentPositony)  > 2.5 ){
						newY:=currentPositony+(increment * locationInstructions.y)
					}
					if(abs( destination.y - currentPositony)  <= 2.5 and abs( destination.z - currentPositonz) <= 2.5 and abs( destination.x - currentPositonx) <= 2.5 ){
						break
					}
				}
				else{
					memoryInstance.x:=currentPositonx+(increment * locationInstructions.X)
					memoryInstance.z:=currentPositonz+(increment * locationInstructions.Z)
					newY:=currentPositony+(increment * locationInstructions.y)
				}
				ground:=entry.Ground
				if(newY > ground){
					memoryInstance.y:=newY
				}
				else{
					memoryInstance.y:= ground
				}
			}
		}	
	}
	IncrementAccordingToLocationInstructions(memoryInstance, locationInstructions, distance, ground , destination:="", charactersBeingMoved:=1,fast:=false){
		
		increment := distance /20 * (CharactersBeingMoved *3)
		if (increment < .15){
			increment:=.15 * (CharactersBeingMoved *3)
		}
		
		while(counter < (distance * 8.5)){ ; * charactersBeingMoved ) ){
			if(charactersBeingMoved == 1){
				sleep 25
			}
			else
			{
				if(fast==true){
					sleep ( 3 / charactersBeingMoved )
				}
				else
						sleep ( 25 / charactersBeingMoved )
			}
			counter+= increment
			currentPositonx:=memoryInstance.X
			currentPositonz:=memoryInstance.Z
			currentPositony:=memoryInstance.y
			if(destination <> ""){
				if(abs( destination.x - currentPositonx) > 2.5 ) {
					memoryInstance.x:=currentPositonx+(increment * locationInstructions.X)
				}
				if(abs( destination.z - currentPositonz) > 2.5 ){
					memoryInstance.z:=currentPositonz+(increment * locationInstructions.z)
				}
				if(abs(  destination.y - currentPositony)  > 2.5 ){
					newY:=currentPositony+(increment * locationInstructions.y)
				}
				if(abs( destination.y - currentPositony)  <= 2.5 and abs( destination.z - currentPositonz) <= 2.5 and abs( destination.x - currentPositonx) <= 2.5 ){
					break
				}
			}
			else{
				memoryInstance.x:=currentPositonx+(increment * locationInstructions.X)
				memoryInstance.z:=currentPositonz+(increment * locationInstructions.Z)
				newY:=currentPositony+(increment * locationInstructions.y)
			}
			if(newY > ground){
				memoryInstance.y:=newY
			}
			else{
				memoryInstance.y:= ground
			}
		}
	}

	
	AdjustDirectionBasedOnFacingAndDirectionTravelling(t,direction){
		facing:=t.Facing 
		;facing:=0
		directionAdj:={Still:0,Forward:0, Right:90, Back:180, left:270, Up:0, Down:0}
		
		if( facing + directionAdj[direction] > 360){
			return directionAdj[direction] -(360 - facing)
		}
		else
		{
			return facing + directionAdj[direction]
		}
	}


	RotateVector(facing, newFacing,vector){
		
		rotation:=facing - newFacing

		pi:=3.141592653589793
		radians:= rotation * pi/180

		rotated:= {}

		rotated.x:=(vector.x * cos(radians) - vector.z * sin(radians))
		rotated.z:=vector.z * cos(radians) + vector.x * sin(radians) 
		rotated.y:=vecor.y
		return rotated
	}
	UpdateLocationInstructionsBasedOnFacing(travelFacing, locationInstructions=""){
		if(locationInstructions==""){
			locationInstructions:={x:0,y:0, z:0}
		}
		if(travelFacing < 180){
			offset:=travelFacing/90
		}
		else{
			offset:=(360 - travelFacing)/90
		}
		
		locationInstructions.z:= 1 * (1 - offset)
		if(offset <= 1){
			locationInstructions.x:= 1 - locationInstructions.z
		}
		else{
			locationInstructions.x:=1 - (locationInstructions.z * -1) 
		}
		if(travelFacing >=180){
			locationInstructions.x:=locationInstructions.x*-1
		}
		return locationInstructions
	}
	
	UpdateLocationInstructionsBasedOnPitch(locationInstructions, pitch, direction,memoryInstance){
  		if(direction =="Forward" or direction == "Back" or direction == "Up" or direction =="Down"){
			offset:=pitch/90
			if(direction =="Forward" or direction =="Back"){
				locationInstructions.z:= locationInstructions.z * (1 - offset)
				locationInstructions.x:= locationInstructions.x * (1 - offset)
				;if(pitch > 0){
					if(direction =="Forward"){
						locationInstructions.y:= 1 * offset
					}
					else{
						if(direction =="Back"){
							locationInstructions.y:= -1 * offset
						}
					}
			}
			if(direction =="Up" or direction=="Down"){
				locationInstructions.y:= locationInstructions.y * (1 - offset)
				  
				if(direction=="Up"){
					if(pitch < 180){
						horizontalDirection:="Back"
					}
					else
					{
						horizontalDirection:="Forward"
					}
				}
				else{
					if(direction=="Down"){
						if(pitch < 180){
							horizontalDirection:="Forward"
						}
						else
						{
							horizontalDirection:="Back"
						}
					}
				}
				
				facing:= this.AdjustDirectionBasedOnFacingAndDirectionTravelling(memoryInstance, horizontalDirection)
				
				horizontalInstruction:= this.UpdateLocationInstructionsBasedOnFacing(facing)
				
				locationInstructions.x:= horizontalInstruction.X * offset
				locationInstructions.z:= horizontalInstruction.z * offset
			}
		}
		return 	locationInstructions
		
	}  
	UpdateLocationInstructionsBasedOnPositionOfStartAndDestination(currentLocation, location){
		totalX:=currentLocation.X*-1 + location.X
		totalY:=currentLocation.y*-1 + location.y
		totalZ:=currentLocation.z*-1 + location.z
		locationInstructions:={x:0, y:0, z:0, facing:0}
		if(abs(totalx) >= abs(totalY) and abs(totalx) >= abs(totalz)){
			if(totalX > 0){
				locationInstructions.X:= 1
			}
			else{
				locationInstructions.X:= -1
			}
			locationInstructions.Y:= totalY/abs(totalX)
			locationInstructions.z:= totalz/abs(totalx)
		}
		else{
			if(abs(totaly) >= abs(totalz) and abs(totaly) >= abs(totalx)){
				if(totaly > 0){
					locationInstructions.y:= 1
				}
				else{
					locationInstructions.y:= -1
				}
				locationInstructions.z:= totalz/abs(totaly)
				locationInstructions.x:= totalx/abs(totaly)
			}
			else{
				if(abs(totalz) >= abs(totalx) and abs(totalz) >= abs(totaly)){
					if(totalz > 0){
						locationInstructions.z:= 1
					}
					else{
						locationInstructions.z:= -1
					}
					locationInstructions.y:= totaly/abs(totalz)
					locationInstructions.x:= totalx/abs(totalz)
				}
			}
		}
		return locationInstructions
	}
	FacingTowardsCamera{
		Get{
			parser:=CommandParserFactory.NewTempKeybindFileParser()
			parser.Build("SpawnNPC",["V_Dest_LC_Trap_Poison_01", "target"])
			parser.SubmitCommand()
			sleep 400
			
			parser.Build("TargetName", ["target"])
			parser.SubmitCommand()
			sleep 600
			
			facing:=this.FacingTowardsTarget
			parser.Build("DeleteNPC", [])
			parser.SubmitCommand()
			return facing
		}
	}
	
	FacingTowardsTarget{
		get{
			t:= new Target()
			facing:= t.GetValueObject().Facing
			if(facing < 181){
				facing:=facing + 180
			}
			else{
					facing:= 180 - (360 - facing)
			}
			return facing
		}
	}
	CalculateRangeToLocation(currentLocation, location){
		totalX:=(currentLocation.X*-1 + location.X)
		totalY:=(currentLocation.y*-1 + location.y)
		totalZ:=(currentLocation.z*-1 + location.z)
		
		
		distance:= (totalX * totalX ) + (totalY *totalY ) + (totalz *totalz)
		distance:= Sqrt(distance)
		
		return distance
	}
	CalculateDelta(firstLocation, secondLocation){
		delta:= {x:0 , y:0, z:0}
		delta.X:= secondLocation.x - firstLocation.x
		delta.y:= secondLocation.y - firstLocation.y
		delta.z:= secondLocation.z - firstLocation.z
		return delta
	}
	LocationAdd(location1, location2){
		sum:= {x:0 , y:0, z:0}
		sum.x:=location1.x+location2.x
		sum.y:=location1.y+location2.y
		sum.z:=location1.z+location2.z
		return sum
	}
	CalculateAbsoluteDelta(firstLocation, secondLocation){
		delta:= {x:0 , y:0, z:0}
		
		delta.X:= abs(secondLocation.x - firstLocation.x)
		delta.y:= abs(secondLocation.y - firstLocation.y)
		delta.z:= abs(secondLocation.z - firstLocation.z)
		if(firstLocation.x < secondLocation.x){
			delta.X:=delta.X * -1
		}
		if(firstLocation.y < secondLocation.y){
			delta.y:=delta.y * -1
		}
		if(firstLocation.z < secondLocation.z){
			delta.z:=delta.z * -1
		}
		return delta
	}
}

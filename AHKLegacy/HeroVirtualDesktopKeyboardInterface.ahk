#SingleInstance force
#Include CharacterManager.ahk
#Include AnimationManager.ahk
#Include CrowdManager.ahk
#Include Movement.ahk
#include TargetedModel.ahk


#Include HeroVirtualDesktopGraphicalInterface.ahk
#Include CrowdManagerKeyboardInterface.ahk
#Include TargetInterface.ahk

#Include HCSEventHandlerInterface.ahk


crowdInterface:= CrowdKeyboardInterface.GetInstance()
targetingInterface:= new TargetInterface()
moveInterface:= MoveInterface.GetInstance()

fileEventHandler:=FileListeningEventHandler.GetInstance()
animationInterface:= AnimationListInterface.GetInstance()
d:= new DesktopGui()
class KeyHandler
{
	Running:=false
	animationManager:=""
	characterManager:=""
	GetInstance(){
		if (this._instance==""){
			this._instance:= new KeyHandler()
		}
		return this._instance
		
	}
	
	__New(){
		this.characterManager:= CharacterManager.GetInstance()
		this.animationManager:= AnimationManager.GetInstance()
		this.crowdManager:= CrowdManager.GetInstance()
		;this.InitKeys()
	}
		

	HandleCharacterEvent(method, ignoreMode:= false)
	{
		this.Running:=true
		SoundPlay sound\N_CharPageScroll.wav
		if(this.crowdManager.CrowdMode == false or ignoreMode== true){
			character:=this.characterManager.ActivateTargetedCharacter
			m:=this.characterManager
			m[method](character)
			this.characterManager.CommandParser.MarkCommandComplete()
		}
		else
		{
			this.crowdManager.LoadCrowdForCharacter()
			characters:= this.crowdManager.CharactersInCrowd
			for key, character in characters{
				this.characterManager.TargetCharacter(character)
				sleep 200
				this.characterManager.ActivateTargetedCharacter
				m:=this.characterManager
				m[method](character)
			}
		}
		this.Running:=false
	}
	
	HandleCrowdEvent(method)
	{
		this.Running:=true
		SoundPlay sound\N_CharPageScroll.wav
		m:=this.crowdManager
		m[method](character)
		this.Running:=false
	}
	
	HandleAnimationEvent(method, animation)
	{
 		this.Running:=true
	;	SoundPlay sound\N_CharPageScroll.wav
		if(this.crowdManager.CrowdMode== false){
			character:=this.characterManager.ActivateTargetedCharacter
			m:=this.animationManager
			m[method](animation , character)
		}
		else
		{
			this.crowdManager.LoadCrowdForCharacter()
			characters:= this.crowdManager.CharactersInCrowd
			for key, character in characters{
				this.characterManager.TargetCharacter(character)
				this.characterManager.ActivateTargetedCharacter
				m:=this.animationManager
				m[method](animation , character)
			}
		}
		this.Running:=false
	}
}
SetTimer HandleAllInputs, 100	

#IfWinActive ahk_exe cityofheroes.exe

~p::
{
	KeyHandler.GetInstance().HandleCharacterEvent("SpawnCharacterToDesktop")
	return
}
~h::
{
	KeyHandler.GetInstance().HandleCharacterEvent("TargetAndFollow")
	return
}
~c::
{
	handler:= KeyHandler.GetInstance()
	if(handler.characterManager.Camout=="true"){
		handler.HandleCharacterEvent("TurnCameraIntoCharacter")
	}
	else
	{	
		handler.HandleCharacterEvent("TurnCharacterIntoCamera")
	}
	return
}
~j::
{
	KeyHandler.GetInstance().HandleCharacterEvent("JumpModelToCharacter")
	return
}

~i::	
{
	KeyHandler.GetInstance().HandleCharacterEvent("JumpCrowdToCameraInFormation", true)
	return
}

~t::
{
	KeyHandler.GetInstance().HandleCharacterEvent("TargetCharacter")
	return
}
~v::
{
	KeyHandler.GetInstance().HandleCrowdEvent("SaveLocationOfSelectedModel")
	return
}

~l::
{
	KeyHandler.GetInstance().HandleCrowdEvent("SpawnAndPlaceNextModelFromCrowd")
	return
}

~r::
{
	characterManager:=CharacterManager.GetInstance()
	targeter:=characterManager.Characters["Targeter"]
	if (targeter.memoryInstance ==""){
		characterManager.SpawnCharacterToDesktop(targeter)
		
	}
	targeter.MemoryInstance.TargetMe()
	characterManager.buildJustTheJumpCommand(targeter)
	facing:=targeter.Memoryinstance.Facing
	targeter.Memoryinstance.Facing:=Facing
	return
}




~1::
{
	playAnimation("1")
	return
}
~2::
{
	playAnimation("2")
	return
}
~3::
{
	playAnimation("3")
	return
}
~4::
{
	playAnimation("4")
	return
}
~5::
{
	playAnimation("5")
	return
}
~6::
{
	playAnimation("6")
	return
}
~7::
{
	playAnimation("7")
	return
}
~8::
{
	playAnimation("8")
	return
}
~9::
{
	playAnimation("9")
	return
}

!Right::
{
	global moveInterface
	moveInterface.MoveCharacter("Right")
	return
}

!Up::
{
	global moveInterface
	moveInterface.MoveCharacter("Up")
	return
}

!Left::
{
	global moveInterface
	moveInterface.MoveCharacter("Left")
	return

}

!Down::
{
	global moveInterface
	moveInterface.MoveCharacter("Down")
	return
}

!Space::
{
	global moveInterface
	moveInterface.MoveCharacter("Space")
	return
}
!w::
{
	global moveInterface
	moveInterface.MoveCharacter("w")
	return
}


	
	
!a::
{
	global moveInterfBY
	moveInterface.MoveCharacter("a")
	return
}
!s::
{
	global moveInterface
	moveInterface.MoveCharacter("s")
	return
}
!d::
{
	global moveInterface
	moveInterface.MoveCharacter("d")
	return
}

!x::
{
	global moveInterface
	moveInterface.MoveCharacter("x")
	return
}

!z::
{
	global moveInterface
	moveInterface.MoveCharacter("z")
	return
}

!y::
{
	
	global moveInterface
	player:= new Player()
	moveInterface.Ground:= player.Y
	return
}

playAnimation(trigger){
	KeyHandler.GetInstance().HandleAnimationEvent("PlayAnimationBasedOnTrigger", trigger)
	return
}


$!m::
{
	global crowdInterface
	crowdInterface.ToggleCrowdMode()
	return
	
}

	

HandleAllInputs:
	if(fileEventHandler.Running==false and KeyHandler.GetInstance().Running==false){
		targetingInterface.HandlePotentialTargetingEvent()
		fileEventHandler.ListenForAndHandleFileEvent()
		d.UpdateCharacter()
	}
	sleep 300
	return


WatchMouse:	
	
	GetKeyState, LButtonState, LButton, P

if LButtonState = U  ; Button has been released, so drag is complete.	
{
	MsgBox "fuck you"
	SetTimer, WatchMouse, off
	return
}
else{
	CoordMode, Mouse
	MouseGetPos, MouseX, MouseY

	MouseStartX:=d.StartMouse.MouseStartX
	MouseStartY:=d.StartMouse.MouseStartY
	
	DeltaX := MouseX
	DeltaX := (DeltaX - MouseStartX) /1000

	DeltaY := MouseY
	Deltay := (DeltaY - MouseStartY) /1000
	
	d.StartMouse.MouseStartX := MouseX  ; Update for the next timer call to this subroutine.
	d.StartMouse.MouseStartXMouseStartY := MouseY

	WinGetPos, GuiX, GuiY,,, ahk_id %GuiID%

	GuiX += %DeltaX%

	GuiY += %DeltaY%

	SetWinDelay, -1   ; Makes the below move faster/smoother.

	WinMove, ahk_id %GuiID%,, %GuiX%, %GuiY%
	return
}
return


	
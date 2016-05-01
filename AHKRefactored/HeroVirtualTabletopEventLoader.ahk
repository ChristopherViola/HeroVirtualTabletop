#Include HeroVirtualTabletopEventHandler.ahk
#Include CharacterRepository.ahk

Class EventLoader{
	LoadEvents(){
		handler:= HeroVirtualTableTopEventHandler.GetInstance()
		repository:=CharacterRepository.GetInstance()
		handler.AddEvent( new Event("Spawn Character", "Character", "Spawn", repository,"P"))
		handler.AddEvent( new Event("Toggle Camera and Character Maneuvering", "Character", "ToggleManueveringWithCamera", repository,"C"))
		handler.AddEvent( new Event("Delete Character from Desktop", "Character", "ClearFromDesktop", repository,"Del"))
		handler.AddEvent( new Event("Toggle Character is Targeted", "Character", "ToggleTargeted", repository,"T"))
		handler.AddEvent( new Event("Follow And Move To Character", "Character", "TargetAndMoveCameraToCharacter", repository,"H"))
	}
	Bootstrap(){
		this.LoadEvents()
		handler:=HeroVirtualTableTopEventHandler.GetInstance()
		handler.ListenForKeyStrokes()
	}
}

l:= new EventLoader()
l.Bootstrap()






		
		
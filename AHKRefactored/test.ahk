#Include CharacterRepository.ahk
Class DesktopGui
{
	static instance:=""
	_character:=""
	
	DisplayBackGround(xpos){
		Gui, VTTBack: New
		Gui VTTBack:  Color, Black
		Gui VTTBack: +LastFound +AlwaysOnTop +ToolWindow
		Gui VTTBack: -Caption
		Gui VTTBack: +Disabled
		WinSet, Transparent, 190
		
		Gui	VTTBack: Show , x%xPos% y100  w355 h500 NoActivate 
	}
	DisplayForGround(xpos){
		global GuiID
		Gui, VTT: New
		Gui VTT:  Color, Black
		Gui, Font, s11 CWhite Bold, mont_hvbold
		Gui +LastFound +AlwaysOnTop +ToolWindow
		Gui -Caption
		WinSet, TransColor, Black
		Gui VTT: Show , x%xPos% y100  w355 h500 NoActivate 
		WinGet, GuiID, ID, A
	}
	__New(){
		xPos:= 0
		this.DisplayBackground(xpos)
		this.DisplayForGround(xpos)
		fn:=ObjBindMethod(this, "listChange")
	;	
		Gui, Add,ListBox, vCharacterList w300 h300 x10 y10 sort,  a|b|c|d|e|f
		;GuiControl, ChooseString, CharacterList, a
		GuiControl +g, CharacterList, % fn
	}
	
	listChange(){
		local duumy
		GuiControlGet, characterList ,VTT:, characterList
		
		;MsgBox % "made it" . characterList 
	}
	UpdateCharacter(){
		global characterList
		
	}

}

d:= new DesktopGui()

SetTimer HandleAllInputs, 100	
HandleAllInputs:
GuiControl, ChooseString, CharacterList, b
return

listchange(var){
	d.ListChange(A_Gui, A_GuiControl, A_GuiEvent,A_EventInfo)
}



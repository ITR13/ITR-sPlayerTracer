Melon Loader mod for VrChat.

NB: Due to changes to VrChat, you now need to open the in-game quick-menu (escape) before creating the tracers if you want them to have the correct color.

Press left ctrl+T to swap between 3 different modes, Off, Follow, and Stick. When not off, there will be a line between you and every other player in the world, color-coded by their nameplate color.  
In Follow mode, your end of the line will follow you as you move around.  
In Stick mode, your end of the line will stay where you activated it.

If a person changes avatar or joins the world, you need to reapply the mod to have a line between you and that player.

![A bunch of players in the distance with different colors lines pointing towards them](https://raw.githubusercontent.com/ITR13/ITR-sPlayerTracer/master/WATCHME.png)

### Config
There is a config in \[GameRoot\]/UserData/TracerConfig.json:  
**hold**: What key to hold to change mode (if set to None, only trigger is used)  
**trigger**: What key to press to change mode (if set to None you cannot change mode)  
**blockedColor**: What lines pointing to a blocked person should be colored  
**errorColor**: What color to use if nameplate color cannot be determined  

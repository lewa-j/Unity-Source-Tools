# Unity-Source-Tools
Plugin to import resources from the Source engine in Unity3D.

Instructions:

  Step 1: Load scene respective to what game you are extracting from, and click the WorldManager gameobject.

  Step 2: Set the Export Location to where you would want to export the map.(This is optional)

  Step 3: Set Map Name to the name of your map you want to import.

  Step 4: Set Model Name to the name of your model you want to import(untested).

  Step 5: Tick "Load Map" or "Load Model", depending on what you're doing.

  Step 6: Tick Skip Sky. Otherwise, your sky will be full of garbage.

  Step 7: Scroll down to uSrcSettings.

  Step 8: Set the Path to wherever your game folder is(example: D:\SteamLibrary\steamapps\common\GarrysMod )

  Step 9: Depending on what you're doing, adjust the game, have mod, and mod settings. ![Example for Garrysmod](https://i.imgur.com/H4dKv2z.png) (This makes the script look for materials/models in D:\SteamLibrary\steamapps\common\GarrysMod\garrysmod)

  Step 10: Tick Textures, Displacements, Props, Props Dynamic, Entities, and Gen Colliders. Do not tick Lightmaps.

That should work! If I got this wrong, please leave an issue.

## Original Readme Below

Author: vk.com/lewa_j

VK Public: vk.com/uSrcTools


Suported formats:

bsp - maps

vmt - materials

vtf - textures

mdl - models

vvd - models vertices

vtx - model indices


==============================================

uSource
UnitySourceTools
uSrcTools

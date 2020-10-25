# Puppeteer

*RimWorld meets web browser remote control*

Puppeteer is a mod for RimWorld that allows players (often streamers) to give control to specific colonists to their viewers or friends. To participate, viewers need to log in to the Puppeteer website with their Twitch account. Twitch is recommended but **not necessary** to play along, the login is only used to identify the participant.

Once logged in, the viewer can enter a game and gets into a waiting queue. At this point, the player can assign a colonist to a connected viewer. A connected viewer can see most statistics and aspects of their colonist, can alter the colonists appearance, define schedules and priorities, issue commands and interact with a web version of the map visually. Since this is done in a browser the interaction is almost real time but the map is only updated a few times per second. The map can be right-clicked (hold tap on mobile) and you can select things on the map to issue commands for them.

Getting started with Puppeteer:

To follow the progress of this mod, please join my discord at
https://discord.gg/mG5D923

## For Streamers

- Install the mod by downloading file **Puppeteer.zip** from https://github.com/pardeike/Puppeteer/releases/latest
- Start your game and open a web browser at https://puppeteer.rimworld.live
- Use the Twitch button (YouTube button currently under review) to log in
- After logging in, go to Settings -> Streamer and create a game token
- Download the game token and put the file in `AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config`
- Now you can configure your listing under **Stream Information** and put your game online
- After your viewers will enter the game, you can assign them by right clicking on the colonist in the colonist bar or by using the last section on the page
- The upper right corner shows a connection meter that shows you the load on your connection to the server (should normally stay at 0ms)
- Beside that is a small colonist icon with a [+] button that can be used to add a brand new colonist to the colony and assign it in one step
- Creating new colonists in the previous step supports Prepare Carefully and you can even give new colonists weapons to start with if you like

## For Viewers

- Open a web browser at https://puppeteer.rimworld.live
- Use the Twitch button (YouTube button currently under review) to log in
- After logging in, wait in the Lobby until a streamer has made their game available, then select it
- When the streamer sees that you are in the game and available, he/she might assign a colonist to you
- Once assigned the colonists data will appear and some commands are already enabled

## Respawning

Puppeteer has a respawn system that makes deaths caused by viewer interactions less fatal. You place a portal in your base and colonists that satisfy the following conditions will respawn with their apparel and fully healed:

#### Tickets
- Tickets are given at start of a NEW game
- EXISTING games will get the tickets the first time you load then with the new Puppeteer (and unless you save the game, you can repeat this)
- The amount of given tickets is defined in the Puppeteer settings
- A respawn consumes a ticket
- Selecting the portal shows the remaining tickets in the lower left corner
- No tickets means all deaths are permanent

#### Deaths
- A death that is caused by a viewer will be compensated by a respawn
- A death caused by the streamer/player will be permanent
- Any interaction of the streamer/player with the colonist will start a cooldown (blue bar)
- During the cooldown, a death is considered the fault of the streamer/player
- The cooldown time can be set in the Puppeteer settings

#### Portal
- You can have zero or one portal per map
- Placing a portal starts a cooldown
- Removing the portal is only possible after the cooldown to prevent you from moving the portal too quickly
- Without portal, deaths are permanent

ENJOY

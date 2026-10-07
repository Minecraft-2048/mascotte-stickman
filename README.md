# Mascotte Stickman

![A few of his animations](apercus/animations.png)

A stick figure that lives on your Windows desktop. He walks around, jumps onto the top of your windows, dances, fights the mouse cursor, builds block towers, glides with elytra… and you can grab him with the mouse and throw him. There are no image files: everything is drawn by code from a skeleton, which is why you can pick his colour and why he has hundreds of animations.

*[Version française](README.fr.md)*

**On a phone or tablet:** https://minecraft-2048.github.io/mascotte-stickman/ — gravity follows the way you hold the device.

> **Unofficial fan project.** The idea comes from the animations where a stick figure escapes from its drawing program and wreaks havoc on the desktop, Alan Becker's first of all. This project is not made or endorsed by him. Minecraft is a trademark of Mojang; this project is not affiliated with it and contains no game files.

## Install

Download `MascotteStickman.exe` from the [Releases](../../releases) and run it. Windows 10 or 11, nothing else to install.

The exe is not signed, so Windows may show "Windows protected your PC". Click *More info*, then *Run anyway*.

The app is in English unless Windows is in French; you can force either language in *Settings → System*.

## What he does

- **Right-click** him for the menu: colour, animations sorted by family, friends, settings, mute, start with Windows, quit.
- **Colour and head**: 16 colours or any colour you like. The head is a solid disc, except for orange, black and dark red where it is a ring; a setting forces one or the other.
- **946 animations**: moving around, dances (34 arm moves × 8 leg moves, plus waltz, can-can, sirtaki, haka…), gestures, fighting, acrobatics, sport, everyday life, emotions, and a few that only make sense on the edge of a window (sitting with his legs dangling, fishing). About forty props drawn by the program: sword, hammer, guitar, umbrella, broom…
- **Mouse**: grab him, swing him, throw him. He fights the cursor when it comes close, and a fast cursor knocks him over.
- **Windows**: he jumps onto the top of your windows (plain jump, somersault or hero landing), travels with them, and falls when they close.
- **Friends**: up to six stickmen at once (right-click → *Friends* → *Add a stickman*), each with his own colour. They go and see each other on their own, even from one window to another: hello, fist bump, high five, handshake, hug, rock paper scissors, friendly duel, cheers, foot tap, the wave, a push-up contest, the "too slow!" high five, dancing together… Eighteen two-player animations.

![The two-player animations](apercus/duos.png)

- **Music**: when the PC plays music, he sometimes feels like dancing, and his dance follows the tempo. He only reads the output level (like the Windows volume meter); nothing is recorded.
- **Minecraft scenes**: he stacks a block tower or a staircase under his feet and jumps off, glides with elytra, saves a huge fall with a water bucket, throws an Ender pearl and teleports to it, lights TNT that blows the whole gang away, launches a firework, walks through a Nether portal; plus pickaxe, crafting table, bed, minecart, boat, slime trampoline…
- **Command staff**: the first stickman carries a staff topped with a command block. He raises it, the command types itself in the air like in the game console (`/setblock ~1 ~ ~ hay_block`, `/tp @s 512 0`…), then it takes effect: place a block, teleport, call the whole gang, summon lightning or rain, levitate, run at full speed, make the house appear… Fifteen commands.
- **Teleporting**: no "magic" teleporting. It takes a staff command, an Ender pearl or a Nether portal.
- **House and scenery**: a small block house appears near their home when they build it or go inside (the window lights up when someone is in), then goes away after a while — or stays, if you tick the setting. They also light a campfire to warm up, and plant flowers.
- **Special tricks** (right-click → *Special tricks*, or the *Special* tab; one checkbox each, so any of them can be switched off): he suddenly turns giant — his head almost touches the top of the screen — and stomps across it while the others run away, shrinks and scurries around, draws on the screen. Three more are off by default because they touch your PC: opening Alan Becker's YouTube channel, lassoing the mouse cursor, shaking the window he stands on.
- **Spell checker** (off by default, right-click → *Spell checker*): when you have just typed a misspelled word, he flies to it, points at it with a pencil, and the word is replaced; the caret goes back where it was. He does not listen to the keyboard: he asks Windows for the bit of text before the caret (as a screen reader does), never reads a password field, and keeps or sends nothing. Spelling comes from the Windows spell checker, in the Windows language.
- **Prank mode** (off by default): now and then he jumps onto a window, walks to its X and presses it. It is a real click on the X: a program with unsaved work still asks for confirmation, but a game or a video closes at once. He spares the window you are using (configurable), warns you with a speech bubble, and grabbing him with the mouse stops him.
- **Sounds**: 14 sound effects computed by the program, no audio files. Volume and sound families are adjustable, and there is a **mute** switch in the menu.
- **Settings**: a tabbed window with about 100 settings and one checkbox per animation.

## Minecraft textures

Blocks and items in the Minecraft scenes are drawn with the real game textures, pixel for pixel, **if Minecraft (Java Edition) is installed on the PC**: the program reads them straight from the game (the `.jar` of the most recent version, in `.minecraft\versions`). Nothing is copied into the exe or into this repository.

Without Minecraft, or if you untick *Real Minecraft textures* (Settings → Appearance), blocks are drawn by the program and items become simple shapes.

## Phone and tablet version

**https://minecraft-2048.github.io/mascotte-stickman/**

It is a web page (the `docs/` folder) where the stickman lives inside the screen. Add it to your home screen to run it full screen, like an app.

- **Gravity follows the device**: he stands on whichever edge of the screen is down. Tilt or flip the device and he slides, loses his footing and falls to the new side. A good shake sends him flying. (On iPhone and iPad, tap *Tilt* first to allow the sensor.)
- **Touch**: grab him and throw him, tap him for a random animation, tap elsewhere to send him there.
- **Buttons**: colour, one more or one fewer stickman (up to six), and on a computer a button that turns gravity a quarter turn.
- It reuses the animations of the Windows version (exported by `MascotteStickman.exe --web docs`), without the Minecraft scenes, the props, the sounds or the two-player animations.

To try it on your own PC: `python -m http.server --directory docs`, then open `http://localhost:8000`.

## Command line

```
MascotteStickman.exe --jouer "Salto avant"
MascotteStickman.exe --jouer "Tour de blocs"
MascotteStickman.exe --jouer "@amis 3"
MascotteStickman.exe --jouer "@duo Check"
MascotteStickman.exe --planches folder
```

`--jouer` talks to the stickman that is already running: an animation name (the internal names are French in both languages), or a command (`@amis N`, `@duo name`, `@musique`, `@fenetre`, `@reglages`, `@couleur RRGGBB`). `--planches` writes contact sheets: every animation in eight frames (`--planches folder duos` for the two-player ones). `--noms file` lists every name with its English translation.

His settings live in `%LOCALAPPDATA%\MascotteStickman`.

## Build

```
powershell -ExecutionPolicy Bypass -File construire.ps1
```

The script uses the C# compiler that ships with Windows (.NET Framework 4): nothing to install. The code and its comments are written in French.

- `src/Stickman.cs`: the engine (skeleton, drawing, throw physics, windows, friends, scenes, sounds, settings).
- `src/StickmanAnimations.cs`: the animation library. A pose is thirteen numbers (torso, head, shoulders, elbows, hips, knees, height, rotation, offset); walks and dances come from generators.
- `src/StickmanOreille.cs`: listens to the output level and works out the tempo.
- `src/StickmanCorrecteur.cs`: the spell checker (reads the text next to the caret, Windows spell checker, word replacement).
- `src/StickmanLangue.cs`: the French → English dictionary for everything the app displays.

He first lived in the [Mascotte Claude](https://github.com/Minecraft-2048/mascotte-claude) repository, with the other mascots.

## Licence

MIT, see [LICENSE](LICENSE).

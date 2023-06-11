Simple Sound Manager
(c) 2016 Digital Ruby, LLC
https://www.digitalruby.com

Version 1.1.1

Change Log:
1.1.1	-	Add option to not stop music when scene loads.
1.1		-	Added ability to persist music between scene changes.
1.0		-	Initial Release.

Simple Sound Manager is a single script that can manage all your sounds and music in your Unity game.

With a simple scripting API and using extension methods, all your sound and music will sound great.

Looping sounds and music will cross fade nicely when starting and stopping. Global sound and music volume multiplier is possible.

Duplicate audio clip plays are quieted smartly to ensure nice, crisp sound.

Music sounds have a persist parameter. This allows the music to persist between scene changes.

* MUSIC *
It is recommended that you create a script and prefab with all your music audio sources underneath it. You can then call the Simple Sound API to play these music audio sources. Every scene will need to have this prefab.

* IMPORTANT *
Do NOT drag the script into your scene. Simply call the extension methods as done in the demo script and the script will take care of itself.

For additive level loading, set StopSoundsOnLevelLoad to false on the sound manager script.

Please See SoundManagerDemo.cs for code examples.

If you have any questions or bug reports, please email me at support@digitalruby.com. Thank you.
# Mass Effect 2 TLK Tool

## Overview

*Mass Effect 2 TLK Tool* is a tool for modifying TLK files from the *Mass Effect 2* game. Those are the files that contain dialogue lines for characters and all the other text lines for UI.

It allows to extract data from TLK files and save it in an XML file, which can be freely modified and saved back to TLK format. It doesn't have any fancy GUI for live-editing TLK text strings, as it was created mainly to allow collaborative translation.

It's original project page was at BioWare Social Network ([link](http://social.bioware.com/project/3492/)), though the service is about to be shut down. Hence the move to Github.

## History

This is a project created back in 2010 when editing TLK files from *Mass Effect 2* wasn't possible at all. It started as a help-project in a group of translators that wanted to see the first lore DLC (*Lair of the Shadowbroker*) in Polish. Unfortunately, though original *Mass Effect 2* game was translated to Polish, the DLCs were said to be technically "incompatible". Of course, that was not true, they were simply blocked from being installed. It wasn't the case only for Polish language, but also for a few more. 

So the only thing that was a problem for translators was that the TLK files containing text lines from the game, were of unknown format. Fortunatelly, it was only a custom binary file format with [Huffman coding](http://en.wikipedia.org/wiki/Huffman_coding) for text data. Simple TLK decoding demo was released by DerPlaya from *BioWare Social Network*, which was a great starting point. After more research, this tool, with full TLK format support, was made available.

Soon after, the first DLC was translated to Polish and many other languages. Many other mods use this tool to change in-game text strings. There is even a full Mass Effect 2 translation to Turkish, made with this tool. And probably many more projects based on this one, that I'm not aware of, since it's been open-source from the beginning.

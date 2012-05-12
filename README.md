# cross copy

The web app http://www.cross-copy.net attacs the problem when you  spontaneously need to transfer data between different devices (eg. "inter-device copy and paste") and don't want any setup or login. The backend is written in node.js. There is also a commandline tool for sending and receiving stuff when working on a server.

## Use Cases

 * sharing a link in the office (alternatives: Skype, spelling, Post-It)
 * getting a phone number from laptop to mobile (alternatives: type in, repeat search on mobile, send mail to yourself)
 * get a file from desktop to laptop or mobile (alternatives: Dropbox, Evernote, send email)
 * send address to someone who is asking for it on the phone (alternatives: send email, spell it) 
 * ...

## Service API

The web-client uses the hostname http://www.cross-copy.net

waiting for data to appear "on the given phrase" (long polling)
    GET   /api/<secret code>

send data in body to all waiting clients    
    PUT   /api/<secret code>

store a file temporary on the server at the given uri
    POST  /api/<secret code>/<filename.extension>

watch number of listeners for changes (long polling)
    GET   /api/<secret code>?watch=listeners&count=<known num of listeners>


## License

Copyright (c) Rodja Trappe

Licensing: GPL v3

See COPYING.txt for the GNU GENERAL PUBLIC LICENSE

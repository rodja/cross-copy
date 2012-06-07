# About

*cross copy* (http://www.cross-copy.net) solves the problem when you  spontaneously need to transfer data between different devices (eg. "inter-device copy and paste") and don't want any setup, login or sharing links through other channels. The only thing which needs to be done on all participating devices is to agree on a secret phrase and connect to a RESTful Web Service (written in node.js). The hosted [Web App](http://www.cross-copy.net) provides a neat Web App for doing so in any modern Browser (not IE). There is also a commandline tool for sending and receiving stuff when working on a remote system via ssh or similar (of course you can also use curl directly). Special Apps for iOS and Android are work in progress.

## Use Cases

 * sharing a link in the office (alternatives: Skype, spelling, Post-It)
 * getting a phone number from laptop to mobile (alternatives: type in, repeat search on mobile, send mail to yourself)
 * get a file from desktop to laptop or mobile (alternatives: Dropbox, Evernote, send email)
 * send address to someone who is asking for it on the phone (alternatives: send email, spell it) 
 * ...

## Service API

The official server is available through http://www.cross-copy.net

### Basic Usage

#### waiting for data to appear "on the given secret" (long polling)

    GET   /api/<secret code>

#### send data in body to all waiting clients    

    PUT   /api/<secret code>

#### parameters

##### Device
If the parameter device_id=[uuid] is added to the above urls for GET & PUT,  you wont receive the data you send out with the same uuid. 

#### lists data which had been recently sumbitted for a given secret

    GET   /api/<secret code>/recent-data.json

Normally the devices which wants to receive something will start their longpolling GET request *before* data is submitted via PUT to the same secret. But if a client comes late to the party, the recent-data.json ressource provides access to a short history of stuff which has been submitted.

### Sharing Files

#### store a file temporary on the server at the given uri

    POST  /api/<secret code>/<filename.extension>

#### watch number of listeners for changes (long polling)

    GET   /api/<secret code>?watch=listeners&count=<known num of listeners>


#### Keep on server
By adding the parameter keep_for=[time in seconds] to the PUT url, you can modify the time until the data will be not longer available in recent-data.json


## License

Copyright (c) Rodja Trappe

Licensing: GPL v3

See COPYING.txt for the GNU GENERAL PUBLIC LICENSE

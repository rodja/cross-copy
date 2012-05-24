//
// cross-copy.net server implemented in node.js 
//
// see https://github.com/rodja/cross-copy/blob/master/README.md#service-api
// for usage and documentation

/*  
    Copyright 2012 Rodja Trappe

    This file is part of cross copy.

    cross copy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    cross copy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with cross copy.  If not, see <http://www.gnu.org/licenses/>.
*/

var port = 8080;

var waitingReceivers = {};
var watchers = {};
var filecache = {};
var messagecache = {};

var header = {'Content-Type': 'text/plain'}

var http = require('http');
var fs = require('fs');
var path = require('path');

// using a fork of formidable be make octstream parsing possible
var formidable = require('./scriby-node-formidable-19219c8');
var util = require('util');

// guid generator from http://stackoverflow.com/questions/105034/how-to-create-a-guid-uuid-in-javascript
function guid() {
    var S4 = function() {
       return (((1+Math.random())*0x10000)|0).toString(16).substring(1);
    };
    return (S4()+S4()+"-"+S4()+"-"+S4()+"-"+S4()+"-"+S4()+S4()+S4());
}

String.prototype.endsWith = function(suffix) {
    return this.indexOf(suffix, this.length - suffix.length) !== -1;
};

function track(pageName){
  return; // no tracking while developing on master branch
  var options = {
    host: 'www.google-analytics.com',
    path: '/__utm.gif?mn=1766671084&utmhn=api.cross-copy.net&utmr=-&utmp=' + pageName + '&utmac=UA-31324545-2&utmcc=__utma%3D103436114.1871841882.1336279481.1337056578.1337314345.13%3B%2B__utmz%3D103436114.1336279481.1.1.utmcsr%3D(direct)%7Cutmccn%3D(direct)%7Cutmcmd%3D(none)%3B'
  };

  http.get(options, function(res) {
    //console.log("Got receiverWhoSendsTheData: " + res.statusCode);
  }).on('error', function(e) {
    //console.log("Got error: " + e.message);
  });
}

track("server-started");

function updateWatchers(secret){
  var untouched = [];     
  watchers[secret].forEach(function(watcher){
    if (watcher.knownNumberOfListeners != waitingReceivers[secret].length){
      watcher.writeHead(200, header);
      watcher.end(waitingReceivers[secret].length + '\n');
    } else untouched.push(watcher);
  });
  watchers[secret] = untouched;
}

server = http.createServer(function (req, res) {

  var pathname = require('url').parse(req.url).pathname;
  var secret = pathname.substring(5);   
  var query = require('url').parse(req.url, true).query;    
  var device = query.device_id;  
  
  console.log(req.method + ": " + pathname);

  if (watchers[secret] === undefined) watchers[secret] = [];
  if (waitingReceivers[secret] === undefined) waitingReceivers[secret] = [];
   

  if (req.method === 'GET' && pathname.indexOf('/api') == 0) {

    if (query.watch == 'listeners') {

      if (waitingReceivers[secret].length != query.count){
        res.writeHead(200, header);
        res.end(waitingReceivers[secret].length + '\n');
      } else{
        res.knownNumberOfListeners = query.count;
        watchers[secret].push(res);
      }
      track("watch-listeners");
      return;
    }
    
    if (secret.indexOf('/') == -1){
    
      if (secret.endsWith(".json")){
        secret = secret.substr(0, secret.length - 5);
        res.writeHead(200);
        res.end(JSON.stringify(messagecache[secret]));
        return;
      }

      req.connection.on('close',function(){
         res.aborted = true;
         track("get-aborted");
        
         var livingwaitingReceivers = [];
         waitingReceivers[secret].forEach(function(response){
           if (!response.aborted) livingwaitingReceivers.push(response);
         });

         waitingReceivers[secret] = livingwaitingReceivers;        
         updateWatchers(secret);
      });
  
      if (device) res.device = device;
      
      // if not asking for a file we will wait for the shared data
      waitingReceivers[secret].push(res); 
      track("get-waiting");
      
      updateWatchers(secret);
      return;
    }

    if (secret.endsWith("/recent-data.json")){

      secret = secret.substr(0, secret.length - 17);
      res.writeHead(200);
      res.end(JSON.stringify(messagecache[secret]));

    } else if (filecache[secret] != undefined){
      var file = filecache[secret];
      fs.readFile(file.path, function(error, content) {
        if (error) {
          res.writeHead(500);
          track("get-file-500");
          res.end();
        } else {
          res.writeHead(200, { 'Content-Type': 'application/octet-stream' });
          res.end(content, 'binary');
          track("get-file-200");
        }
      });
    } else {
      res.writeHead(404);
      track("get-file-404");
      res.end();
    }
 
  } else if (req.method === 'PUT' && pathname.indexOf('/api') == 0) {
    //console.log("PUT waitingReceivers  " + waitingReceivers[secret].length);

    if (waitingReceivers[secret] == [] || waitingReceivers[secret].length == 0 || 
       ( waitingReceivers[secret].length == 1 && waitingReceivers[secret][0].device === device && device !== undefined )){
      res.writeHead(202, header);
      res.end('0\n');
      track("put-202");
    }

    req.on('data', function(chunk) {

     if (messagecache[secret] === undefined) messagecache[secret] = [];
     var msg = {data: chunk.toString(), id: guid()};
     messagecache[secret].push(msg);
     setTimeout(function(){
       messagecache[secret].splice(messagecache[secret].indexOf(msg), 1);
     }, 1000 * (query.keep_for || 10) );

      var receiverWhoSendsTheData;
      waitingReceivers[secret].forEach(function(response){
        if (response.device !== device || !device){
          response.writeHead(200, header);
          track("get-200");
          response.end(chunk);
        } else
          receiverWhoSendsTheData = response;
      });
      
      track("put-" + waitingReceivers[secret].length);

      res.writeHead(200, header);
      res.end( (waitingReceivers[secret].length - (receiverWhoSendsTheData ? 1 : 0)) + '\n');
  
      if (receiverWhoSendsTheData)
        waitingReceivers[secret] = [receiverWhoSendsTheData];
      else
        waitingReceivers[secret] = [];

      updateWatchers(secret);
   });

  } else if (req.method === 'POST' && pathname.indexOf('/api') == 0) {

    if (secret.indexOf('/') == -1) {
      track("post-403");
      res.writeHead(403);
      res.end();
    }

    var form = new formidable.IncomingForm();
      form.parse(req, function(err, fields, files) {
        if (err) {
          res.writeHead(500); res.end();
          track("post-500");
          return;
        }
        var file = files.file;
        if (file === undefined) file = files.data;
        filecache[secret] = file;

        res.writeHead(200, {'content-type': 'text/plain'});
        res.end('{"url": "/api/'+ secret + '"}');
        track("post-200");
        
        setTimeout(function(){ 
          fs.unlink(file.path);
        }, 10 * 60 * 1000);
      });
  }

  if (pathname.indexOf('/api') == 0) return;

  var filePath = '.' + req.url;
  if (filePath == './')
    filePath = './index.html';
  filePath = 'web-client/' + filePath;    

    var extname = path.extname(filePath);
    var contentType = 'text/html';
    switch (extname) {
        case '.png':
            contentType = 'image/png';
            break;
        case '.js':
            contentType = 'text/javascript';
            break;
        case '.css':
            contentType = 'text/css';
            break;
    }
     
    path.exists(filePath, function(exists) {
     
        if (exists) {
            fs.readFile(filePath, function(error, content) {
                if (error) {
                    res.writeHead(500);
                    res.end();
                }
                else {
                    res.writeHead(200, { 'Content-Type': contentType });
                    res.end(content, 'utf-8');
                }
            });
        }
        else {
            res.writeHead(404);
            res.end();
        }
    });


}).listen(port);

console.log('cross-copy is running at http://loclahost:' + port + '/');

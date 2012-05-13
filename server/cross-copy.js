//
// cross-copy.net server implemented in node.js 
//
// usage:

//  wait's for data on the given phrase (long polling)
//    GET   /api/<secret code>
//  sends data in body to all waiting clients    
//    PUT   /api/<secret code>
//  store a file temporary on the server at the given uri
//    POST  /api/<secret code>/<filename.extension>
//  watch number of listeners for changes (long polling)
//    GET   /api/<secret code>?watch=listeners&count=<known num of listeners>

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

var getters = {};
var watchers = {};
var filecache = {};

var header = {'Content-Type': 'text/plain'}

var http = require('http');
var fs = require('fs');
var path = require('path');

// using a fork of formidable be make octstream parsing possible
var formidable = require('./scriby-node-formidable-19219c8');
var util = require('util');

function updateWatchers(secret){
  var untouched = [];     
  watchers[secret].forEach(function(watcher){
    if (watcher.knownNumberOfListeners != getters[secret].length){
      watcher.writeHead(200, header);
      watcher.end(getters[secret].length + '\n');
    } else untouched.push(watcher);
  });
  watchers[secret] = untouched;
}

server = http.createServer(function (req, res) {

  var pathname = require('url').parse(req.url).pathname;
  var secret = pathname.substring(5);
  
  //console.log(util.inspect(filecache));
  //console.log(util.inspect(getters));
  if (watchers[secret] === undefined) watchers[secret] = [];
  if (getters[secret] === undefined) getters[secret] = [];
   

  if (req.method === 'GET' && pathname.indexOf('/api') == 0) {
    var query = require('url').parse(req.url, true).query;    

    //console.log(req.method + ' ' + pathname + " " + require('url').parse(req.url).query );
    
    if (query.watch == 'listeners') {

      if (getters[secret].length != query.count){
        res.writeHead(200, header);
        res.end(getters[secret].length + '\n');
      } else{
        res.knownNumberOfListeners = query.count;
        watchers[secret].push(res);
      }
      return;
    }
    
    req.socket.secret = secret;
    req.connection.on('close',function(){
       res.aborted = true;
       var livingGetters = [];
       getters[secret].forEach(function(getter){
           if (!getter.aborted) livingGetters.push(getter);
       });

       getters[secret] = livingGetters;        
       updateWatchers(secret);
    });

    if (secret.indexOf('/') == -1){
      // if not asking for a file we will wait for the shared data
      getters[secret].push(res); 
      
      updateWatchers(secret);
      return;
    }

    if (filecache[secret] != undefined){
      var file = filecache[secret];
      fs.readFile(file.path, function(error, content) {
        if (error) {
          res.writeHead(500);
          res.end();
        } else {
          res.writeHead(200, { 'Content-Type': 'application/octet-stream' });
          res.end(content, 'binary');
        }
      });
    } else {
      res.writeHead(404);
      res.end();
    }
 
  } else if (req.method === 'PUT' && pathname.indexOf('/api') == 0) {
    //console.log("PUT getters  " + getters[secret].length);

    if (getters[secret] == [] || getters[secret].length == 0){
      res.writeHead(404, header);
      res.end('0\n');
      return;
    }

    req.on('data', function(chunk) {
      getters[secret].forEach(function(getter){
        getter.writeHead(200, header);
        getter.end(chunk);
      });
      
      res.writeHead(200, header);
      res.end(getters[secret].length + '\n');
      getters[secret] = [];
      updateWatchers(secret);
   });

  } else if (req.method === 'POST' && pathname.indexOf('/api') == 0) {

    if (secret.indexOf('/') == -1) {
      res.writeHead(403);
      res.end();
    }

    var form = new formidable.IncomingForm();
      form.parse(req, function(err, fields, files) {
        if (err) {
          res.writeHead(500); res.end();
          return;
        }
        var file = files.file;
        if (file === undefined) file = files.data;
        filecache[secret] = file;

        res.writeHead(200, {'content-type': 'text/plain'});
        res.end('{"url": "/api/'+ secret + '"}');

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

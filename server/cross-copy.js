// cross-copy.net server implemented in node.js 
//
// usage:
//    GET /<your secret code>      wait's for data on the given phrase
//    PUT /<your secret code>      sends data in body to all waiting clients

var port = 8124;
var host = "127.0.0.1";

var getters = {};
var header = {'Content-Type': 'text/plain'}

var http = require('http');
http.createServer(function (req, res) {

  var secret = require('url').parse(req.url).pathname.substring(1);

  console.log(req.method + " " + secret);

  if (req.method === 'GET') {
    
    if (secret === ""){
      fs.readFile('../web-client/index.html', function(error, content) {
        if (error) {
            console.log(error);
            res.writeHead(500);
            res.end();
        }
        else {
            res.writeHead(200, { 'Content-Type': 'text/html' });
            res.end(content, 'utf-8');
        }
      });
      return;
    }

    if (getters[secret] === undefined) getters[secret] = [];
    getters[secret].push(res);
 
  } else if (req.method === 'PUT' || req.method === 'OPTIONS' ) {

      if (getters[secret] == undefined){
        res.writeHead(404, header);
        res.end('0\n');
        return;
      }

      req.on('data', function(chunk) {
      console.log("chunk is: " + chunk);
        getters[secret].forEach(function(getter){
          getter.writeHead(200, header);
          getter.end(chunk + '\n');
        });
        
        res.writeHead(200, header);
        res.end(getters[secret].length + '\n');
        getters[secret] = undefined;
     });
  }


}).listen(port, host);

console.log('cross-copy is running at http://' + host + ':' + port + '/');


var fs = require('fs');
var path = require('path');
 
http.createServer(function (request, response) {
 
    var filePath = '.' + request.url;
    if (filePath == './')
        filePath = './index.html';
    filePath = '../web-client/' + filePath;    
  
    var extname = path.extname(filePath);
    var contentType = 'text/html';
    switch (extname) {
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
                    response.writeHead(500);
                    response.end();
                }
                else {
                    response.writeHead(200, { 'Content-Type': contentType });
                    response.end(content, 'utf-8');
                }
            });
        }
        else {
            response.writeHead(404);
            response.end();
        }
    });
     
}).listen(++port);

console.log('web-client is running at http://' + host + ':' + port + '/');


// cross-copy.net server implemented in node.js 
//
// usage:
//    GET /api/<your secret code>      wait's for data on the given phrase
//    PUT /api/<your secret code>      sends data in body to all waiting clients

var port = 8080;

var getters = {};
var header = {'Content-Type': 'text/plain'}

var http = require('http');
var fs = require('fs');
var path = require('path');
server = http.createServer(function (req, res) {

  var pathname = require('url').parse(req.url).pathname;
  var secret = pathname.substring(5);
  
  //console.log(req.method + ' ' + pathname);

  if (req.method === 'GET' && pathname.indexOf('/api') == 0) {
    
    if (getters[secret] === undefined) getters[secret] = [];
    req.socket.secret = secret;
    getters[secret].push(res);    
 
  } else if (req.method === 'PUT' && pathname.indexOf('/api') == 0) {

      if (getters[secret] == undefined){
        res.writeHead(404, header);
        res.end('0\n');
        return;
      }

      req.on('data', function(chunk) {
        var livingGetters = 0;
        getters[secret].forEach(function(getter){
          if (getter.socket.remoteAddress != undefined)
            livingGetters++;
          getter.writeHead(200, header);
          getter.end(chunk + '\n');
        });
        
        res.writeHead(200, header);
        res.end(livingGetters + '\n');
        getters[secret] = undefined;
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

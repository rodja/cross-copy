// cross-copy.net server implemented in node.js 
//
// usage:
//    GET /<your secret code>      wait's for data on the given phrase
//    PUT /<your secret code>      sends data in body to all waiting clients

var port = 8124;
var host = "127.0.0.1";

var getters = {};
var header = {'Content-Type': 'text/plain', 'Access-Control-Allow-Origin' : '*'}

var http = require('http');
http.createServer(function (req, res) {

  var secret = require('url').parse(req.url).pathname.substring(1);

  if (req.method === 'GET') {
    
    if (getters[secret] === undefined) getters[secret] = [];
    getters[secret].push(res);
 
 } else if (req.method === 'PUT') {

      if (getters[secret] == undefined){
        res.writeHead(404, header);
        res.end('0\n');
        return;
      }

      req.on('data', function(chunk) {
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

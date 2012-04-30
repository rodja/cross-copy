var port = 8124;
var host = "127.0.0.1";

var url = require('url');

var cache = {};

var http = require('http');
http.createServer(function (req, res) {

  var secret = url.parse(req.url).pathname.substring(1);

  if (req.method === 'GET') {
    res.writeHead(200, {'Content-Type': 'text/plain'});
    res.end(secret + '\n');
  } else if (req.method === 'PUT') {
      if (cache[secret] != undefined){
        res.writeHead(403, {'Content-Type': 'text/plain'});
        res.end('ALREADY IN USE\n');
      }

      req.on('data', function (chunk) {
        cache[secret] = chunk;
         
        res.writeHead(200, {'Content-Type': 'text/plain'});
        res.end('ACCEPTED\n');
     });
  }


}).listen(port, host);

console.log('cross-copy is running at http://' + host + ':' + port + '/');
